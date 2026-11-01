#nullable enable

using System.Collections.Generic;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存文件的读写通道
    /// </summary>
    public interface IChartHashCacheStore
    {
        /// <summary>
        /// 缓存文件路径，供日志与排查使用
        /// </summary>
        string CacheFilePath { get; }

        /// <summary>
        /// 读取缓存文件
        /// </summary>
        /// <returns>读取结果，含文件缺失、不可用、不可信等语义</returns>
        /// <remarks>本方法不因文件内容异常而抛出，异常一律折算为 <see cref="ChartHashCacheStatus.Unusable"/></remarks>
        ChartHashCacheLoadResult Load();

        /// <summary>
        /// 整体写出缓存文件
        /// </summary>
        /// <param name="entries">要写出的全部条目</param>
        /// <returns>是否写出成功</returns>
        /// <remarks>
        /// <para>缓存文件是一个整体，写出采用全量覆盖；写出失败不阻断游戏流程，下次启动会重算缺失部分</para>
        /// <para>TODO: 覆盖写出没有临时文件替换，进程中断可能留下半截文件，当前依赖读取侧的整体重算自愈</para>
        /// <para>TODO: 玩家谱面数量上去后全量重写代价过高，应改为增量写出</para>
        /// </remarks>
        bool TrySave(IReadOnlyDictionary<string, ChartHashCacheEntry> entries);
    }
}
