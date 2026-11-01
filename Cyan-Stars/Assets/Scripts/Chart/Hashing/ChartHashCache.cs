#nullable enable

using System.Collections.Generic;
using CyanStars.Utils;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存的内存视图
    /// </summary>
    /// <remarks>
    /// <para>按缓存键索引条目，记录本次会话内的新增与删除，只在有变化时要求落盘</para>
    /// <para>持久化通道由 <see cref="IChartHashCacheStore"/> 提供，本视图不承担解析与校验职责</para>
    /// </remarks>
    public sealed class ChartHashCache
    {
        private readonly IChartHashCacheStore cacheStore;

        private readonly Dictionary<string, ChartHashCacheEntry> entries =
            new Dictionary<string, ChartHashCacheEntry>(PathUtil.PathComparer);

        public ChartHashCache(IChartHashCacheStore cacheStore)
        {
            this.cacheStore = cacheStore;
        }

        /// <summary>
        /// 本次会话内是否发生过尚未落盘的变化
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// 供缓存文件整体写出的条目视图，调用方不得在此期间修改本缓存
        /// </summary>
        public IReadOnlyDictionary<string, ChartHashCacheEntry> Entries => entries;

        /// <summary>
        /// 装入一次缓存文件的读取结果，只收录解析成功的条目
        /// </summary>
        /// <param name="loadResult">缓存文件读取结果</param>
        /// <remarks>字段解析失败的条目不装入，后续按缺失处理并重算；装入不视为变化</remarks>
        public void LoadFrom(ChartHashCacheLoadResult loadResult)
        {
            entries.Clear();

            if (loadResult.Status == ChartHashCacheStatus.Loaded)
            {
                foreach (KeyValuePair<string, ChartHashCacheEntry> pair in loadResult.Entries)
                    entries[pair.Key] = pair.Value;
            }

            IsDirty = false;
        }

        /// <summary>
        /// 把变化写回缓存文件
        /// </summary>
        /// <returns>是否已处于全部落盘的状态；写出失败时保持脏状态，下次启动重算缺失部分</returns>
        public bool TryFlush()
        {
            if (!IsDirty)
                return true;

            if (!cacheStore.TrySave(entries))
                return false;

            IsDirty = false;
            return true;
        }

        /// <summary>
        /// 按键取条目
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="entry">条目；键不存在时为 null</param>
        /// <returns>键是否存在</returns>
        public bool TryGet(string cacheKey, out ChartHashCacheEntry? entry)
        {
            return entries.TryGetValue(cacheKey, out entry);
        }

        /// <summary>
        /// 写入或覆盖一个条目
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="entry">条目</param>
        /// <remarks>内容与已有条目完全一致时不标记为脏，避免无意义的缓存文件重写</remarks>
        public void Set(string cacheKey, ChartHashCacheEntry entry)
        {
            if (entries.TryGetValue(cacheKey, out ChartHashCacheEntry? existing) && IsSameContent(existing, entry))
                return;

            entries[cacheKey] = entry;
            IsDirty = true;
        }

        /// <summary>
        /// 删除一个条目
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <returns>是否存在并被删除</returns>
        public bool Remove(string cacheKey)
        {
            if (!entries.Remove(cacheKey))
                return false;

            IsDirty = true;
            return true;
        }

        /// <summary>
        /// 删除位于某个目录前缀之下的全部条目
        /// </summary>
        /// <param name="folderPrefix">目录前缀形式的缓存键</param>
        /// <param name="removedKeys">被删除的缓存键</param>
        /// <returns>删除的条目数量</returns>
        public int RemoveUnderFolder(string folderPrefix, List<string> removedKeys)
        {
            List<string> matched = new List<string>();
            foreach (string key in entries.Keys)
            {
                if (ChartHashCacheKey.IsUnderFolder(key, folderPrefix))
                    matched.Add(key);
            }

            foreach (string key in matched)
            {
                entries.Remove(key);
                removedKeys.Add(key);
            }

            if (matched.Count > 0)
                IsDirty = true;

            return matched.Count;
        }

        /// <summary>
        /// 删除不在有效键集合内的条目
        /// </summary>
        /// <param name="validKeys">本次扫描确认存在的谱面缓存键</param>
        /// <param name="removedKeys">被删除的缓存键</param>
        /// <returns>删除的条目数量</returns>
        /// <remarks>用于清理玩家已删除或已移动出目录的谱面遗留的条目</remarks>
        public int RemoveNotIn(IEnumerable<string> validKeys, List<string> removedKeys)
        {
            HashSet<string> valid = new HashSet<string>(validKeys, PathUtil.PathComparer);

            List<string> matched = new List<string>();
            foreach (string key in entries.Keys)
            {
                if (!valid.Contains(key))
                    matched.Add(key);
            }

            foreach (string key in matched)
            {
                entries.Remove(key);
                removedKeys.Add(key);
            }

            if (matched.Count > 0)
                IsDirty = true;

            return matched.Count;
        }

        /// <summary>
        /// 判断两个条目的哈希与两份文件凭据是否完全一致
        /// </summary>
        private static bool IsSameContent(ChartHashCacheEntry left, ChartHashCacheEntry right)
        {
            return left.ChartHash == right.ChartHash
                   && left.ChartFileSnapshot == right.ChartFileSnapshot
                   && left.ChartPackFileSnapshot == right.ChartPackFileSnapshot;
        }
    }
}
