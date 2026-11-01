#nullable enable

using System;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面文件与谱包索引文件的一致性凭据
    /// </summary>
    /// <remarks>
    /// 只用于判断「文件是否可能被改过」，因此取修改时间与大小两要素：
    /// 修改时间精确到秒，避免不同文件系统的时间戳精度差异造成无谓重算
    /// </remarks>
    public readonly struct ChartFileSnapshot : IEquatable<ChartFileSnapshot>
    {
        /// <summary>
        /// 文件最后修改时间，UTC Unix 秒
        /// </summary>
        public readonly long LastWriteUtcSeconds;

        /// <summary>
        /// 文件字节大小
        /// </summary>
        public readonly long LengthBytes;

        public ChartFileSnapshot(long lastWriteUtcSeconds, long lengthBytes)
        {
            LastWriteUtcSeconds = lastWriteUtcSeconds;
            LengthBytes = lengthBytes;
        }

        public bool Equals(ChartFileSnapshot other)
        {
            return LastWriteUtcSeconds == other.LastWriteUtcSeconds && LengthBytes == other.LengthBytes;
        }

        public override bool Equals(object? obj)
        {
            return obj is ChartFileSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LastWriteUtcSeconds.GetHashCode() * 397) ^ LengthBytes.GetHashCode();
            }
        }

        /// <summary>
        /// 按修改时间与大小逐项比较
        /// </summary>
        public static bool operator ==(ChartFileSnapshot left, ChartFileSnapshot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 按修改时间与大小逐项比较
        /// </summary>
        public static bool operator !=(ChartFileSnapshot left, ChartFileSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
