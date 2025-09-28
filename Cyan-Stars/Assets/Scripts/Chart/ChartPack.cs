namespace CyanStars.Chart
{
    /// <summary>
    /// 运行时谱包数据
    /// </summary>
    public class ChartPack
    {
        public ChartPackData ChartPackData;

        /// <summary>
        /// 是否为内置谱包，内置谱包的定数可用于计算玩家实力水平，内置谱面的定数要求可转换为 1~20 整数的 string，且内置谱包无法被制谱器编辑
        /// </summary>
        /// <remarks>
        /// 此值在加载时根据加载方式确定，内置谱包必须位于编辑器多媒体路径，且在 SO 中注册
        /// </remarks>
        public readonly bool IsInternal;

        /// <summary>
        /// 谱包文件夹（制谱工作区）绝对路径
        /// </summary>
        public readonly string ChartPackFolderPath;


        public ChartPack(ChartPackData chartPackData, bool isInternal, string chartPackFolderPath)
        {
            ChartPackData = chartPackData;
            IsInternal = isInternal;
            ChartPackFolderPath = chartPackFolderPath;
        }
    }
}
