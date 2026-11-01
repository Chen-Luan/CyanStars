#nullable enable

using System.Collections.Generic;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 缓存文件落盘的结果
    /// </summary>
    public enum ChartHashCacheWriteState
    {
        /// <summary>本次比对没有产生变化，不需要写出</summary>
        NotRequired,

        /// <summary>变化已写出</summary>
        Saved,

        /// <summary>写出失败，本次会话内的哈希仍可用，下次启动会重算缺失部分</summary>
        Failed,
    }

    /// <summary>
    /// 一次谱面哈希缓存比对的结果
    /// </summary>
    /// <remarks>用于启动日志与问题排查，不承载业务判定</remarks>
    public sealed class ChartHashSyncReport
    {
        /// <summary>路径与文件凭据均匹配、直接沿用缓存哈希的谱面数</summary>
        public int ReusedEntryCount { get; }

        /// <summary>缓存中没有记录、新计算并写入的谱面数</summary>
        public int AddedEntryCount { get; }

        /// <summary>文件凭据变化或缓存字段不合法、重算并覆盖的谱面数</summary>
        public int RefreshedEntryCount { get; }

        /// <summary>本地已不存在或已无法解析、被删除的缓存条目数</summary>
        public int RemovedEntryCount { get; }

        /// <summary>读不到或解析失败而被跳过的谱面数，谱包级跳过按 1 计</summary>
        public int FailedChartCount { get; }

        /// <summary>未收录进内置谱面哈希表的内置谱面数</summary>
        public int InternalHashMissingCount { get; }

        /// <summary>缓存文件落盘结果</summary>
        public ChartHashCacheWriteState CacheWriteState { get; }

        /// <summary>被删除的缓存键，供日志与排查使用</summary>
        public IReadOnlyList<string> RemovedCacheKeys { get; }

        public ChartHashSyncReport(
            int reusedEntryCount,
            int addedEntryCount,
            int refreshedEntryCount,
            int removedEntryCount,
            int failedChartCount,
            int internalHashMissingCount,
            ChartHashCacheWriteState cacheWriteState,
            IReadOnlyList<string> removedCacheKeys)
        {
            ReusedEntryCount = reusedEntryCount;
            AddedEntryCount = addedEntryCount;
            RefreshedEntryCount = refreshedEntryCount;
            RemovedEntryCount = removedEntryCount;
            FailedChartCount = failedChartCount;
            InternalHashMissingCount = internalHashMissingCount;
            CacheWriteState = cacheWriteState;
            RemovedCacheKeys = removedCacheKeys;
        }

        /// <summary>
        /// 一行式摘要文本，供启动日志输出
        /// </summary>
        public string BuildLogMessage()
        {
            string writeState = CacheWriteState switch
            {
                ChartHashCacheWriteState.NotRequired => "缓存未变化",
                ChartHashCacheWriteState.Saved => "缓存已写出",
                _ => "缓存写出失败"
            };

            return $"谱面哈希比对完成：沿用 {ReusedEntryCount}，新增 {AddedEntryCount}，重算 {RefreshedEntryCount}，" +
                   $"删除 {RemovedEntryCount}，跳过 {FailedChartCount}，内置谱面缺哈希 {InternalHashMissingCount}；{writeState}。";
        }
    }
}
