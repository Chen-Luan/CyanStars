using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CyanStars.Chart;
using CyanStars.Chart.Hashing;
using CyanStars.Framework.File.Serialization;
using CyanStars.Gameplay.MusicGame;
using CyanStars.Utils;
using UnityEditor;
using UnityEngine;

namespace CyanStars.Editor
{
    /// <summary>
    /// 内置谱面哈希生成工具
    /// </summary>
    /// <remarks>
    /// <para>内置谱包打进资源包后，运行时取不到谱面文件的路径、修改时间与大小，无法参与哈希缓存比对，
    /// 因此哈希在出包前由本工具算好，写入 <see cref="InternalChartPackListSO"/> 的各谱包条目</para>
    /// <para>本工具只写 <see cref="InternalChartPackListSO.InternalChartPackEntry.ChartHashes"/>，
    /// 手维护的谱包索引文件路径不受影响；内置谱面内容或哈希的输入范围变化后必须重新生成，
    /// 生成结果需重新构建资源包才会生效</para>
    /// <para>TODO: 尚未挂到构建前置流程，出包前需手工执行本菜单，容易漏做</para>
    /// </remarks>
    public static class InternalChartHashGenerator
    {
        [MenuItem("CyanStars工具箱/内置谱面/生成内置谱面哈希")]
        private static void GenerateInternalChartHashes()
        {
            InternalChartPackListSO chartPackList = LoadChartPackList();
            if (chartPackList == null)
                return;

            ChartHashCalculator calculator = new ChartHashCalculator();

            int writtenCount = 0;
            int failedCount = 0;
            int keptPackCount = 0;

            foreach (InternalChartPackListSO.InternalChartPackEntry packEntry in chartPackList.ChartPacks)
            {
                if (packEntry == null || string.IsNullOrEmpty(packEntry.ChartPackFilePath))
                {
                    Debug.LogError("内置谱包条目缺少谱包索引文件路径，已跳过该项。");
                    failedCount++;
                    continue;
                }

                List<InternalChartPackListSO.ChartHashEntry> hashes =
                    CollectChartPackHashes(packEntry.ChartPackFilePath, calculator, out int packFailedCount);
                failedCount += packFailedCount;

                // 一条都没算出来时保留该项原有记录，避免读取或解析临时失败把已有哈希抹掉
                if (hashes.Count == 0)
                {
                    Debug.LogWarning($"谱包未生成任何哈希，保留原有记录：{packEntry.ChartPackFilePath}");
                    keptPackCount++;
                    continue;
                }

                hashes.Sort((left, right) => string.CompareOrdinal(left.ChartFilePath, right.ChartFilePath));
                packEntry.ChartHashes = hashes;
                writtenCount += hashes.Count;
            }

            if (writtenCount == 0)
            {
                Debug.LogError("没有写入任何内置谱面哈希，资产保持原样。");
                return;
            }

            EditorUtility.SetDirty(chartPackList);
            AssetDatabase.SaveAssetIfDirty(chartPackList);

            Debug.Log($"内置谱面哈希生成完成：写入 {writtenCount} 条，失败 {failedCount} 条，" +
                      $"未改动记录的谱包 {keptPackCount} 个。");

            if (failedCount > 0)
                Debug.LogWarning("存在未生成哈希的内置谱面，这些谱面的历史成绩无法展示，需修复后重新生成。");
        }

        /// <summary>
        /// 取内置谱包列表资产
        /// </summary>
        private static InternalChartPackListSO LoadChartPackList()
        {
            InternalChartPackListSO chartPackList =
                AssetDatabase.LoadAssetAtPath<InternalChartPackListSO>(InternalChartPackListSO.AssetPath);

            if (chartPackList == null)
            {
                Debug.LogError($"无法加载内置谱包列表：{InternalChartPackListSO.AssetPath}");
                return null;
            }

            if (chartPackList.ChartPacks == null || chartPackList.ChartPacks.Count == 0)
            {
                Debug.LogError($"内置谱包列表为空：{InternalChartPackListSO.AssetPath}");
                return null;
            }

            return chartPackList;
        }

        /// <summary>
        /// 计算单个谱包内全部谱面的哈希
        /// </summary>
        /// <param name="chartPackFilePath">谱包索引文件的资源路径</param>
        /// <param name="calculator">谱面哈希计算器</param>
        /// <param name="failedCount">未能生成哈希的条数；谱包索引文件本身失败按 1 条计，谱面失败逐条计</param>
        /// <returns>哈希记录，仅含成功项；按谱面相对路径的序数顺序排列</returns>
        private static List<InternalChartPackListSO.ChartHashEntry> CollectChartPackHashes(
            string chartPackFilePath, ChartHashCalculator calculator, out int failedCount)
        {
            List<InternalChartPackListSO.ChartHashEntry> hashes =
                new List<InternalChartPackListSO.ChartHashEntry>();

            failedCount = 0;

            if (!TryLoadJson(chartPackFilePath, out ChartPackData packData))
            {
                failedCount++;
                return hashes;
            }

            string chartPackDirectory = Path.GetDirectoryName(chartPackFilePath);
            if (string.IsNullOrEmpty(chartPackDirectory))
            {
                Debug.LogError($"无法取得谱包索引文件所在目录：{chartPackFilePath}");
                failedCount++;
                return hashes;
            }

            if (packData.ChartMetaDatas == null)
            {
                Debug.LogError($"谱包未包含谱面元数据列表：{chartPackFilePath}");
                failedCount++;
                return hashes;
            }

            foreach (ChartMetaData metaData in packData.ChartMetaDatas)
            {
                if (metaData == null || string.IsNullOrEmpty(metaData.FilePath))
                {
                    Debug.LogError($"谱包中存在缺少谱面文件路径的元数据：{chartPackFilePath}");
                    failedCount++;
                    continue;
                }

                string chartFilePath = PathUtil.Combine(chartPackDirectory, metaData.FilePath);

                if (!TryLoadJson(chartFilePath, out ChartData chartData))
                {
                    failedCount++;
                    continue;
                }

                try
                {
                    ChartHash chartHash = calculator.Compute(new ChartHashSource(packData, chartData));

                    hashes.Add(new InternalChartPackListSO.ChartHashEntry
                    {
                        // 相对谱包目录的写法与谱包索引文件保持一致，谱包目录移动或改名后记录仍然对得上
                        ChartFilePath = PathUtil.Normalize(metaData.FilePath),
                        ChartHash = chartHash.ToFullText()
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"计算谱面哈希失败：{chartFilePath}，{e}");
                    failedCount++;
                }
            }

            return hashes;
        }

        /// <summary>
        /// 读取并反序列化 json 文件
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="filePath">json 文件路径</param>
        /// <param name="data">反序列化结果；失败时为 null</param>
        /// <returns>读取与解析是否都成功</returns>
        private static bool TryLoadJson<T>(string filePath, out T data) where T : class
        {
            data = null;

            string json;
            try
            {
                json = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"读取文件失败：{filePath}，{e.Message}");
                return false;
            }

            if (!JsonSerializer.TryDeserialize(json, out data) || data == null)
            {
                Debug.LogError($"解析文件失败：{filePath}");
                return false;
            }

            return true;
        }
    }
}
