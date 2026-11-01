#nullable enable

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 读取谱包索引文件与谱面文件内容的通道
    /// </summary>
    public interface IChartContentLoader
    {
        /// <summary>
        /// 读取并解析谱包索引文件
        /// </summary>
        /// <param name="chartPackFilePath">谱包索引文件的绝对路径</param>
        /// <param name="chartPackData">解析结果；失败时为 null</param>
        /// <returns>是否解析成功</returns>
        bool TryLoadChartPack(string chartPackFilePath, out ChartPackData? chartPackData);

        /// <summary>
        /// 读取并解析谱面文件
        /// </summary>
        /// <param name="chartFilePath">谱面文件的绝对路径</param>
        /// <param name="chartData">解析结果；失败时为 null</param>
        /// <returns>是否解析成功</returns>
        bool TryLoadChartData(string chartFilePath, out ChartData? chartData);
    }
}
