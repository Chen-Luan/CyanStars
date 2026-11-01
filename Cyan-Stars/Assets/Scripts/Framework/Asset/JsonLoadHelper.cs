#nullable enable

using System.Text;
using CyanStars.Framework.File.Serialization;
using UnityEngine;

namespace CyanStars.Framework.Asset
{
    /// <summary>
    /// .json 通用解析器：把 byte[] 或 string 形式的 json 数据解析为领域对象
    /// </summary>
    /// <remarks>
    /// 格式参数由 <see cref="JsonSerializer"/> 统一持有，本类只做入口适配：
    /// CatAsset 的原生资源转换器与直接持有文本的调用方都经此处，
    /// 保证同一份 json 无论从资源系统还是从磁盘取得，都解析成同一个领域对象
    /// </remarks>
    public static class JsonLoadHelper
    {
        /// <summary>
        /// 从 json 字节解析数据
        /// </summary>
        /// <param name="bytes">UTF-8 编码的 json 字节</param>
        /// <returns>解析结果；失败时为默认值</returns>
        public static T? LoadData<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError($"用于反序列化的 byte[] 为空，目标类型 {typeof(T).Name}。");
                return default;
            }

            return LoadData<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// 从 json 文本解析数据
        /// </summary>
        /// <param name="str">json 文本</param>
        /// <returns>解析结果；失败时为默认值</returns>
        public static T? LoadData<T>(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                Debug.LogError($"用于反序列化的 json 为空，目标类型 {typeof(T).Name}。");
                return default;
            }

            return JsonSerializer.TryDeserialize(str, out T? data) ? data : default;
        }
    }
}
