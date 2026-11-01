#nullable enable

using System;
using System.IO;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 基于本地磁盘读取文件快照的通道
    /// </summary>
    /// <remarks>
    /// 只读取文件元数据，不打开文件内容。读取失败时返回 false 且不记录日志，由调用方统一记录
    /// </remarks>
    public sealed class DiskChartFileSnapshotProvider : IChartFileSnapshotProvider
    {
        /// <inheritdoc />
        public bool TryGetSnapshot(string? absoluteFilePath, out ChartFileSnapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrEmpty(absoluteFilePath))
            {
                return false;
            }

            try
            {
                FileInfo info = new FileInfo(absoluteFilePath);
                if (!info.Exists)
                {
                    return false;
                }

                // 截断到秒，吸收不同文件系统的时间戳精度差异，避免无谓重算
                long lastWriteUtcSeconds = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeSeconds();
                snapshot = new ChartFileSnapshot(lastWriteUtcSeconds, info.Length);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
