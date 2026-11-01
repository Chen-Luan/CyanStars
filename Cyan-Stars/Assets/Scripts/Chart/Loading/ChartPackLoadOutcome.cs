#nullable enable

using System;
using System.Collections.Generic;

namespace CyanStars.Chart.Loading
{
    /// <summary>
    /// 一次谱包批量加载的结果
    /// </summary>
    public sealed class ChartPackLoadOutcome
    {
        /// <summary>
        /// 加载成功的运行时谱包
        /// </summary>
        public IReadOnlyList<RuntimeChartPack> ChartPacks { get; }

        /// <summary>
        /// 索引文件读取或解析失败的谱包索引文件路径
        /// </summary>
        /// <remarks>内置谱包为资源地址，玩家谱包为绝对路径</remarks>
        public IReadOnlyList<string> UnparsableChartPackFilePaths { get; }

        public ChartPackLoadOutcome(
            IReadOnlyList<RuntimeChartPack> chartPacks,
            IReadOnlyList<string> unparsableChartPackFilePaths)
        {
            ChartPacks = chartPacks ?? throw new ArgumentNullException(nameof(chartPacks));
            UnparsableChartPackFilePaths =
                unparsableChartPackFilePaths ?? throw new ArgumentNullException(nameof(unparsableChartPackFilePaths));
        }
    }
}
