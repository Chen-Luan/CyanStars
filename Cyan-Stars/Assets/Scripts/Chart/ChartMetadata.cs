using Newtonsoft.Json;

namespace CyanStars.Chart
{
    public sealed class ChartMetaData
    {
        /// <summary>
        /// 对应的谱面文件
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 谱面难度区域文本覆写
        /// </summary>
        /// <remarks>
        /// 此字段为空时将以 难度+"Lv"+(int)定数 生成文本，非空时直接作为文本。内置谱约定此字段为空。
        /// </remarks>
        public string OverrideDifficultyText;

        /// <summary>
        /// 谱面难度
        /// </summary>
        /// <remarks>
        /// 为空时只在制谱器内可见，游戏内不加载
        /// </remarks>
        public ChartDifficulty? Difficulty;

        /// <summary>
        /// 谱面定数
        /// </summary>
        /// <remarks>
        /// 此字段用于生成默认的难度区域文本。
        /// 内置谱还将用于计算玩家实力；社区谱当前版本暂不参与计算。
        /// TODO：后续开发社区谱面审核等其他机制后，可考虑允许社区谱参与玩家实力计算。
        /// </remarks>
        public float Level;

        /// <summary>
        /// 构造函数
        /// </summary>
        [JsonConstructor]
        public ChartMetaData(string filePath, ChartDifficulty? difficulty = null)
        {
            FilePath = filePath;
            Difficulty = difficulty;
        }
    }
}
