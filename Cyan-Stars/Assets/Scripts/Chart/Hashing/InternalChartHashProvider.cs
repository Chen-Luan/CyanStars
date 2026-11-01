#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CatAsset.Runtime;
using CyanStars.Framework;
using CyanStars.Gameplay.MusicGame;
using CyanStars.Utils;
using UnityEngine;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 内置谱面哈希表的加载与查询实现
    /// </summary>
    /// <remarks>哈希随 <see cref="InternalChartPackListSO"/> 的各内置谱包条目提供，由编辑器生成</remarks>
    public sealed class InternalChartHashProvider : IInternalChartHashProvider
    {
        private readonly Dictionary<string, ChartHash> chartHashes =
            new Dictionary<string, ChartHash>(PathUtil.PathComparer);

        /// <inheritdoc />
        public bool IsReady { get; private set; }

        public async Task<bool> EnsureReadyAsync()
        {
            if (IsReady)
                return true;

            using (AssetHandler<InternalChartPackListSO> handler =
                   await GameRoot.Asset.LoadAssetAsync<InternalChartPackListSO>(InternalChartPackListSO.AssetPath))
            {
                InternalChartPackListSO? chartPackList = handler.Asset;
                if (chartPackList == null)
                {
                    Debug.LogError($"内置谱包列表加载失败：{InternalChartPackListSO.AssetPath}");
                    return false;
                }

                return Populate(chartPackList);
            }
        }

        /// <inheritdoc />
        public bool TryGetChartHash(string chartAssetPath, out ChartHash chartHash)
        {
            chartHash = default;

            if (!IsReady || string.IsNullOrEmpty(chartAssetPath))
                return false;

            return chartHashes.TryGetValue(PathUtil.Normalize(chartAssetPath), out chartHash);
        }

        /// <summary>
        /// 按谱包条目装入哈希记录
        /// </summary>
        /// <remarks>
        /// 谱面地址由谱包索引文件所在目录与记录里的相对路径拼出，与运行时按谱包工作区拼出的写法一致；
        /// 一条记录都没有时视为哈希表未生成，此时内置谱面没有可用哈希，历史成绩无法展示
        /// </remarks>
        private bool Populate(InternalChartPackListSO chartPackList)
        {
            chartHashes.Clear();

            int invalidCount = 0;

            foreach (InternalChartPackListSO.InternalChartPackEntry packEntry in chartPackList.ChartPacks)
            {
                if (packEntry == null || string.IsNullOrEmpty(packEntry.ChartPackFilePath)
                    || packEntry.ChartHashes == null)
                {
                    invalidCount++;
                    continue;
                }

                string? chartPackFolder = Path.GetDirectoryName(packEntry.ChartPackFilePath);
                if (string.IsNullOrEmpty(chartPackFolder))
                {
                    invalidCount++;
                    continue;
                }

                foreach (InternalChartPackListSO.ChartHashEntry hashEntry in packEntry.ChartHashes)
                {
                    if (hashEntry == null || string.IsNullOrEmpty(hashEntry.ChartFilePath)
                        || !ChartHash.TryParse(hashEntry.ChartHash, out ChartHash chartHash))
                    {
                        invalidCount++;
                        continue;
                    }

                    chartHashes[PathUtil.Combine(chartPackFolder, hashEntry.ChartFilePath)] = chartHash;
                }
            }

            if (invalidCount > 0)
                Debug.LogWarning($"内置谱包列表中有 {invalidCount} 条哈希记录不合法，对应内置谱面没有历史成绩可展示。");

            if (chartHashes.Count == 0)
            {
                Debug.LogError($"内置谱面哈希表为空：{InternalChartPackListSO.AssetPath}，" +
                               "请用编辑器菜单「生成内置谱面哈希」写入并重新构建资源包。");
                return false;
            }

            IsReady = true;
            return true;
        }
    }
}
