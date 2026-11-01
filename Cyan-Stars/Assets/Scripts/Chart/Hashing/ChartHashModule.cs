#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyanStars.Chart.Loading;
using CyanStars.Framework;
using CyanStars.Framework.File;
using CyanStars.Framework.File.AppPaths;
using CyanStars.Utils;
using UnityEngine;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希模块 — 谱面身份的解析与缓存
    /// </summary>
    /// <remarks>
    /// <para>谱包加载完成后比对缓存文件，解析出每张谱面的哈希；进入音游前重算玩家谱面哈希，
    /// 与缓存不一致或缺失时覆盖写入；内置谱面取编译时哈希，不进入缓存文件</para>
    /// <para>解析出的哈希就是成绩记录的键，展示历史成绩时按谱包与谱面元数据下标取用</para>
    /// </remarks>
    // ReSharper disable once ClassNeverInstantiated.Global // 由反射实例化
    public class ChartHashModule : BaseDataModule
    {
        private ChartHashCache cache = null!;
        private ChartHashReconciler reconciler = null!;
        private IChartHashCacheStore cacheStore = null!;
        private IInternalChartHashProvider internalHashProvider = null!;
        private IChartHashCalculator calculator = null!;
        private IChartFileSnapshotProvider snapshotProvider = null!;
        private IChartContentLoader contentLoader = null!;

        private string playerChartPacksRoot = "";

        public override void OnInit()
        {
            FileManager fileManager = GameRoot.File;

            playerChartPacksRoot = fileManager.GetAppPath(AppPathKind.PlayerChartPacks);

            calculator = new ChartHashCalculator();
            snapshotProvider = new DiskChartFileSnapshotProvider();
            contentLoader = new ChartContentLoader(fileManager);
            internalHashProvider = new InternalChartHashProvider();
            cacheStore = new ChartHashCacheStore(fileManager, fileManager.Environment.PersistentDataRoot);
            cache = new ChartHashCache(cacheStore);
            reconciler = new ChartHashReconciler(
                calculator,
                snapshotProvider,
                contentLoader,
                internalHashProvider,
                cache,
                playerChartPacksRoot);
        }

        /// <summary>
        /// 用缓存文件比对已加载的谱包，并解析出各谱面的哈希
        /// </summary>
        /// <param name="outcome">一次谱包批量加载的结果</param>
        /// <returns>比对结果</returns>
        /// <remarks>
        /// 比对是阻塞性的：成绩展示需要确定的谱面哈希，宁可延长启动时间也不要展示错配的成绩。
        /// 首次运行或缓存不可用时会全量计算玩家谱面哈希
        /// </remarks>
        public async Task<ChartHashSyncReport> ReconcileWithCacheAsync(ChartPackLoadOutcome outcome)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));

            await internalHashProvider.EnsureReadyAsync();

            ChartHashCacheLoadResult loadResult = cacheStore.Load();
            LogCacheLoadResult(loadResult);
            cache.LoadFrom(loadResult);

            ChartHashSyncReport report =
                reconciler.Reconcile(outcome.ChartPacks, outcome.UnparsableChartPackFilePaths);
            Debug.Log(report.BuildLogMessage());

            return report;
        }

        /// <summary>
        /// 进入音游前重算谱面哈希，与缓存不一致或缺失时覆盖缓存
        /// </summary>
        /// <param name="chartPack">要进入音游的谱包</param>
        /// <param name="chartMetadataIndex">谱面元数据下标</param>
        /// <remarks>
        /// <para>内置谱面直接取编译时哈希，不写缓存文件</para>
        /// <para>本方法读取并重算一份谱面，任何失败都只记录日志：缓存与哈希解析不阻断进入音游</para>
        /// <para>TODO: 校验过程中的文件读取、重算与缓存写出都是同步操作，落在进音游的加载帧上，
        /// 应改到异步或复用已加载的谱面数据</para>
        /// </remarks>
        public void VerifyAndRefreshChartHash(RuntimeChartPack chartPack, int chartMetadataIndex)
        {
            if (chartPack == null)
                throw new ArgumentNullException(nameof(chartPack));

            IReadOnlyList<ChartMetaData> chartMetaDatas = chartPack.ChartPackData.ChartMetaDatas;
            if (chartMetadataIndex < 0 || chartMetadataIndex >= chartMetaDatas.Count)
                throw new IndexOutOfRangeException($"谱面元数据下标越界：{chartMetadataIndex}");

            string chartFilePath = PathUtil.Combine(chartPack.WorkspacePath, chartMetaDatas[chartMetadataIndex].FilePath);

            if (chartPack.IsInternal)
            {
                VerifyInternalChartHash(chartPack, chartMetadataIndex, chartFilePath);
                return;
            }

            if (!ChartHashCacheKey.TryCreate(playerChartPacksRoot, chartFilePath, out string? cacheKey) || cacheKey == null)
            {
                Debug.LogWarning($"谱面文件不在玩家谱包目录内，无法校验哈希：{chartFilePath}");
                return;
            }

            string chartPackFilePath =
                PathUtil.Combine(chartPack.WorkspacePath, ChartPackDataLoader.ChartPackFileName);

            if (!contentLoader.TryLoadChartData(chartFilePath, out ChartData? chartData) || chartData == null)
            {
                Debug.LogWarning($"进入音游前无法读取谱面内容，沿用已有哈希：{chartFilePath}");
                return;
            }

            ChartHash chartHash;
            try
            {
                // bpm 组取自谱包索引文件，现读一次磁盘，保证条目凭据与哈希内容出自同一份文件
                if (!contentLoader.TryLoadChartPack(chartPackFilePath, out ChartPackData? diskPackData)
                    || diskPackData == null)
                {
                    Debug.LogWarning($"无法重新读取谱包索引文件，本次沿用内存中的 bpm 组：{chartPackFilePath}");
                    diskPackData = chartPack.ChartPackData;
                }

                chartHash = calculator.Compute(new ChartHashSource(diskPackData, chartData));
            }
            catch (Exception e)
            {
                Debug.LogError($"进入音游前重算谱面哈希失败，沿用已有哈希：{chartFilePath}\n{e.Message}");
                return;
            }

            chartPack.ResolveChartHash(chartMetadataIndex, chartHash);

            // 内容未变时只刷新文件凭据，避免下次启动无谓重算；条目完全一致时不产生写盘
            if (!snapshotProvider.TryGetSnapshot(chartFilePath, out ChartFileSnapshot chartSnapshot)
                || !snapshotProvider.TryGetSnapshot(chartPackFilePath, out ChartFileSnapshot packSnapshot))
            {
                cache.TryGet(cacheKey, out ChartHashCacheEntry? cachedEntry);
                if (cachedEntry == null || cachedEntry.ChartHash != chartHash)
                    Debug.LogWarning($"无法读取谱面或谱包索引文件的凭据，本次不更新缓存：{chartFilePath}");

                return;
            }

            cache.Set(cacheKey, new ChartHashCacheEntry(chartHash, chartSnapshot, packSnapshot));

            if (!cache.TryFlush())
                Debug.LogError($"谱面哈希缓存文件写出失败：{cacheStore.CacheFilePath}");
        }

        /// <summary>
        /// 取谱面哈希，即成绩记录的键
        /// </summary>
        /// <param name="chartPack">谱包</param>
        /// <param name="chartMetadataIndex">谱面元数据下标</param>
        /// <param name="chartHash">谱面哈希；未解析时为未赋值哈希</param>
        /// <returns>是否已有可用哈希</returns>
        /// <remarks>
        /// TODO: 选曲与结算界面尚未取用本接口，成绩展示与存档写入仍待接入
        /// </remarks>
        public bool TryGetChartHash(RuntimeChartPack chartPack, int chartMetadataIndex, out ChartHash chartHash)
        {
            if (chartPack == null)
            {
                chartHash = default;
                return false;
            }

            return chartPack.TryGetChartHash(chartMetadataIndex, out chartHash);
        }

        /// <summary>
        /// 内置谱面取编译时哈希
        /// </summary>
        private void VerifyInternalChartHash(RuntimeChartPack chartPack, int chartMetadataIndex, string chartAssetPath)
        {
            if (internalHashProvider.TryGetChartHash(chartAssetPath, out ChartHash chartHash))
            {
                chartPack.ResolveChartHash(chartMetadataIndex, chartHash);
                return;
            }

            Debug.LogWarning($"内置谱面缺少编译时哈希，该谱面的历史成绩无法展示：{chartAssetPath}");
        }

        private void LogCacheLoadResult(ChartHashCacheLoadResult loadResult)
        {
            switch (loadResult.Status)
            {
                case ChartHashCacheStatus.NotCreated:
                    Debug.Log($"谱面哈希缓存文件不存在，本次将全量计算：{cacheStore.CacheFilePath}");
                    break;
                case ChartHashCacheStatus.Unusable:
                    Debug.LogWarning($"谱面哈希缓存文件不可用，本次整体重算：{loadResult.Message}");
                    break;
                case ChartHashCacheStatus.Loaded:
                    if (!string.IsNullOrEmpty(loadResult.Message))
                        Debug.LogWarning(loadResult.Message);

                    break;
            }
        }
    }
}
