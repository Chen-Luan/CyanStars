#nullable enable

using System;
using System.Collections.Generic;
using CyanStars.Chart.Loading;
using CyanStars.Utils;
using UnityEngine;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存与本地文件的比对器
    /// </summary>
    /// <remarks>
    /// <para>比对规则：</para>
    /// <para>· 缓存键、谱面文件与谱包索引文件的修改时间和大小全部一致时沿用缓存哈希；</para>
    /// <para>· 任一凭据不一致或缓存条目字段不合法、校验值不符时重算该谱面并覆盖条目；缓存中没有记录时计算并写入条目；</para>
    /// <para>· 谱包索引文件读不到时跳过该谱包，删除其目录下的条目；谱面文件读不到或解析失败时跳过该谱面，删除其条目；</para>
    /// <para>· 缓存中存在但本地已找不到的键一律删除。</para>
    /// <para>内置谱面不进入缓存文件，其哈希取自编译时哈希表；本类只在玩家谱包目录内做读写</para>
    /// </remarks>
    public sealed class ChartHashReconciler
    {
        private readonly IChartHashCalculator calculator;
        private readonly IChartFileSnapshotProvider snapshotProvider;
        private readonly IChartContentLoader contentLoader;
        private readonly IInternalChartHashProvider internalHashProvider;
        private readonly ChartHashCache cache;
        private readonly string playerChartPacksRoot;

        public ChartHashReconciler(
            IChartHashCalculator calculator,
            IChartFileSnapshotProvider snapshotProvider,
            IChartContentLoader contentLoader,
            IInternalChartHashProvider internalHashProvider,
            ChartHashCache cache,
            string playerChartPacksRoot)
        {
            this.calculator = calculator;
            this.snapshotProvider = snapshotProvider;
            this.contentLoader = contentLoader;
            this.internalHashProvider = internalHashProvider;
            this.cache = cache;
            this.playerChartPacksRoot = playerChartPacksRoot;
        }

        /// <summary>
        /// 比对已加载的谱包与缓存文件，并把解析出的哈希写回各谱包
        /// </summary>
        /// <param name="chartPacks">本次已加载成功的谱包</param>
        /// <param name="unparsableChartPackFilePaths">索引文件解析失败的谱包索引文件绝对路径</param>
        /// <returns>比对结果</returns>
        public ChartHashSyncReport Reconcile(
            IReadOnlyList<RuntimeChartPack> chartPacks,
            IReadOnlyList<string> unparsableChartPackFilePaths)
        {
            List<string> validCacheKeys = new List<string>();
            List<string> removedCacheKeys = new List<string>();

            int reused = 0;
            int added = 0;
            int refreshed = 0;
            int failed = 0;
            int internalMissing = 0;

            foreach (RuntimeChartPack chartPack in chartPacks)
            {
                if (chartPack.IsInternal)
                {
                    internalMissing += ResolveInternalChartPack(chartPack);
                    continue;
                }

                string chartPackFilePath =
                    PathUtil.Combine(chartPack.WorkspacePath, ChartPackDataLoader.ChartPackFileName);

                if (!snapshotProvider.TryGetSnapshot(chartPackFilePath, out ChartFileSnapshot packSnapshot))
                {
                    Debug.LogWarning($"无法读取谱包索引文件，已跳过该谱包并清理其缓存条目：{chartPackFilePath}");
                    PurgeCacheUnderFolder(chartPack.WorkspacePath, removedCacheKeys);
                    failed++;
                    continue;
                }

                ReconcilePlayerChartPack(chartPack, chartPackFilePath, packSnapshot, validCacheKeys, removedCacheKeys,
                    ref reused, ref added, ref refreshed, ref failed);
            }

            // 解析失败的谱包：其目录下的条目不再有机会被比对，直接删除
            foreach (string chartPackFilePath in unparsableChartPackFilePaths)
            {
                Debug.LogWarning($"谱包索引文件解析失败，已跳过该谱包并清理其缓存条目：{chartPackFilePath}");
                PurgeCacheUnderFolder(GetChartPackFolder(chartPackFilePath), removedCacheKeys);
                failed++;
            }

            // 缓存中有记录但本地已找不到的谱面
            removedCacheKeys.AddRange(RemoveStaleEntries(validCacheKeys));

            ChartHashCacheWriteState writeState;
            if (!cache.IsDirty)
                writeState = ChartHashCacheWriteState.NotRequired;
            else
                writeState = cache.TryFlush()
                    ? ChartHashCacheWriteState.Saved
                    : ChartHashCacheWriteState.Failed;

            return new ChartHashSyncReport(
                reused,
                added,
                refreshed,
                removedCacheKeys.Count,
                failed,
                internalMissing,
                writeState,
                removedCacheKeys);
        }

        /// <summary>
        /// 逐张比对玩家谱面
        /// </summary>
        private void ReconcilePlayerChartPack(
            RuntimeChartPack chartPack,
            string chartPackFilePath,
            ChartFileSnapshot packSnapshot,
            List<string> validCacheKeys,
            List<string> removedCacheKeys,
            ref int reused,
            ref int added,
            ref int refreshed,
            ref int failed)
        {
            IReadOnlyList<ChartMetaData> chartMetaDatas = chartPack.ChartPackData.ChartMetaDatas;

            // 参与哈希的 bpm 组取自谱包索引文件，需要计算时现读一次磁盘，
            // 保证条目里记录的凭据与算出哈希所用的内容出自同一份文件
            ChartPackData? diskPackData = null;
            bool diskPackDataResolved = false;

            for (int index = 0; index < chartMetaDatas.Count; index++)
            {
                string chartFilePath = PathUtil.Combine(chartPack.WorkspacePath, chartMetaDatas[index].FilePath);

                if (!ChartHashCacheKey.TryCreate(playerChartPacksRoot, chartFilePath, out string? cacheKey))
                {
                    Debug.LogError($"谱面文件不在玩家谱包目录内，无法纳入哈希缓存：{chartFilePath}");
                    failed++;
                    continue;
                }

                validCacheKeys.Add(cacheKey!);

                if (!snapshotProvider.TryGetSnapshot(chartFilePath, out ChartFileSnapshot chartSnapshot))
                {
                    Debug.LogWarning($"无法读取谱面文件，已跳过该谱面并清理其缓存条目：{chartFilePath}");
                    if (cache.Remove(cacheKey!))
                        removedCacheKeys.Add(cacheKey!);

                    failed++;
                    continue;
                }

                cache.TryGet(cacheKey!, out ChartHashCacheEntry? cachedEntry);

                if (cachedEntry != null
                    && cachedEntry.ChartFileSnapshot == chartSnapshot
                    && cachedEntry.ChartPackFileSnapshot == packSnapshot)
                {
                    chartPack.ResolveChartHash(index, cachedEntry.ChartHash);
                    reused++;
                    continue;
                }

                if (!diskPackDataResolved)
                {
                    diskPackDataResolved = true;
                    if (!contentLoader.TryLoadChartPack(chartPackFilePath, out diskPackData) || diskPackData == null)
                        Debug.LogWarning($"无法重新读取谱包索引文件，本次沿用内存中的 bpm 组：{chartPackFilePath}");
                }

                if (!TryComputeChartHash(chartFilePath, diskPackData ?? chartPack.ChartPackData, out ChartHash chartHash))
                {
                    if (cache.Remove(cacheKey!))
                        removedCacheKeys.Add(cacheKey!);

                    failed++;
                    continue;
                }

                cache.Set(cacheKey!, new ChartHashCacheEntry(chartHash, chartSnapshot, packSnapshot));
                chartPack.ResolveChartHash(index, chartHash);

                if (cachedEntry == null)
                    added++;
                else
                    refreshed++;
            }
        }

        /// <summary>
        /// 取内置谱面的编译时哈希
        /// </summary>
        /// <returns>未收录哈希的内置谱面数</returns>
        private int ResolveInternalChartPack(RuntimeChartPack chartPack)
        {
            int missing = 0;
            IReadOnlyList<ChartMetaData> chartMetaDatas = chartPack.ChartPackData.ChartMetaDatas;

            for (int index = 0; index < chartMetaDatas.Count; index++)
            {
                string chartAssetPath = PathUtil.Combine(chartPack.WorkspacePath, chartMetaDatas[index].FilePath);

                if (internalHashProvider.TryGetChartHash(chartAssetPath, out ChartHash chartHash))
                {
                    chartPack.ResolveChartHash(index, chartHash);
                    continue;
                }

                Debug.LogWarning($"内置谱面缺少编译时哈希：{chartAssetPath}，该谱面的历史成绩无法展示，需重新生成内置谱面哈希表。");
                missing++;
            }

            return missing;
        }

        /// <summary>
        /// 读取谱面内容并计算哈希
        /// </summary>
        /// <param name="chartFilePath">谱面文件绝对路径</param>
        /// <param name="chartPackData">提供 bpm 组的谱包数据</param>
        /// <param name="chartHash">计算结果；失败时为未赋值哈希</param>
        /// <returns>是否取得哈希</returns>
        /// <remarks>读取失败、解析失败或内容无法编码时记录日志并返回 false，业务上跳过该谱面</remarks>
        private bool TryComputeChartHash(string chartFilePath, ChartPackData chartPackData, out ChartHash chartHash)
        {
            chartHash = default;

            if (!contentLoader.TryLoadChartData(chartFilePath, out ChartData? chartData) || chartData == null)
            {
                Debug.LogWarning($"谱面文件解析失败，已跳过该谱面：{chartFilePath}");
                return false;
            }

            try
            {
                chartHash = calculator.Compute(new ChartHashSource(chartPackData, chartData));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"计算谱面哈希时出现异常，已跳过该谱面：{chartFilePath}\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除位于某个谱包目录下的全部缓存条目
        /// </summary>
        /// <param name="chartPackFolderPath">谱包目录的绝对路径</param>
        /// <param name="removedCacheKeys">收集被删除的缓存键</param>
        /// <returns>被删除的条目数</returns>
        private int PurgeCacheUnderFolder(string chartPackFolderPath, List<string> removedCacheKeys)
        {
            if (!ChartHashCacheKey.TryCreate(playerChartPacksRoot, chartPackFolderPath, out string? folderPrefix)
                || folderPrefix == null)
                return 0;

            return cache.RemoveUnderFolder(folderPrefix, removedCacheKeys);
        }

        /// <summary>
        /// 取谱包索引文件所在的目录
        /// </summary>
        private static string GetChartPackFolder(string chartPackFilePath)
        {
            return System.IO.Path.GetDirectoryName(chartPackFilePath) ?? chartPackFilePath;
        }

        /// <summary>
        /// 删除不在有效键集合内的条目
        /// </summary>
        private List<string> RemoveStaleEntries(List<string> validCacheKeys)
        {
            List<string> removedCacheKeys = new List<string>();
            cache.RemoveNotIn(validCacheKeys, removedCacheKeys);
            return removedCacheKeys;
        }
    }
}
