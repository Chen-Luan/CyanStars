#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;
using CyanStars.Framework.File.Serialization;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面内容哈希的计算
    /// </summary>
    /// <remarks>
    /// 先把谱面关键字段投影为 <see cref="ChartFingerprintData"/>，按项目统一的 json 格式写成单行文本，
    /// 再对文本的 UTF-8 字节取 SHA-256，最后加上版本前缀
    /// </remarks>
    public sealed class ChartHashCalculator : IChartHashCalculator
    {
        public ChartHash Compute(ChartHashSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!JsonSerializer.TrySerializeCompact(ChartFingerprintBuilder.Build(source), out string fingerprintText)
                || string.IsNullOrEmpty(fingerprintText))
            {
                throw new InvalidOperationException("谱面指纹序列化失败，无法计算谱面哈希。");
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                return ChartHash.Create(sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintText)));
            }
        }
    }
}
