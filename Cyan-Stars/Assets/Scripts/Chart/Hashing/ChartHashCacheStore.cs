#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CyanStars.Framework.File;
using CyanStars.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存文件的落盘实现
    /// </summary>
    /// <remarks>
    /// <para>文件为 UTF-8 编码的 json，位于应用数据根目录，全部玩家谱面共用一个文件</para>
    /// <para>每个条目带一个校验值，覆盖条目键、谱面哈希与两份文件凭据：改动其中任何一项都会让该条目失效并按缺失重算，
    /// 玩家手改缓存因此只有自愈能力，不具备对抗改机的作用</para>
    /// </remarks>
    public sealed class ChartHashCacheStore : IChartHashCacheStore
    {
        /// <summary>
        /// 缓存文件名
        /// </summary>
        public const string CacheFileName = "ChartHashCache.json";

        /// <summary>
        /// 缓存文件的数据格式版本
        /// </summary>
        public const int CacheVersion = 1;

        private const string CacheVersionFieldName = "CacheVersion";
        private const string HashVersionFieldName = "HashVersion";
        private const string EntriesFieldName = "Entries";

        /// <summary>
        /// 条目校验值参与摘要时的分隔符
        /// </summary>
        private const char IntegrityFieldSeparator = '|';

        /// <summary>
        /// 条目校验值的范围标记，避免与其他摘要场景的文本混淆
        /// </summary>
        private const string IntegrityScope = "chartHashCacheEntry";

        private static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();

        private readonly FileManager fileManager;
        private readonly string cacheFilePath;

        public ChartHashCacheStore(FileManager fileManager, string playerDataRoot)
        {
            this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));

            if (string.IsNullOrEmpty(playerDataRoot))
                throw new ArgumentException("应用数据根目录为空", nameof(playerDataRoot));

            cacheFilePath = PathUtil.Combine(playerDataRoot, CacheFileName);
        }

        /// <inheritdoc />
        public string CacheFilePath => cacheFilePath;

        public ChartHashCacheLoadResult Load()
        {
            if (!FileManager.IsFileExists(cacheFilePath))
                return ChartHashCacheLoadResult.NotCreated();

            if (!fileManager.TryReadTextFromPath(cacheFilePath, out string text) || string.IsNullOrEmpty(text))
                return ChartHashCacheLoadResult.Unusable($"无法读取谱面哈希缓存文件：{cacheFilePath}");

            JObject entriesToken;

            try
            {
                JObject root = JObject.Parse(text);

                JToken? versionToken = root[CacheVersionFieldName];
                if (versionToken == null || versionToken.Value<int>() != CacheVersion)
                    return ChartHashCacheLoadResult.Unusable("谱面哈希缓存文件的格式版本与当前程序不一致");

                JToken? hashVersionToken = root[HashVersionFieldName];
                if (hashVersionToken == null || hashVersionToken.Value<string>() != ChartHash.VersionPrefix)
                    return ChartHashCacheLoadResult.Unusable("谱面哈希缓存文件的哈希版本与当前程序不一致");

                JToken? entriesProperty = root[EntriesFieldName];
                if (entriesProperty == null || entriesProperty.Type != JTokenType.Object)
                    return ChartHashCacheLoadResult.Unusable("谱面哈希缓存文件缺少条目区");

                entriesToken = (JObject)entriesProperty;
            }
            catch (Exception e)
            {
                return ChartHashCacheLoadResult.Unusable($"谱面哈希缓存文件不是合法的 json 数据：{e.Message}");
            }

            return ReadEntries(entriesToken);
        }

        public bool TrySave(IReadOnlyDictionary<string, ChartHashCacheEntry> entries)
        {
            JObject entriesToken = new JObject();

            // 键按序数顺序写出，保证同一份缓存内容每次写出的文本一致，便于比对与排查
            foreach (string key in SortKeys(entries.Keys))
            {
                ChartHashCacheEntry entry = entries[key];
                JObject entryToken = new JObject();
                entry.WriteTo(entryToken);
                entryToken[ChartHashCacheEntry.IntegrityFieldName] = ComputeEntryIntegrity(key, entry);

                entriesToken[key] = entryToken;
            }

            JObject root = new JObject
            {
                [CacheVersionFieldName] = CacheVersion,
                [HashVersionFieldName] = ChartHash.VersionPrefix,
                [EntriesFieldName] = entriesToken
            };

            return fileManager.TryWriteTextToPath(cacheFilePath, root.ToString(Formatting.Indented));
        }

        /// <summary>
        /// 逐条读取条目区
        /// </summary>
        /// <remarks>字段不合法或校验值不符的条目按待重算处理，不影响其余条目</remarks>
        private static ChartHashCacheLoadResult ReadEntries(JObject entriesToken)
        {
            Dictionary<string, ChartHashCacheEntry> loaded =
                new Dictionary<string, ChartHashCacheEntry>(PathUtil.PathComparer);

            List<string> invalidKeys = new List<string>();

            foreach (JProperty property in entriesToken.Properties())
            {
                if (TryReadEntry(property.Name, property.Value, out ChartHashCacheEntry? entry) && entry != null)
                    loaded[property.Name] = entry;
                else
                    invalidKeys.Add(property.Name);
            }

            string message = invalidKeys.Count > 0
                ? $"谱面哈希缓存文件中有 {invalidKeys.Count} 个条目失效，这些谱面将重算"
                : "";

            return ChartHashCacheLoadResult.Loaded(loaded, invalidKeys, message);
        }

        /// <summary>
        /// 读取并校验单个条目
        /// </summary>
        /// <remarks>条目内容以任何方式不合法都判定为读取失败，不向外抛出异常</remarks>
        private static bool TryReadEntry(string cacheKey, JToken? token, out ChartHashCacheEntry? entry)
        {
            entry = null;

            try
            {
                if (!ChartHashCacheEntry.TryReadFrom(token, out entry) || entry == null)
                    return false;

                string? storedIntegrity = token?[ChartHashCacheEntry.IntegrityFieldName]?.Value<string>();
                if (string.IsNullOrEmpty(storedIntegrity) ||
                    storedIntegrity != ComputeEntryIntegrity(cacheKey, entry))
                {
                    entry = null;
                    return false;
                }

                return true;
            }
            catch (Exception e) when (e is JsonException or InvalidCastException or FormatException
                                       || e is InvalidOperationException)
            {
                entry = null;
                return false;
            }
        }

        /// <summary>
        /// 计算条目的校验值
        /// </summary>
        /// <remarks>摘要文本只由条目键与解析后的取值构成，与 json 的书写格式无关</remarks>
        private static string ComputeEntryIntegrity(string cacheKey, ChartHashCacheEntry entry)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(IntegrityScope).Append(IntegrityFieldSeparator)
                .Append(cacheKey).Append(IntegrityFieldSeparator)
                .Append(entry.ChartHash.ToFullText()).Append(IntegrityFieldSeparator);
            AppendSnapshot(builder, entry.ChartFileSnapshot);
            builder.Append(IntegrityFieldSeparator);
            AppendSnapshot(builder, entry.ChartPackFileSnapshot);

            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
            }
        }

        /// <summary>
        /// 追加文件凭据的固定文本
        /// </summary>
        private static void AppendSnapshot(StringBuilder builder, ChartFileSnapshot snapshot)
        {
            builder.Append(snapshot.LastWriteUtcSeconds.ToString(CultureInfo.InvariantCulture))
                .Append(IntegrityFieldSeparator)
                .Append(snapshot.LengthBytes.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 按键的序数顺序排序
        /// </summary>
        private static List<string> SortKeys(IEnumerable<string> keys)
        {
            List<string> sorted = new List<string>(keys);
            sorted.Sort(StringComparer.Ordinal);
            return sorted;
        }

        /// <summary>
        /// 摘要字节转小写十六进制文本
        /// </summary>
        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                builder.Append(HexDigits[b >> 4]);
                builder.Append(HexDigits[b & 0x0F]);
            }

            return builder.ToString();
        }
    }
}
