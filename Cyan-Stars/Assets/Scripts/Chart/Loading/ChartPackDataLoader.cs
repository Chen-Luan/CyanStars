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
        /// 从磁盘重新加载全部谱包（内置谱包按 SO 顺序排在最前，社区谱包按持久化顺序排列）
        /// </summary>
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
            paths.AddRange(ScanAndOrderCommunityPackPaths(playerChartPacksFolderPath));

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
        /// 从磁盘增量加载单个社区谱包
        /// </summary>
        public static Task<RuntimeChartPack> AddFromDiskAsync(string chartPackFilePath)
        {
            return LoadSinglePlayerPackAsync(chartPackFilePath, "加载");
        }

        /// <summary>
        /// 从磁盘重载单个社区谱包
        /// </summary>
        public static Task<RuntimeChartPack> ReloadFromDiskAsync(string workspacePath)
        {
            string chartPackFilePath = PathUtil.Combine(workspacePath, ChartPackFileName);
            return LoadSinglePlayerPackAsync(chartPackFilePath, "更新");
        }

        private static async Task<RuntimeChartPack> LoadSinglePlayerPackAsync(string chartPackFilePath, string actionName)
        {
            using var handler = await GameRoot.Asset.LoadAssetAsync<ChartPackData>(chartPackFilePath);
            ChartPackData? chartPackData = handler.Asset;

            if (chartPackData == null)
                throw new NullReferenceException($"尝试{actionName}的谱包为 null，操作失败：{chartPackFilePath}");

            if (!TryGetWorkspacePath(chartPackFilePath, out string workspacePath))
                throw new ArgumentException($"无法获取谱包的工作区目录：{chartPackFilePath}", nameof(chartPackFilePath));

            Debug.Log($"已增量{actionName}谱包 {chartPackFilePath}");
            return new RuntimeChartPack(chartPackData, false, workspacePath);
        }

        /// <summary>
        /// 扫描社区谱包根目录的一级子文件夹，并按持久化顺序返回对应的索引文件路径
        /// </summary>
        private static List<string> ScanAndOrderCommunityPackPaths(string playerChartPacksFolderPath)
        {
            if (!Directory.Exists(playerChartPacksFolderPath))
                Directory.CreateDirectory(playerChartPacksFolderPath);

            var scannedFolderNames = new List<string>();
            foreach (string packFolderPath in Directory.EnumerateDirectories(playerChartPacksFolderPath))
            {
                string packFilePath = PathUtil.Combine(packFolderPath, ChartPackFileName);
                if (File.Exists(packFilePath))
                    scannedFolderNames.Add(PathUtil.GetName(packFolderPath));
            }

            List<string> orderedFolderNames =
                ChartPackOrderStore.ApplyPersistedOrder(playerChartPacksFolderPath, scannedFolderNames);

            var orderedPaths = new List<string>(orderedFolderNames.Count);
            foreach (string folderName in orderedFolderNames)
            {
                orderedPaths.Add(PathUtil.Combine(playerChartPacksFolderPath, folderName, ChartPackFileName));
            }

            return orderedPaths;
        }

        private static bool TryGetWorkspacePath(string chartPackFilePath, out string workspacePath)
        {
            string? dir = Path.GetDirectoryName(PathUtil.Normalize(chartPackFilePath));
            if (string.IsNullOrEmpty(dir))
            {
                workspacePath = string.Empty;
                return false;
            }

            workspacePath = PathUtil.Normalize(dir);
            return true;
        }

        /// <summary>
        /// 校验内置谱包是否严格包含 4 个难度且与下标顺序一致
        /// </summary>
        private static bool VerifyInternalChartPackMetaData(ChartPackData chartPackData)
        {
            var metas = chartPackData.ChartMetaDatas;
            return metas is { Count: 4 } &&
                metas[0]?.Difficulty == ChartDifficulty.KuiXing &&
                metas[1]?.Difficulty == ChartDifficulty.QiMing &&
                metas[2]?.Difficulty == ChartDifficulty.TianShu &&
                metas[3]?.Difficulty == ChartDifficulty.WuYin;
        }
    }
}
