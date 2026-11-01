#nullable enable

using CyanStars.Framework.File;
using CyanStars.Framework.File.Serialization;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 从磁盘读取谱包与谱面内容
    /// </summary>
    /// <remarks>
    /// <para>只用于哈希计算这类需要批量读取原始文件内容的场合：启动比对与进入音游校验都要逐份读谱面，
    /// 走资源系统会把整批谱面对象挂进资源缓存并计入引用计数，玩家谱面数量上去后启动内存与耗时都不可控；
    /// 玩家谱包目录必定是本地路径，因此读取一律走 <see cref="FileManager"/>，不经过资源系统</para>
    /// <para>解析与 CatAsset 的原生资源转换器共用同一套 json 参数（都经
    /// <see cref="CyanStars.Framework.File.Serialization.JsonSerializer"/>），
    /// 哈希输入因此与游戏实际加载到的谱面对象一致</para>
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
