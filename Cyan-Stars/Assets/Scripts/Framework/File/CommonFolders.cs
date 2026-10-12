namespace CyanStars.Framework.File
{
    /// <summary>
    /// 跨平台的常用文件夹
    /// </summary>
    public enum CommonFolders
    {
        /// <summary>
        /// 下载文件夹
        /// </summary>
        Downloads,

        /// <summary>
        /// 桌面
        /// </summary>
        Desktop,

        /// <summary>
        /// 玩家谱包目录：应用持久化数据目录下的谱包文件夹
        /// </summary>
        /// <remarks>目录本身可能尚未创建，由业务侧按需建目录</remarks>
        ChartPacks,
    }

    /// <summary>
    /// 硬编码的常用文件夹名称
    /// </summary>
    internal static class CommonFolderDirectoryNames
    {
        /// <summary>
        /// 玩家谱包目录名
        /// </summary>
        public const string ChartPacks = "ChartPacks";
    }
}
