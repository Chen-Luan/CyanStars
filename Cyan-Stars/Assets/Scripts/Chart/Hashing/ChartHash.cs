#nullable enable

using System;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面内容哈希
    /// </summary>
    /// <remarks>
    /// <para>文本形式为版本前缀（当前为 <c>cys0_</c>）加 SHA-256 摘要的小写十六进制表示</para>
    /// <para>哈希标识的是谱面内容本身：与谱面文件的路径、文件名、json 文本的键顺序与缩进无关；
    /// 前缀随参与哈希的内容范围变化而递增，旧前缀的哈希一律视为不同谱面</para>
    /// </remarks>
    public readonly struct ChartHash : IEquatable<ChartHash>
    {
        /// <summary>
        /// 当前版本的文本前缀，同时作为参与摘要计算的版本标记
        /// </summary>
        public const string VersionPrefix = "cys0_";

        /// <summary>
        /// SHA-256 摘要的字节长度
        /// </summary>
        private const int DigestByteLength = 32;

        /// <summary>
        /// 摘要的小写十六进制长度
        /// </summary>
        private const int DigestHexLength = DigestByteLength * 2;

        private static readonly char[] HexChars = "0123456789abcdef".ToCharArray();

        /// <summary>
        /// 不含版本前缀的小写十六进制摘要，未解析出哈希时为空字符串
        /// </summary>
        public readonly string Digest;

        private ChartHash(string digest)
        {
            Digest = digest;
        }

        /// <summary>
        /// 尚未解析出哈希
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Digest);

        /// <summary>
        /// 由摘要字节创建哈希
        /// </summary>
        /// <param name="digest">SHA-256 摘要字节，长度必须为 32</param>
        /// <exception cref="ArgumentException">摘要长度不符合要求</exception>
        public static ChartHash Create(byte[] digest)
        {
            if (digest == null || digest.Length != DigestByteLength)
                throw new ArgumentException("摘要长度不符合 SHA-256 要求", nameof(digest));

            char[] hex = new char[DigestHexLength];
            for (int i = 0; i < digest.Length; i++)
            {
                hex[i * 2] = HexChars[digest[i] >> 4];
                hex[i * 2 + 1] = HexChars[digest[i] & 0x0F];
            }

            return new ChartHash(new string(hex));
        }

        /// <summary>
        /// 解析带版本前缀的哈希文本
        /// </summary>
        /// <param name="text">待解析的文本</param>
        /// <param name="chartHash">解析结果；解析失败时为未赋值哈希</param>
        /// <returns>是否为当前版本的合法哈希文本</returns>
        /// <remarks>前缀不匹配（含更高或更低版本）一律解析失败，交由调用方重算</remarks>
        public static bool TryParse(string? text, out ChartHash chartHash)
        {
            chartHash = default;

            if (string.IsNullOrEmpty(text) || !text!.StartsWith(VersionPrefix, StringComparison.Ordinal))
                return false;

            string digest = text.Substring(VersionPrefix.Length);
            if (digest.Length != DigestHexLength)
                return false;

            foreach (char c in digest)
            {
                if (Array.IndexOf(HexChars, c) < 0)
                    return false;
            }

            chartHash = new ChartHash(digest);
            return true;
        }

        /// <summary>
        /// 带版本前缀的完整哈希文本，可直接用作存档与缓存文件的键
        /// </summary>
        public string ToFullText()
        {
            return IsEmpty ? "" : VersionPrefix + Digest;
        }

        public override string ToString()
        {
            return ToFullText();
        }

        public bool Equals(ChartHash other)
        {
            return string.Equals(Digest, other.Digest, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is ChartHash other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Digest?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 按摘要内容比较两个哈希
        /// </summary>
        public static bool operator ==(ChartHash left, ChartHash right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 按摘要内容比较两个哈希
        /// </summary>
        public static bool operator !=(ChartHash left, ChartHash right)
        {
            return !left.Equals(right);
        }
    }
}
