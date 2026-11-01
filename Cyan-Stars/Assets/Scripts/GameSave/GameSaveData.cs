using System;
using System.Collections.Generic;
using CyanStars.Chart;
using CyanStars.Gameplay.MusicGame;

namespace CyanStars.GameSave
{
    /// <summary>
    /// 玩家存档结构
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int Version { get; set; } // 存档版本
        public DateTime CreateTime { get; set; } // 存档首次创建的时间
        public DateTime SaveTime { get; set; } // 存档末次保存的时间

        public MusicGameSaveData MusicGameData { get; set; }
        // public StorySaveData StoryData { get;set; } // Todo:剧情模式存档数据

        public string Verification { get; set; } // 存档数据校验

        /// <summary>
        /// 音游成绩类
        /// </summary>
        [Serializable]
        public class MusicGameSaveData
        {
            /// <summary>
            /// 键为谱面哈希，值为该谱面的成绩记录
            /// </summary>
            /// <remarks>
            /// <para>内置谱面的哈希来自编译时哈希表，社区谱面的哈希来自玩家设备上的谱面哈希缓存文件</para>
            /// <para>谱面内容变化后哈希随之变化，成绩不会自动延续到修改后的谱面；
            /// 以难度或谱包定位谱面都不够可靠，因此成绩直接挂在谱面哈希之下</para>
            /// <para>找不到谱面但有分数时仍然记录分数，导入对应内容的谱面后即可按哈希正常展示</para>
            /// <para>TODO: 成绩读写尚未接入流程，本结构目前只是预留；接入后需按谱面哈希取用与写入</para>
            /// </remarks>
            public Dictionary<string, ChartSaveData> ChartSaveDatas { get; set; }

            /// <summary>
            /// 每个谱面的成绩。
            /// 内置谱和社区谱通用的结构，如果找不到谱面但有分数，仍然记录分数，导入谱面后即可正常展示分数。
            /// </summary>
            [Serializable]
            public class ChartSaveData
            {
                public ChartDifficulty ChartDifficulty { get; set; } // 谱面难度

                public int PlayCount { get; set; } // 玩家累计游玩此谱面的次数

                public DateTime FirstPlayTime { get; set; } // 首次完成游玩的时间（进入结算页时时间）

                public DateTime BestScorePlayTime { get; set; } // 最佳成绩对应的游玩时间（进入结算页时时间）
                public MusicGamePlayData BestPlayData { get; set; } // 最佳成绩时的游玩数据

                public DateTime LastPlayTime { get; set; } // 末次游玩时间（进入结算页时时间）
                public MusicGamePlayData LastPlayData { get; set; } // 末次游玩时的游玩数据
            }
        }
    }
}
