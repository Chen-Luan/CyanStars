#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CyanStars.Utils;
using SimpleFileBrowser;
using UnityEngine;

namespace CyanStars.Framework.File
{
    /// <summary>
    /// 文件管理器：业务层的文件读写门户，提供文件（夹）选择对话框与文件句柄读写能力
    /// </summary>
    /// <remarks>
    /// <para>初始化时按编译和运行环境创建唯一一个 <see cref="IPlatformEnvironment"/> 实现，并创建内部的
    /// <see cref="FileHandleStore"/> 负责文件搬运；业务逻辑经 <see cref="Environment"/> 获取平台路径能力，
    /// 经本类的公开方法读写文件，不直接接触内部存储实现</para>
    /// <para>选中的文件复制进缓存并创建句柄；选中的文件夹只返回归一化后的路径</para>
    /// </remarks>
    public class FileManager : BaseManager
    {
        /// <summary>
        /// 默认缓存作用域：与具体业务无关的跨平台文件缓存，生命周期同游戏进程
        /// </summary>
        private const string DefaultCacheScope = "CrossPlatform";


        [SerializeField]
        private UISkin fileBrowserSkin = null!;

        /// <summary>
        /// 文件句柄存储引擎，只服务于本管理器
        /// </summary>
        private FileHandleStore fileHandleStore = null!;


        /// <summary>
        /// 当前平台环境
        /// </summary>
        public IPlatformEnvironment Environment { get; private set; } = null!;


        public override int Priority { get; }


        public override void OnInit()
        {
            // 创建环境，删除残余会话的临时文件
            Environment = CreatePlatformEnvironment();
            fileHandleStore = new FileHandleStore(Environment);
            fileHandleStore.Init();

            // 设置颜色主题，在文件弹窗中显示所有后缀的文件，包括默认排除的 .lnk 和 .tmp，添加快捷链接
            FileBrowser.Skin = fileBrowserSkin;
            FileBrowser.SetExcludedExtensions();
            AddQuickLink(CommonFolders.Downloads, "下载");
            AddQuickLink(CommonFolders.Desktop, "桌面");

            Debug.Log("FileManager Initialized.");
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 退出游戏时结束存储引擎并删除本次会话的临时目录
        /// </summary>
        private void OnDestroy()
        {
            fileHandleStore.Shutdown();
        }


        #region --- 外部文件的静态工具方法 ---

        /// <summary>
        /// 文件是否存在（支持普通路径和安卓 content:// 路径）
        /// </summary>
        public static bool IsFileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return FileBrowserHelpers.FileExists(path);
        }

        /// <summary>
        /// 文件夹是否存在（支持普通路径和安卓 content:// 路径）
        /// </summary>
        public static bool IsFolderExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return FileBrowserHelpers.DirectoryExists(path);
        }

        /// <summary>
        /// 取文件（夹）名（支持普通路径和安卓 content:// 路径）
        /// </summary>
        public static string GetFileOrFolderName(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : FileBrowserHelpers.GetFilename(path);
        }

        #endregion

        #region --- 从对话框获取路径 ---

        /// <summary>
        /// 获取单个文件并创建句柄
        /// </summary>
        /// <param name="onSuccess">成功获取的回调，参数为文件句柄</param>
        /// <param name="onCancel">取消时的回调</param>
        /// <param name="title">弹窗标题</param>
        /// <param name="showAllFilesFilter">是否允许玩家选择任意后缀的文件</param>
        /// <param name="filters">依据后缀筛选文件</param>
        /// <param name="defaultFilter">默认后缀过滤器的显示名</param>
        /// <param name="cacheScope">缓存作用域标识，决定缓存副本放在会话临时目录的哪个子目录下</param>
        public void OpenLoadFileHandleBrowser(
            Action<FileHandle>? onSuccess,
            Action? onCancel = null,
            string title = "打开文件",
            bool showAllFilesFilter = false,
            FileTypeFilter[]? filters = null,
            string? defaultFilter = null,
            string cacheScope = DefaultCacheScope
        )
        {
            if (IsBrowserOpen())
                return;

            // 将 Action? 转为 OnCancel?
            var cancelWrapper = onCancel != null
                ? new FileBrowser.OnCancel(onCancel)
                : null;

            FileBrowser.SetFilters(showAllFilesFilter, ConvertFilters(filters));
            FileBrowser.SetDefaultFilter(defaultFilter);
            FileBrowser.ShowLoadDialog(
                SuccessWrapper,
                cancelWrapper,
                FileBrowser.PickMode.Files,
                false,
                null,
                null,
                title,
                "选择"
            );
            return;

            void SuccessWrapper(string[] paths)
            {
                if (paths.Length == 0)
                    return;

                FileHandle? fileHandle = fileHandleStore.TryLoadFile(paths[0], cacheScope);
                if (fileHandle == null)
                {
                    Debug.LogError($"无法加载玩家选中的文件，已放弃本次选择：{paths[0]}");
                    onCancel?.Invoke();
                    return;
                }

                onSuccess?.Invoke(fileHandle);
            }
        }

        /// <summary>
        /// 获取要打开的文件夹路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调，参数为归一化后的文件夹路径</param>
        /// <param name="onCancel">取消后的回调</param>
        /// <param name="title">弹窗标题</param>
        /// <remarks>只返回路径，不复制进缓存</remarks>
        public void OpenLoadFolderPathBrowser(
            Action<string>? onSuccess,
            Action? onCancel = null,
            string title = "打开文件夹"
        )
        {
            ShowFolderPathBrowser(onSuccess, onCancel, title);
        }

        /// <summary>
        /// 获取要保存到的文件夹路径
        /// </summary>
        /// <param name="onSuccess">成功获取的回调，参数为归一化后的文件夹路径</param>
        /// <param name="onCancel">取消后的回调</param>
        /// <param name="title">弹窗标题</param>
        /// <remarks>保存目标是写入位置，不会复制到缓存区，因此安卓上可能仍是 <c>content://</c> 路径</remarks>
        public void OpenSaveFolderPathBrowser(
            Action<string>? onSuccess,
            Action? onCancel = null,
            string title = "保存到文件夹"
        )
        {
            ShowFolderPathBrowser(onSuccess, onCancel, title);
        }

        /// <summary>
        /// 打开文件夹选择对话框并返回归一化后的路径
        /// </summary>
        private void ShowFolderPathBrowser(Action<string>? onSuccess, Action? onCancel, string title)
        {
            if (IsBrowserOpen()) return;

            FileBrowser.OnSuccess successWrapper = (paths) =>
            {
                if (paths.Length == 0)
                    return;

                onSuccess?.Invoke(PathUtil.Normalize(paths[0]));
            };

            FileBrowser.OnCancel? cancelWrapper = onCancel != null ? new FileBrowser.OnCancel(onCancel) : null;

            FileBrowser.ShowLoadDialog(successWrapper, cancelWrapper,
                FileBrowser.PickMode.Folders, false, null, null, title, "选择");
        }

        #endregion

        #region --- 文件句柄读写 ---

        /// <summary>
        /// 加载、固定一份已有的文件（夹）并创建句柄
        /// </summary>
        /// <param name="pathUri">外部路径，可以是普通绝对路径或安卓 <c>content://</c> 路径</param>
        /// <param name="cacheScope">缓存作用域标识，决定缓存副本放在会话临时目录的哪个子目录下，由业务侧自定义</param>
        /// <returns>文件句柄；失败时返回 null</returns>
        /// <remarks>内容一律复制进会话缓存，加载路径不能位于会话缓存内</remarks>
        public FileHandle? TryLoadFile(string pathUri, string cacheScope)
        {
            return fileHandleStore.TryLoadFile(pathUri, cacheScope);
        }

        /// <summary>
        /// 设置句柄的保存位置
        /// </summary>
        /// <param name="fileHandle">修改此文件句柄</param>
        /// <param name="targetPath">保存时写到的绝对路径（含文件名）；传 null 或空字符串表示解绑</param>
        /// <returns>是否已经按传入的路径完成设置</returns>
        public bool TrySetSaveTarget(FileHandle fileHandle, string? targetPath)
        {
            return fileHandleStore.TrySetSaveTarget(fileHandle, targetPath);
        }

        /// <summary>
        /// 判断句柄当前是否可以读取
        /// </summary>
        /// <param name="fileHandle">待判断的句柄</param>
        /// <remarks>句柄已释放、或者临时缓存被系统清理掉时返回 false</remarks>
        public bool IsHandleReadable(FileHandle? fileHandle)
        {
            return fileHandleStore.IsHandleReadable(fileHandle);
        }

        /// <summary>
        /// 把句柄指向的内容保存到它的目标路径
        /// </summary>
        /// <param name="fileHandle">保存此句柄</param>
        /// <param name="overwrite">允许覆盖目标路径原有的文件</param>
        /// <returns>是否保存成功</returns>
        public bool TrySaveToTarget(FileHandle fileHandle, bool overwrite = false)
        {
            return fileHandleStore.TrySaveToTarget(fileHandle, overwrite);
        }

        /// <summary>
        /// 批量保存句柄
        /// </summary>
        /// <param name="fileHandles">要保存的句柄集合</param>
        /// <param name="overwrite">允许覆盖目标路径原有的文件</param>
        /// <returns>是否全部保存成功</returns>
        /// <remarks>任一项失败都不中断其余项；调用方应自行决定要不要在失败后再写引用这些资源的元数据文件</remarks>
        public bool TrySaveAll(IEnumerable<FileHandle> fileHandles, bool overwrite = false)
        {
            return fileHandleStore.TrySaveAll(fileHandles, overwrite);
        }

        /// <summary>
        /// 释放句柄：将句柄标记为已释放，一并删除缓存文件
        /// </summary>
        /// <param name="fileHandle">要释放的句柄</param>
        /// <returns>是否释放成功；已经释放过的句柄返回 false</returns>
        public bool TryReleaseFile(FileHandle fileHandle)
        {
            return fileHandleStore.TryReleaseFile(fileHandle);
        }

        /// <summary>
        /// 批量释放句柄
        /// </summary>
        /// <param name="fileHandles">要释放的句柄集合</param>
        /// <returns>成功释放的句柄数量</returns>
        public int TryReleaseAll(IEnumerable<FileHandle> fileHandles)
        {
            return fileHandleStore.TryReleaseAll(fileHandles);
        }

        /// <summary>
        /// 删除某个缓存作用域的文件夹（只在它已经是空文件夹时删除）
        /// </summary>
        /// <param name="cacheScope">缓存作用域标识</param>
        /// <remarks>业务侧在用完一个缓存作用域后用它收尾；文件夹里还有别的缓存文件时什么都不做</remarks>
        public void ClearCacheScopeIfEmpty(string cacheScope)
        {
            fileHandleStore.ClearCacheScopeIfEmpty(cacheScope);
        }

        /// <summary>
        /// 尝试向文件中写入文本
        /// </summary>
        /// <param name="targetPath">目标路径，可以是普通绝对路径或安卓 <c>content://</c> 路径</param>
        /// <param name="text">要写入的内容</param>
        /// <returns>是否写入成功</returns>
        /// <remarks>目标路径不能位于会话缓存内</remarks>
        public bool TryWriteTextToPath(string targetPath, string text)
        {
            return fileHandleStore.TryWriteTextToPath(targetPath, text);
        }

        #endregion


        /// <summary>
        /// 按编译和运行环境创建平台环境
        /// </summary>
        private static IPlatformEnvironment CreatePlatformEnvironment()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Simple File Browser 会在运行时判断安卓 10+ 是否必须走 SAF
            if (FileBrowserHelpers.ShouldUseSAF)
                return new AndroidSafPlatformEnvironment();
#endif
            return new DiskPlatformEnvironment();
        }

        /// <summary>
        /// 把自有过滤器转换为文件对话框插件的过滤器
        /// </summary>
        private static FileBrowser.Filter[]? ConvertFilters(FileTypeFilter[]? filters)
        {
            return filters
                ?.Select(filter => new FileBrowser.Filter(filter.DisplayName, filter.Extensions))
                .ToArray();
        }

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

        /// <summary>
        /// 把常用文件夹加进文件对话框的侧边栏
        /// </summary>
        /// <param name="folder">常用文件夹枚举实例</param>
        /// <param name="displayName">侧边栏显示的名称</param>
        /// <remarks>当前平台上没有这个文件夹时只打日志，不影响其它入口</remarks>
        private void AddQuickLink(CommonFolders folder, string displayName)
        {
            string? folderPath = Environment.GetCommonFolderPath(folder);
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning($"当前平台上没有可用的常用文件夹，已跳过侧边栏入口：{folder}");
                return;
            }

            FileBrowser.AddQuickLink(displayName, folderPath, null);
        }
    }
}
