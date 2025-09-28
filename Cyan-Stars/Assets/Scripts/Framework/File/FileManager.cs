using UnityEngine;
using SimpleFileBrowser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CyanStars.Chart;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CyanStars.Framework.File
{
    /// <summary>
    /// 文件管理器，基于 Unity Simple File Browser 实现
    /// 提供统一的文件和文件夹选择对话框接口
    /// </summary>
    public class FileManager : BaseManager
    {
        [SerializeField]
        private UISkin skin;

        public readonly FileBrowser.Filter ChartFilter = new FileBrowser.Filter("谱面文件", ".json");
        public readonly FileBrowser.Filter SpriteFilter = new FileBrowser.Filter("图片", ".jpg", ".png");
        public readonly FileBrowser.Filter AudioFilter = new FileBrowser.Filter("音频", ".mp3", ".wav", ".ogg");

        public override int Priority { get; }

        public enum PathType
        {
            PersistentDataPath,
            StreamingAssets,
            DataPath
        }

        /// <summary>
        /// 自定义 JsonConverter 列表
        /// </summary>
        private static readonly IList<JsonConverter> Converters = new List<JsonConverter>
        {
            new ColorConverter(), new ChartNoteDataReadConverter(), new ChartTrackDataReadConverter()
        };


        /// <summary>
        /// 管理器初始化，在此处对 FileBrowser 进行全局配置
        /// </summary>
        public override void OnInit()
        {
            // 设置颜色主题
            FileBrowser.Skin = skin;

            // 显示所有后缀的文件，包括默认排除的 .lnk 和 .tmp
            FileBrowser.SetExcludedExtensions();

            // 添加侧边栏快速链接
            FileBrowser.AddQuickLink("游戏数据目录", Application.persistentDataPath, null);
            FileBrowser.AddQuickLink("桌面", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), null);

            Debug.Log("FileManager Initialized.");
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        #region --- Public API: 文件/文件夹的加载/保存操作弹窗 ---

        public void CreateFolder(PathType pathType, string relativePath)
        {
            // 获取基础路径
            string basePath = GetBasePath(pathType);
            if (string.IsNullOrEmpty(basePath))
            {
                Debug.LogError("Unsupported PathType.");
                return;
            }

            // 组合成完整路径
            string fullPath = Path.Combine(basePath, relativePath);

            // 检查路径是否为空
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Debug.LogError("Folder path cannot be empty.");
                return;
            }

            try
            {
                // 如果文件夹不存在，则创建它
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Debug.Log($"Successfully created folder at: {fullPath}");
                }
                else
                {
                    Debug.LogWarning($"Folder already exists at: {fullPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create folder: {e.Message}");
            }
        }


        /// <summary>
        /// 获取单个文件的路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调</param>
        /// <param name="onCancel">玩家取消的回调</param>
        /// <param name="title">窗口标题</param>
        /// <param name="showAllFilesFilter">是否允许玩家选择任意后缀的文件</param>
        /// <param name="filters">依据后缀筛选文件</param>
        /// <param name="defaultFilter">默认筛选后缀名</param>
        public void GetFilePath(Action<string> onSuccess,
            Action onCancel = null,
            string title = "打开文件",
            bool showAllFilesFilter = false,
            FileBrowser.Filter[] filters = null,
            string defaultFilter = null)
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) =>
            {
                if (paths.Length > 0)
                {
                    onSuccess?.Invoke(paths[0]);
                }
            };

            FileBrowser.OnCancel cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.SetFilters(showAllFilesFilter, filters);
            FileBrowser.SetDefaultFilter(defaultFilter);
            FileBrowser.ShowLoadDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Files, false, null, null, title, "选择");
        }

        /// <summary>
        /// 获取多个文件的路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调，参数为文件路径数组</param>
        /// <param name="onCancel">玩家取消的回调</param>
        /// <param name="title">窗口标题</param>
        /// <param name="showAllFilesFilter">是否允许玩家选择任意后缀的文件</param>
        /// <param name="filters">依据后缀筛选文件</param>
        /// <param name="defaultFilter">默认筛选后缀名</param>
        public void GetMultipleFilePaths(Action<string[]> onSuccess,
            Action onCancel = null,
            string title = "打开文件",
            bool showAllFilesFilter = false,
            FileBrowser.Filter[] filters = null,
            string defaultFilter = null)
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) => { onSuccess?.Invoke(paths); };

            FileBrowser.OnCancel cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.SetFilters(showAllFilesFilter, filters);
            FileBrowser.SetDefaultFilter(defaultFilter);
            FileBrowser.ShowLoadDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Files, true, null, null, title, "选择");
        }

        /// <summary>
        /// 获取要加载的文件夹路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调</param>
        /// <param name="onCancel">玩家取消的回调</param>
        /// <param name="title">窗口标题</param>
        public void GetLoadFolderPath(Action<string> onSuccess,
            Action onCancel = null,
            string title = "打开文件夹")
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) =>
            {
                if (paths.Length > 0)
                {
                    onSuccess?.Invoke(paths[0]);
                }
            };

            FileBrowser.OnCancel cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.ShowLoadDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Folders, false, null, null, title, "选择");
        }

        /// <summary>
        /// 获取要保存的文件路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调</param>
        /// <param name="onCancel">玩家取消的回调</param>
        /// <param name="title">窗口标题</param>
        public void GetSaveFilePath(Action<string> onSuccess,
            Action onCancel = null,
            string title = "保存文件")
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) =>
            {
                if (paths.Length > 0)
                {
                    onSuccess?.Invoke(paths[0]);
                }
            };

            FileBrowser.OnCancel cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.ShowSaveDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Files, false, null, null, title, "选择");
        }

        /// <summary>
        /// 获取要保存到的文件夹路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调</param>
        /// <param name="onCancel">玩家取消的回调</param>
        /// <param name="title">窗口标题</param>
        public void GetSaveFolderPath(Action<string> onSuccess,
            Action onCancel = null,
            string title = "保存到文件夹")
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) =>
            {
                if (paths.Length > 0)
                {
                    onSuccess?.Invoke(paths[0]);
                }
            };

            FileBrowser.OnCancel cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.ShowSaveDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Folders, false, null, null, title, "选择");
        }

        #endregion

        #region --- Public API: Json 序列化和反序列化操作（用于读写 persistentDataPath） ---

        /// <summary>
        /// 序列化对象为 Json 文件
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="filePath">保存到的路径和文件全名</param>
        /// <returns>是否成功序列化</returns>
        public static bool SaveJson(object obj, string filePath)
        {
            try
            {
                // 设置序列化格式参数
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented,
                    Culture = CultureInfo.InvariantCulture,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Converters = Converters
                };

                // 如果目录不存在，创建目录
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(obj, settings);
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"序列化完成，文件路径：{filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"序列化时出现异常：{e}");
                return false;
            }
        }

        /// <summary>
        /// 从 Json 文件反序列化为对象
        /// </summary>
        /// <remarks>此方法使用了 IO，仅兼容 Windows 端，待弃用</remarks>
        /// <param name="filePath">要读取的文件路径</param>
        /// <param name="obj">输出的反序列化对象，若成功返回反序列化的对象，若失败则为默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>是否成功反序列化</returns>
        public static bool LoadJson<T>(string filePath, [CanBeNull] out T obj)
        {
            obj = default;
            try
            {
                // 设置反序列化格式参数
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented,
                    Culture = CultureInfo.InvariantCulture,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Converters = Converters
                };

                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogError($"未找到需要反序列化的文件：{filePath}");
                    return false;
                }

                string json = System.IO.File.ReadAllText(filePath);
                obj = JsonConvert.DeserializeObject<T>(json, settings);
                Debug.Log($"反序列化完成，从文件：{filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"反序列化时出现异常：{e}");
                return false;
            }
        }

        /// <summary>
        /// 从 byte[] 反序列化为对象
        /// </summary>
        /// <param name="bytes">包含 Json 数据的字节数组 (应为 UTF-8 编码)</param>
        /// <param name="obj">输出的反序列化对象，若成功返回反序列化的对象，若失败则为默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>是否成功反序列化</returns>
        public static bool LoadJsonFromBytes<T>(byte[] bytes, [CanBeNull] out T obj)
        {
            obj = default;
            try
            {
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogError("用于反序列化的 byte[] 为空或 null。");
                    return false;
                }

                // 设置反序列化格式参数 (与其它方法保持一致)
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented,
                    Culture = CultureInfo.InvariantCulture,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Converters = Converters
                };

                // 将 byte[] 转换为 UTF-8 字符串
                string json = System.Text.Encoding.UTF8.GetString(bytes);

                obj = JsonConvert.DeserializeObject<T>(json, settings);
                Debug.Log($"从 byte[] 反序列化完成。");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"从 byte[] 反序列化时出现异常：{e}");
                return false;
            }
        }

        #endregion


        /// <summary>
        /// 检查文件浏览器是否已经打开，并打印警告
        /// </summary>
        private bool IsBrowserOpen()
        {
            if (FileBrowser.IsOpen)
            {
                Debug.LogWarning("无法打开新的文件对话框，因为已有对话框处于打开状态。");
                return true;
            }

            return false;
        }

        public string GetBasePath(PathType pathType)
        {
            switch (pathType)
            {
                case PathType.PersistentDataPath:
                    return Application.persistentDataPath;

                case PathType.StreamingAssets:
                    return Application.streamingAssetsPath;

                case PathType.DataPath:
                    return Application.dataPath;

                default:
                    return null;
            }
        }
    }
}
