#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CatAsset.Runtime;
using CyanStars.Framework;
using CyanStars.Framework.Asset;
using CyanStars.Gameplay.MusicGame;
using CyanStars.Utils;
using UnityEngine;

namespace CyanStars.Chart.Loading
{
    /// <summary>
    /// 谱包加载器 — 负责谱包的批量/增量加载和重载
    /// </summary>
    public static class ChartPackDataLoader
    {
        /// <summary>
        /// 谱包索引文件的文件名
        /// </summary>
        public const string ChartPackFileName = "ChartPack.json";

        /// <summary>
        /// 谱包内存放曲绘、音频等资源文件的文件夹名
        /// </summary>
        /// <remarks>谱包数据里的 <c>CoverFilePath</c>、<c>AudioFilePath</c> 是以本文件夹开头的、相对于工作区的路径</remarks>
        public const string ChartPackAssetsFolder = "Assets";

        /// <summary>
        /// 内置谱包 SO 文件路径
        /// </summary>
        private const string InternalChartPackListFilePath =
            "Assets/BundleRes/ScriptObjects/InternalMap/InternalMapList.asset";

        /// <summary>
        /// 从磁盘重新加载全部谱包，包括内置和玩家谱包
        /// </summary>
        /// <remarks>
        /// <para>内置谱包以 SO 中的顺序为准，必定排在社区谱包之前；</para>
        /// <para>社区谱包按 <see cref="ChartPackOrderStore"/> 记录的顺序排列，详见 <see cref="ApplyCommunityChartPackOrder"/>。</para>
        /// </remarks>
        public static async Task<List<RuntimeChartPack>> ReloadAllAsync(string playerChartPacksFolderPath)
        {
            var paths = new List<string>();

            using var internalListHandler = await GameRoot.Asset.LoadAssetAsync<InternalChartPackListSO>(InternalChartPackListFilePath);
            InternalChartPackListSO internalChartPackListSO = internalListHandler.Asset;
            foreach (var path in internalChartPackListSO.ChartPackFilePaths)
            {
                paths.Add(path);
            }

            int internalPacksCount = paths.Count;

            paths.AddRange(ApplyCommunityChartPackOrder(playerChartPacksFolderPath));

            // 批量加载以提高效率，后续单个加载时能直接快速完成
            using var batchAssetHandler = await GameRoot.Asset.BatchLoadAssetAsync(paths);

            var newPacks = new List<RuntimeChartPack>();
            for (int i = 0; i < paths.Count; i++)
            {
                using AssetHandler<TextAsset> textHandler = GameRoot.Asset.LoadAssetAsync<TextAsset>(paths[i]);

                if (!textHandler.IsDone)
                    await textHandler;

                if (textHandler.Asset?.text == null)
                {
                    Debug.LogError($"无法将 {paths[i]} 转换为 {nameof(ChartPackData)}，相关谱包无法加载！");
                    continue;
                }

                ChartPackData? chartPackData = JsonLoadHelper.LoadData<ChartPackData>(textHandler.Asset.text);

                if (chartPackData == null)
                {
                    Debug.LogError($"无法将 {paths[i]} 转换为 {nameof(ChartPackData)}，相关谱包无法加载！");
                    continue;
                }

                string? workspacePath = Path.GetDirectoryName(paths[i]);
                if (workspacePath == null)
                {
                    Debug.LogError($"谱包路径为空：{chartPackData.Title}，相关谱包无法加载！");
                    continue;
                }

                bool isInternal = i < internalPacksCount;
                if (isInternal && !VerifyInternalChartPackMetaData(chartPackData))
                {
                    Debug.LogWarning($"某个内置谱包元数据不正确：{chartPackData.Title}，当前已允许加载，正式发布时应当修复");
                }

                newPacks.Add(new RuntimeChartPack(chartPackData, isInternal, workspacePath));
            }

            return newPacks;
        }

        /// <summary>
        /// 扫描并排序社区谱包的索引文件路径
        /// </summary>
        /// <param name="playerChartPacksFolderPath">社区谱包根目录的绝对路径</param>
        /// <returns>按游戏内顺序排列的谱包索引文件绝对路径</returns>
        /// <remarks>
        /// <para>按约定，社区谱包平铺在根目录下的一级文件夹内；</para>
        /// <para>已在顺序文件中的按记录排列，丢失的从记录中删去，未记录的新谱包排到最后，记录变化时写回文件。</para>
        /// </remarks>
        private static List<string> ApplyCommunityChartPackOrder(string playerChartPacksFolderPath)
        {
            if (!Directory.Exists(playerChartPacksFolderPath))
            {
                Directory.CreateDirectory(playerChartPacksFolderPath);
            }

            var scannedPackFolderNames = new List<string>();
            foreach (string packFolderPath in Directory.EnumerateDirectories(playerChartPacksFolderPath))
            {
                string packFilePath = PathUtil.Combine(packFolderPath, ChartPackFileName);
                if (File.Exists(packFilePath))
                {
                    scannedPackFolderNames.Add(PathUtil.GetName(packFolderPath));
                }
            }

            List<string> orderedPackFolderNames =
                ChartPackOrderStore.ApplyPersistedOrder(playerChartPacksFolderPath, scannedPackFolderNames);

            var orderedPaths = new List<string>(orderedPackFolderNames.Count);
            foreach (string packFolderName in orderedPackFolderNames)
            {
                orderedPaths.Add(PathUtil.Combine(playerChartPacksFolderPath, packFolderName, ChartPackFileName));
            }

            return orderedPaths;
        }

        /// <summary>
        /// 从磁盘加载一张谱包（视为玩家谱包）
        /// </summary>
        public static async Task<RuntimeChartPack> AddFromDiskAsync(string chartPackFilePath)
        {
            using var handler = await GameRoot.Asset.LoadAssetAsync<ChartPackData>(chartPackFilePath);
            ChartPackData? chartPackData = handler.Asset;

            if (chartPackData == null)
                throw new NullReferenceException("尝试加载的谱包为 null，加载失败！");

            Debug.Log($"已增量加载谱包 {chartPackFilePath}");
            return new RuntimeChartPack(
                chartPackData,
                false,
                GetWorkspacePath(chartPackFilePath)
            );
        }

        /// <summary>
        /// 从磁盘重载某个玩家谱包的数据
        /// </summary>
        public static async Task<RuntimeChartPack> ReloadFromDiskAsync(string workspacePath)
        {
            string chartPackFilePath = PathUtil.Combine(workspacePath, ChartPackFileName);

            using var reloadHandler = await GameRoot.Asset.LoadAssetAsync<ChartPackData>(chartPackFilePath);
            ChartPackData? chartPackData = reloadHandler.Asset;
            if (chartPackData == null)
                throw new NullReferenceException("尝试加载的谱包为 null，增量更新失败！");

            Debug.Log($"已增量更新谱包 {chartPackFilePath}");
            return new RuntimeChartPack(
                chartPackData,
                false,
                GetWorkspacePath(chartPackFilePath)
            );
        }

        /// <summary>
        /// 取谱包索引文件所在的工作区目录
        /// </summary>
        /// <exception cref="ArgumentException">路径里取不到目录（如传入了文件名）</exception>
        private static string GetWorkspacePath(string chartPackFilePath)
        {
            string? workspacePath = Path.GetDirectoryName(PathUtil.Normalize(chartPackFilePath));
            if (string.IsNullOrEmpty(workspacePath))
                throw new ArgumentException($"无法获取谱包的工作区目录：{chartPackFilePath}", nameof(chartPackFilePath));

            return workspacePath;
        }

        /// <summary>
        /// 校验内置谱包下谱面是否合规
        /// </summary>
        /// <remarks>各个难度都有且仅有一张谱面，难度与下标一致，不存在 null 难度谱面</remarks>
        private static bool VerifyInternalChartPackMetaData(ChartPackData chartPackData)
        {
            return chartPackData.ChartMetaDatas.Count == 4 &&
                chartPackData.ChartMetaDatas[0].Difficulty == ChartDifficulty.KuiXing &&
                chartPackData.ChartMetaDatas[1].Difficulty == ChartDifficulty.QiMing &&
                chartPackData.ChartMetaDatas[2].Difficulty == ChartDifficulty.TianShu &&
                chartPackData.ChartMetaDatas[3].Difficulty == ChartDifficulty.WuYin;
        }
    }
}
