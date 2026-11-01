#nullable enable

using System.Threading.Tasks;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 内置谱面哈希的查询通道
    /// </summary>
    public interface IInternalChartHashProvider
    {
        /// <summary>
        /// 哈希表是否已就绪
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 确保哈希表已加载
        /// </summary>
        /// <returns>是否加载成功并就绪</returns>
        Task<bool> EnsureReadyAsync();

        /// <summary>
        /// 查询内置谱面的编译时哈希
        /// </summary>
        /// <param name="chartAssetPath">谱面文件的资源路径</param>
        /// <param name="chartHash">查询结果；未收录时为未赋值哈希</param>
        /// <returns>是否查询到哈希</returns>
        /// <remarks>哈希表未就绪时一律返回 false</remarks>
        bool TryGetChartHash(string chartAssetPath, out ChartHash chartHash);
    }
}
