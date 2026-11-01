#nullable enable

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面文件快照的取值通道
    /// </summary>
    /// <remarks>
    /// 谱面哈希缓存以文件快照判定「文件是否变化」，该通道将取值动作从缓存比对逻辑中剥离，
    /// 使比对逻辑不直接依赖具体文件系统
    /// </remarks>
    public interface IChartFileSnapshotProvider
    {
        /// <summary>
        /// 读取文件的修改时间与大小
        /// </summary>
        /// <param name="absoluteFilePath">文件绝对路径</param>
        /// <param name="snapshot">读取成功时为文件快照；失败时为 default 值的空快照</param>
        /// <returns>读取成功返回 true；路径为空、文件不存在或无法访问时返回 false</returns>
        bool TryGetSnapshot(string? absoluteFilePath, out ChartFileSnapshot snapshot);
    }
}
