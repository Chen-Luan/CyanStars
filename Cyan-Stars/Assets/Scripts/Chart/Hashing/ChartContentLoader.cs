#nullable enable

using CyanStars.Framework.File;
using CyanStars.Framework.File.Serialization;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 从磁盘读取谱包与谱面内容
    /// </summary>
    /// <remarks>
    /// 只用于哈希计算这类需要读取原始文件内容的场合：玩家谱包目录必定是本地路径，
    /// 因此读取一律走 <see cref="FileManager"/>，不经过资源系统
    /// </remarks>
    public sealed class ChartContentLoader : IChartContentLoader
    {
        private readonly FileManager fileManager;

        public ChartContentLoader(FileManager fileManager)
        {
            this.fileManager = fileManager;
        }

        public bool TryLoadChartPack(string chartPackFilePath, out ChartPackData? chartPackData)
        {
            chartPackData = null;

            if (!fileManager.TryReadTextFromPath(chartPackFilePath, out string json))
                return false;

            return JsonSerializer.TryDeserialize(json, out chartPackData);
        }

        public bool TryLoadChartData(string chartFilePath, out ChartData? chartData)
        {
            chartData = null;

            if (!fileManager.TryReadTextFromPath(chartFilePath, out string json))
                return false;

            return JsonSerializer.TryDeserialize(json, out chartData);
        }
    }
}
