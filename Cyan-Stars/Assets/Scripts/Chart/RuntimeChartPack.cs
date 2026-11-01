#nullable enable

using System;
using CyanStars.Chart.Hashing;

namespace CyanStars.Chart
{
    /// <summary>
    /// 运行时谱包数据
    /// </summary>
    public class RuntimeChartPack
    {
        public readonly ChartPackData ChartPackData;

        /// <summary>
        /// 是否为内置谱包，内置谱包的定数可用于计算玩家实力水平，内置谱包的定数要求为 1~20 的 float
        /// </summary>
        /// <remarks>
        /// 此值在加载时根据加载方式确定
        /// </remarks>
        public readonly bool IsInternal;

        /// <summary>
        /// 谱包工作区的绝对路径
        /// </summary>
        /// <remarks>谱包索引文件所在的目录，基于此路径读取其中的谱面和资源文件</remarks>
        public readonly string WorkspacePath;

        /// <summary>
        /// 与 <see cref="ChartPackData.ChartMetaDatas"/> 下标一一对应的谱面哈希，未解析时为未赋值哈希
        /// </summary>
        private readonly ChartHash[] chartHashes;


        public RuntimeChartPack(ChartPackData chartPackData, bool isInternal, string workspacePath)
        {
            ChartPackData = chartPackData;
            IsInternal = isInternal;
            WorkspacePath = workspacePath;
            chartHashes = new ChartHash[chartPackData.ChartMetaDatas.Count];
        }

        /// <summary>
        /// 取已解析的谱面哈希
        /// </summary>
        /// <param name="chartMetadataIndex">谱面元数据下标</param>
        /// <param name="chartHash">谱面哈希；未解析或下标越界时为未赋值哈希</param>
        /// <returns>是否已有可用哈希</returns>
        /// <remarks>内置谱包的哈希来自编译时哈希表，玩家谱包的哈希来自谱面哈希缓存文件</remarks>
        public bool TryGetChartHash(int chartMetadataIndex, out ChartHash chartHash)
        {
            chartHash = default;

            if (chartMetadataIndex < 0 || chartMetadataIndex >= chartHashes.Length)
                return false;

            ChartHash resolved = chartHashes[chartMetadataIndex];
            if (resolved.IsEmpty)
                return false;

            chartHash = resolved;
            return true;
        }

        /// <summary>
        /// 记录解析出的谱面哈希
        /// </summary>
        /// <param name="chartMetadataIndex">谱面元数据下标</param>
        /// <param name="chartHash">谱面哈希</param>
        /// <exception cref="IndexOutOfRangeException">下标越界</exception>
        /// <remarks>哈希由谱包加载后的缓存比对或进入音游前的重算填入</remarks>
        internal void ResolveChartHash(int chartMetadataIndex, ChartHash chartHash)
        {
            if (chartMetadataIndex < 0 || chartMetadataIndex >= chartHashes.Length)
                throw new IndexOutOfRangeException($"谱面哈希下标越界：{chartMetadataIndex}，谱包 {ChartPackData.Title}");

            chartHashes[chartMetadataIndex] = chartHash;
        }
    }
}

