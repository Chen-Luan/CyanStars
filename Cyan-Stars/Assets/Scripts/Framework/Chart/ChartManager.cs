using System;
using System.Collections.Generic;
using System.IO;
using CyanStars.Chart;
using CyanStars.Framework.File;
using CyanStars.Gameplay.MusicGame;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

namespace CyanStars.Framework.Chart
{
    /// <summary>
    /// 谱包管理类，用于在选曲界面解析谱包、提供导入导出支持，和在制谱器内提供谱面/资源读写支持
    /// </summary>
    public class ChartManager : BaseManager
    {
        /*  谱包文件结构：
         *  - 内部谱包路径，位于游戏读写区内
         *    - 谱包0
         *      - ChartPackData.json
         *      - Charts
         *        - Chart0.json
         *        - Chart1.json
         *        - ...
         *      - Assets
         *        - Music1.ogg
         *        - Cover.png
         *        - ...
         *    - 谱包1
         *      - ...
         */

        /// <summary>
        /// 内置谱包清单 SO 文件路径
        /// </summary>
        public const string InternalChartPackListFilePath =
            "Assets/BundleRes/ScriptObjects/InternalMap/InternalMapList.asset";

        /// <summary>
        /// 谱包中谱面文件夹的名称
        /// </summary>
        /// <example>.../【ChartPacks】/ChartPack0/【Charts】/Chart0.json</example>
        public const string ChartPacksFolderName = "ChartPacks";

        /// <summary>
        /// 谱包中谱面文件夹的名称
        /// </summary>
        /// <example>.../ChartPacks/ChartPack0/【Charts】/Chart0.json</example>
        public const string ChartsFolderName = "Charts";

        /// <summary>
        /// 谱包中资源文件夹的名称
        /// </summary>
        /// <example>.../ChartPacks/ChartPack0/【Assets】/Cover.png</example>
        public const string AssetsFolderName = "Assets";

        /// <summary>
        /// 谱包（谱面元数据）文件的名称，含后缀
        /// </summary>
        /// <example>.../ChartPacks/ChartPack0/【ChartPackData.json】</example>
        public const string ChartPackFileName = "ChartPackData.json";


        /// <summary>
        /// 内置谱包文件夹路径
        /// </summary>
        public string InternalChartPackFolderPath =>
            Path.Combine("Assets/CysMultimediaAssets/ChartPacks", ChartPacksFolderName);


        /// <summary>
        /// 玩家导入谱包文件夹绝对路径，位于应用数据
        /// </summary>
        /// <example>【.../ChartPacks】/ChartPack0/Charts/Chart0.json</example>
        public static string PlayerChartPacksFolderPath =>
            Path.Combine(Application.persistentDataPath, ChartPacksFolderName);


        /// <summary>
        /// 加载的全部谱包
        /// </summary>
        public List<ChartPack> ChartPacks { get; private set; } = new List<ChartPack>();

        /// <summary>
        /// 当前选中的谱包下的谱面数据
        /// </summary>
        /// <remarks>考虑到性能问题，不会加载所有谱包的谱面，只会在选中谱包时加载其中的谱面</remarks>
        public List<CyanStars.Chart.Chart> Charts { get; private set; } = new List<CyanStars.Chart.Chart>();

        /// <summary>
        /// 当前选中的谱包
        /// </summary>
        public ChartPack SelectedChartPack { get; private set; }

        /// <summary>
        /// 当前选中的谱面
        /// </summary>
        public CyanStars.Chart.Chart SelectedChart { get; private set; }

        /// <summary>
        /// 当前选中的音乐版本
        /// </summary>
        public MusicVersionData SelectedMusicVersion { get; private set; }


        public override int Priority { get; }

        public override void OnInit()
        {
        }

        public override void OnUpdate(float deltaTime)
        {
        }


        /// <summary>
        /// 清空谱包，并从磁盘获取内置谱包和玩家谱包
        /// </summary>
        private async void LoadChartPacksFromDisk()
        {
            ChartPacks.Clear();

            // 先加载内置谱面
            InternalChartPackListSO internalListSO =
                await GameRoot.Asset.LoadAssetAsync<InternalChartPackListSO>(InternalChartPackListFilePath);
            foreach (string filePath in internalListSO.Paths)
            {
                byte[] bytes = await GameRoot.Asset.LoadAssetAsync<byte[]>(filePath);
                if (!FileManager.LoadJsonFromBytes(bytes, out ChartPackData chartPackData) ||
                    chartPackData == null)
                {
                    Debug.LogError($"加载内置谱包 {filePath} 时出错，请检查。");
                    throw new Exception();
                }

                int d0Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.KuiXing);
                int d1Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.QiMing);
                int d2Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.TianShu);
                int d3Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.WuYin);
                int dNullCount = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == null);

                if (d0Count > 1 || d1Count > 1 || d2Count > 1 || d3Count > 1)
                {
                    Debug.LogError($"内置谱包 {filePath} 某个难度拥有多个谱面，请检查。");
                    throw new Exception();
                }

                if (d0Count == 0 || d1Count == 0 || d2Count == 0 || d3Count == 0)
                {
                    Debug.LogWarning($"内置谱包 {filePath} 某个难度缺少谱面，发布版本请完善。");
                }

                if (dNullCount > 0)
                {
                    Debug.LogWarning($"内置谱包 {filePath} 包含未指定难度的冗余谱面，发布版本请删除。");
                }

                Debug.Log($"已加载内置谱包 {filePath}");
                ChartPacks.Add(new ChartPack(chartPackData, true, Path.GetDirectoryName(filePath)));
            }

            // 加载玩家谱面
            IEnumerable<string> filePaths =
                Directory.EnumerateFiles(PlayerChartPacksFolderPath, ChartPackFileName, SearchOption.AllDirectories);
            foreach (string filePath in filePaths)
            {
                byte[] bytes = await GameRoot.Asset.LoadAssetAsync<byte[]>(filePath);
                if (!FileManager.LoadJsonFromBytes(bytes, out ChartPackData chartPackData))
                {
                    Debug.LogWarning($"加载玩家谱包 {filePath} 时出错，相关加载将被跳过。");
                    continue;
                }

                if (chartPackData == null)
                {
                    Debug.LogError($"加载玩家谱包 {filePath} 时为 null，相关加载将被跳过。");
                    continue;
                }

                int d0Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.KuiXing);
                int d1Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.QiMing);
                int d2Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.TianShu);
                int d3Count = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == ChartDifficulty.WuYin);
                int dNullCount = chartPackData.ChartMetaDatas.Count(cmd => cmd.Difficulty == null);
                if (d0Count > 1 || d1Count > 1 || d2Count > 1 || d3Count > 1)
                {
                    Debug.LogError($"玩家谱包 {filePath} 某个难度拥有多个谱面，相关加载将被跳过。");
                    continue;
                }

                if (d0Count == 0 || d1Count == 0 || d2Count == 0 || d3Count == 0)
                {
                    Debug.LogWarning($"玩家谱包 {filePath} 某个难度缺少谱面。");
                }

                if (dNullCount > 0)
                {
                    Debug.LogWarning($"玩家谱包 {filePath} 包含未指定难度的冗余谱面，如果这些谱面不再编辑，可以考虑删除。");
                }

                Debug.Log($"已加载玩家谱包 {filePath}");
                ChartPacks.Add(new ChartPack(chartPackData, false, Path.GetDirectoryName(filePath)));
            }
        }

        /// <summary>
        /// 重置选定的谱包谱面并按照给定的难度选择谱包和谱面
        /// </summary>
        /// <param name="index">谱包下标</param>
        /// <param name="difficulty">选择此难度的谱面，为 null 或找不到对应难度时将把谱面设为 null</param>
        /// <returns>自动设置选定谱包字段，如果给定了 difficulty，还将设置选定谱面字段</returns>
        public void SelectChartPack(int index, ChartDifficulty? difficulty = null)
        {
            SelectChartPack(ChartPacks[index], difficulty);
        }

        /// <summary>
        /// 重置选定的谱包谱面并按照给定的难度选择谱包和谱面
        /// </summary>
        /// <param name="chartPack">谱包实例</param>
        /// <param name="difficulty">选择此难度的谱面，为 null 或找不到对应难度时将把谱面设为 null</param>
        public async void SelectChartPack(ChartPack chartPack, ChartDifficulty? difficulty = null)
        {
            SelectedChartPack = chartPack;
            SelectedChart = null;
            Charts.Clear();

            string chartPackPath = chartPack.ChartPackFolderPath; // 谱包文件夹的路径

            foreach (var chartMetaData in chartPack.ChartPackData.ChartMetaDatas)
            {
                string chartFilePath = Path.Combine(chartPackPath, chartMetaData.FilePath);
                try
                {
                    byte[] bytes = await GameRoot.Asset.LoadAssetAsync<byte[]>(chartFilePath);
                    if (!FileManager.LoadJsonFromBytes(bytes, out ChartData chartData))
                    {
                        throw new Exception("LoadJsonFromBytes 失败");
                    }

                    CyanStars.Chart.Chart chart = new CyanStars.Chart.Chart(chartData, chartMetaData);
                    if (difficulty != null && chartMetaData.Difficulty == difficulty)
                    {
                        SelectedChart = chart;
                    }

                    Charts.Add(chart);
                }
                catch (Exception e)
                {
                    if (chartPack.IsInternal)
                    {
                        Debug.LogError($"读取内置谱面 {chartFilePath} 时出现异常 {e}，请检查。");
                        throw;
                    }
                    else
                    {
                        Debug.LogWarning($"读取玩家谱面 {chartFilePath} 时出现异常 {e}，将跳过谱面。");
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 按照难度在已选中的谱包中选择一个谱面
        /// </summary>
        /// <param name="difficulty">难度，若不存在将把谱面设为 null</param>
        public void SelectChart(ChartDifficulty difficulty)
        {
            if (SelectedChartPack == null)
            {
                return;
            }

            SelectedChart = null;
            foreach (var chart in Charts)
            {
                if (chart.ChartMetadata.Difficulty == difficulty)
                {
                    SelectedChart = chart;
                }
            }
        }

        /// <summary>
        /// 按照下标选择一个谱面
        /// </summary>
        /// <param name="index">下标</param>
        public void SelectChart(int index)
        {
            SelectedChart = Charts[index];
        }
    }
}
