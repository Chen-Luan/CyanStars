#nullable enable

using System;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 一份谱面哈希的输入内容
    /// </summary>
    /// <remarks>
    /// 节拍与变速信息分布在谱包索引文件与谱面文件两处，因此哈希输入同时需要两者；
    /// 标题、曲绘、谱包介绍等展示信息不影响谱面内容，不参与哈希
    /// </remarks>
    public sealed class ChartHashSource
    {
        /// <summary>
        /// 谱面所属谱包的数据，提供 bpm 组
        /// </summary>
        public ChartPackData ChartPackData { get; }

        /// <summary>
        /// 待哈希的谱面数据
        /// </summary>
        public ChartData ChartData { get; }

        public ChartHashSource(ChartPackData chartPackData, ChartData chartData)
        {
            ChartPackData = chartPackData ?? throw new ArgumentNullException(nameof(chartPackData));
            ChartData = chartData ?? throw new ArgumentNullException(nameof(chartData));
        }
    }
}
