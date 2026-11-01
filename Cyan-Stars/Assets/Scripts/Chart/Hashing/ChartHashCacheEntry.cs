#nullable enable

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存文件中的单个条目
    /// </summary>
    /// <remarks>
    /// <para>缓存文件以谱面文件相对谱包根目录的路径为键，本类表示对应的值</para>
    /// <para>类中的字段名常量即缓存文件的字段契约，读写两侧都必须经由这些常量取字段名</para>
    /// <para>条目同时记录谱面文件与谱包索引文件的凭据，任一份变化都意味着需要重算哈希</para>
    /// </remarks>
    public sealed class ChartHashCacheEntry
    {
        /// <summary>
        /// 谱面哈希字段名
        /// </summary>
        public const string HashFieldName = "ChartHash";

        /// <summary>
        /// 谱面文件最后修改时间的字段名
        /// </summary>
        public const string ChartFileLastWriteFieldName = "ChartFileLastWriteUtcSeconds";

        /// <summary>
        /// 谱面文件字节大小的字段名
        /// </summary>
        public const string ChartFileLengthFieldName = "ChartFileLengthBytes";

        /// <summary>
        /// 谱包索引文件最后修改时间的字段名
        /// </summary>
        public const string ChartPackFileLastWriteFieldName = "ChartPackFileLastWriteUtcSeconds";

        /// <summary>
        /// 谱包索引文件字节大小的字段名
        /// </summary>
        public const string ChartPackFileLengthFieldName = "ChartPackFileLengthBytes";

        /// <summary>
        /// 条目校验值的字段名
        /// </summary>
        /// <remarks>校验值由缓存文件的读写通道负责计算与核对，本类不承担该职责</remarks>
        public const string IntegrityFieldName = "Integrity";

        /// <summary>
        /// 谱面内容哈希
        /// </summary>
        public ChartHash ChartHash { get; }

        /// <summary>
        /// 谱面文件的凭据，该文件变化即需要重算哈希
        /// </summary>
        public ChartFileSnapshot ChartFileSnapshot { get; }

        /// <summary>
        /// 谱包索引文件的凭据，该文件变化即需要重算哈希
        /// </summary>
        public ChartFileSnapshot ChartPackFileSnapshot { get; }

        /// <summary>
        /// 创建缓存条目
        /// </summary>
        /// <param name="chartHash">谱面内容哈希，不得为未赋值哈希</param>
        /// <param name="chartFileSnapshot">谱面文件的凭据</param>
        /// <param name="chartPackFileSnapshot">谱包索引文件的凭据</param>
        /// <exception cref="ArgumentException"><paramref name="chartHash"/> 为未赋值哈希</exception>
        public ChartHashCacheEntry(
            ChartHash chartHash,
            ChartFileSnapshot chartFileSnapshot,
            ChartFileSnapshot chartPackFileSnapshot)
        {
            if (chartHash.IsEmpty)
                throw new ArgumentException("哈希为空的缓存条目没有意义。", nameof(chartHash));

            ChartHash = chartHash;
            ChartFileSnapshot = chartFileSnapshot;
            ChartPackFileSnapshot = chartPackFileSnapshot;
        }

        /// <summary>
        /// 将条目写入目标对象，字段名取自本类的字段名常量
        /// </summary>
        /// <param name="target">写入目标</param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> 为 null</exception>
        public void WriteTo(JObject target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target[HashFieldName] = new JValue(ChartHash.ToFullText());
            target[ChartFileLastWriteFieldName] = new JValue(ChartFileSnapshot.LastWriteUtcSeconds);
            target[ChartFileLengthFieldName] = new JValue(ChartFileSnapshot.LengthBytes);
            target[ChartPackFileLastWriteFieldName] = new JValue(ChartPackFileSnapshot.LastWriteUtcSeconds);
            target[ChartPackFileLengthFieldName] = new JValue(ChartPackFileSnapshot.LengthBytes);
        }

        /// <summary>
        /// 从 json 片段读取缓存条目
        /// </summary>
        /// <param name="token">候选 json 片段</param>
        /// <param name="entry">读取结果；读取失败时为 null</param>
        /// <returns>是否读取出合法条目</returns>
        /// <remarks>
        /// <para>容错策略：片段为 null、不是对象、任一字段缺失、字段类型不是整数、
        /// 或哈希字段不是字符串且不符合当前版本格式，一律判定为读取失败</para>
        /// <para>本方法不抛出解析类异常，也不记录日志，失败原因由调用方结合上下文记录</para>
        /// </remarks>
        public static bool TryReadFrom(JToken? token, out ChartHashCacheEntry? entry)
        {
            entry = null;

            if (token == null || token.Type != JTokenType.Object)
                return false;

            JObject source = (JObject)token;

            try
            {
                JToken? hashField = source[HashFieldName];
                if (hashField == null || hashField.Type != JTokenType.String)
                    return false;

                if (!ChartHash.TryParse(hashField.Value<string>(), out ChartHash chartHash))
                    return false;

                if (!TryReadInteger(source, ChartFileLastWriteFieldName, out long chartFileLastWrite))
                    return false;

                if (!TryReadInteger(source, ChartFileLengthFieldName, out long chartFileLength))
                    return false;

                if (!TryReadInteger(source, ChartPackFileLastWriteFieldName, out long chartPackFileLastWrite))
                    return false;

                if (!TryReadInteger(source, ChartPackFileLengthFieldName, out long chartPackFileLength))
                    return false;

                entry = new ChartHashCacheEntry(
                    chartHash,
                    new ChartFileSnapshot(chartFileLastWrite, chartFileLength),
                    new ChartFileSnapshot(chartPackFileLastWrite, chartPackFileLength));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// 读取整数字段，字段缺失或类型不是整数时判定为失败
        /// </summary>
        private static bool TryReadInteger(JObject source, string fieldName, out long value)
        {
            value = 0;

            JToken? field = source[fieldName];
            if (field == null || field.Type != JTokenType.Integer)
                return false;

            value = field.Value<long>();
            return true;
        }
    }
}
