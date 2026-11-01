#nullable enable

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面内容哈希的计算契约
    /// </summary>
    public interface IChartHashCalculator
    {
        /// <summary>
        /// 计算谱面内容哈希
        /// </summary>
        /// <param name="source">谱面哈希的输入内容</param>
        /// <returns>带版本前缀的哈希</returns>
        /// <exception cref="System.ArgumentNullException">输入内容为空</exception>
        /// <exception cref="System.InvalidOperationException">内容无法编码</exception>
        ChartHash Compute(ChartHashSource source);
    }
}
