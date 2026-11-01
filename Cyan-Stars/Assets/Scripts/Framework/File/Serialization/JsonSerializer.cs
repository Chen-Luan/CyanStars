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
        /// <remarks>与 <see cref="TrySerialize"/> 共用一套格式参数；失败原因由调用方结合上下文记录，本方法只在异常时记录一条日志</remarks>
        public static bool TryDeserialize<T>(string json, out T? data)
        {
            data = default;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
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
