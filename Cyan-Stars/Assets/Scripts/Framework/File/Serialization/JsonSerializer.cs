#nullable enable

using System;
using System.Globalization;
using CyanStars.Utils.JsonSerialization;
using Newtonsoft.Json;
using UnityEngine;

namespace CyanStars.Framework.File.Serialization
{
    /// <summary>
    /// Json 序列化工具
    /// </summary>
    public static class JsonSerializer
    {
        /// <summary>
        /// 序列化格式参数
        /// </summary>
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Converters = JsonConverters.Converters
        };

        /// <summary>
        /// UTF-8 BOM 字符
        /// </summary>
        private const char ByteOrderMark = '\uFEFF';

        /// <summary>
        /// 序列化对象到 json 字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="json">序列化结果，失败时为空字符串</param>
        /// <returns>是否成功序列化</returns>
        public static bool TrySerialize(object obj, out string json)
        {
            try
            {
                json = JsonConvert.SerializeObject(obj, Settings);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"序列化时出现异常：{e}");
                json = "";
                return false;
            }
        }

        /// <summary>
        /// 反序列化 json 字符串到对象
        /// </summary>
        /// <param name="json">json 文本</param>
        /// <param name="data">反序列化结果；失败时为默认值</param>
        /// <returns>是否成功反序列化</returns>
        /// <remarks>
        /// 开头的 UTF-8 BOM 在此剔除：资源系统按字节取出内容再解码，带 BOM 的文件会留下一个不可见字符，
        /// 而文本读取会自动去掉它，不抹平会让同一份文件在两条通道下一个能解析一个不能；
        /// 失败原因由调用方结合上下文记录，本方法只在异常时记录一条日志
        /// </remarks>
        public static bool TryDeserialize<T>(string json, out T? data)
        {
            data = default;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                if (json[0] == ByteOrderMark)
                    json = json.Substring(1);

                data = JsonConvert.DeserializeObject<T>(json, Settings);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"反序列化 {typeof(T).Name} 时出现异常：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 序列化对象到单行 json 文本
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="json">序列化结果，失败时为空字符串</param>
        /// <returns>是否成功序列化</returns>
        /// <remarks>用于内容指纹与校验文本这类需要稳定、紧凑文本的场合；落盘存档与谱包仍用 <see cref="TrySerialize"/></remarks>
        public static bool TrySerializeCompact(object obj, out string json)
        {
            try
            {
                json = JsonConvert.SerializeObject(obj, Formatting.None, Settings);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"序列化单行 json 时出现异常：{e}");
                json = "";
                return false;
            }
        }
    }
}
