#nullable enable

using System;
using System.IO;
using CyanStars.Utils;

namespace CyanStars.Chart.Hashing
{
    /// <summary>
    /// 谱面哈希缓存文件的键
    /// </summary>
    /// <remarks>
    /// <para>键是谱面文件相对于玩家谱包目录的路径，统一为正斜杠写法且不带前导分隔符</para>
    /// <para>用相对路径而不是绝对路径，玩家重定向谱包目录后缓存仍然有效；键里不含谱包标题等展示信息，
    /// 谱包目录被移动或重命名时按键自然失效并重算</para>
    /// </remarks>
    public static class ChartHashCacheKey
    {
        /// <summary>
        /// 取文件（或目录）相对玩家谱包目录的路径作为缓存键或缓存键前缀
        /// </summary>
        /// <param name="playerChartPacksRoot">玩家谱包目录的绝对路径</param>
        /// <param name="absolutePath">文件（或目录）的绝对路径</param>
        /// <param name="key">相对路径；目标不在玩家谱包目录内或路径无法解析时为 null</param>
        /// <returns>是否取得缓存键</returns>
        public static bool TryCreate(string playerChartPacksRoot, string absolutePath, out string? key)
        {
            key = null;

            if (string.IsNullOrEmpty(playerChartPacksRoot) || string.IsNullOrEmpty(absolutePath))
                return false;

            try
            {
                string normalizedPath = PathUtil.Normalize(Path.GetFullPath(absolutePath));
                string normalizedRoot = PathUtil.Normalize(Path.GetFullPath(playerChartPacksRoot));

                string prefix = normalizedRoot.EndsWith("/", StringComparison.Ordinal)
                    ? normalizedRoot
                    : normalizedRoot + "/";

                if (!normalizedPath.StartsWith(prefix, PathUtil.SemanticComparison))
                    return false;

                string relative = normalizedPath.Substring(prefix.Length);
                if (relative.Length == 0)
                    return false;

                key = relative;
                return true;
            }
            catch (Exception e) when (e is ArgumentException or PathTooLongException or NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断缓存键是否位于某个目录前缀之下
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="folderPrefix">由 <see cref="TryCreate"/> 得到的目录前缀</param>
        /// <returns>键是否指向该目录内的文件</returns>
        public static bool IsUnderFolder(string key, string folderPrefix)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(folderPrefix))
                return false;

            return key.StartsWith(folderPrefix + "/", PathUtil.SemanticComparison);
        }
    }
}
