#nullable enable

using System.Collections.Generic;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存文件的读取状态
    /// </summary>
    public enum ChartHashCacheStatus
    {
        /// <summary>文件可按当前格式解读，条目已逐个校验，失效条目见 <c>InvalidEntryKeys</c></summary>
        Loaded,

        /// <summary>缓存文件不存在，通常出现在首次运行</summary>
        NotCreated,

        /// <summary>文件读不出、不是合法 json、或版本不符，全部条目不可信</summary>
        Unusable,
    }

    /// <summary>
    /// 谱面哈希缓存文件的读取结果
    /// </summary>
    /// <remarks>
    /// <para><see cref="ChartHashCacheStatus.Unusable"/> 要求整体重算：缓存文件已经无法按当前格式解读</para>
    /// <para><see cref="InvalidEntryKeys"/> 只影响单个条目：字段不合法或条目校验值不符的条目按缺失处理并重算，
    /// 其余条目照常使用</para>
    /// </remarks>
    public sealed class ChartHashCacheLoadResult
    {
        /// <summary>
        /// 读取结果状态
        /// </summary>
        public ChartHashCacheStatus Status { get; }

        /// <summary>
        /// 可用的缓存条目，键为谱面文件的相对路径
        /// </summary>
        public IReadOnlyDictionary<string, ChartHashCacheEntry> Entries { get; }

        /// <summary>
        /// 字段解析失败、需要重算哈希的条目键
        /// </summary>
        public IReadOnlyList<string> InvalidEntryKeys { get; }

        /// <summary>
        /// 供日志使用的原因描述，无描述时为空字符串
        /// </summary>
        public string Message { get; }

        private ChartHashCacheLoadResult(
            ChartHashCacheStatus status,
            IReadOnlyDictionary<string, ChartHashCacheEntry> entries,
            IReadOnlyList<string> invalidEntryKeys,
            string? message)
        {
            Status = status;
            Entries = entries;
            InvalidEntryKeys = invalidEntryKeys;
            Message = message ?? "";
        }

        /// <summary>
        /// 构造解析成功的结果
        /// </summary>
        /// <param name="entries">可用的缓存条目</param>
        /// <param name="invalidEntryKeys">需要重算哈希的条目键</param>
        /// <param name="message">供日志使用的原因描述，可为 null</param>
        public static ChartHashCacheLoadResult Loaded(
            IReadOnlyDictionary<string, ChartHashCacheEntry> entries,
            IReadOnlyList<string> invalidEntryKeys,
            string? message)
        {
            return new ChartHashCacheLoadResult(
                ChartHashCacheStatus.Loaded,
                entries,
                invalidEntryKeys,
                message);
        }

        /// <summary>
        /// 构造缓存文件不存在的结果
        /// </summary>
        public static ChartHashCacheLoadResult NotCreated()
        {
            return new ChartHashCacheLoadResult(
                ChartHashCacheStatus.NotCreated,
                new Dictionary<string, ChartHashCacheEntry>(),
                new List<string>(),
                null);
        }

        /// <summary>
        /// 构造缓存文件整体不可用的结果
        /// </summary>
        /// <param name="message">供日志使用的原因描述</param>
        public static ChartHashCacheLoadResult Unusable(string message)
        {
            return new ChartHashCacheLoadResult(
                ChartHashCacheStatus.Unusable,
                new Dictionary<string, ChartHashCacheEntry>(),
                new List<string>(),
                message);
        }
    }
}
