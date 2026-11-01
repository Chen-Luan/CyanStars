using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyanStars.Gameplay.MusicGame
{
    /// <summary>
    /// 内置谱包列表SO
    /// </summary>
    /// <remarks>
    /// <para>列表每项对应一个内置谱包：索引文件路径由制谱与维护者手维护，决定哪些谱包按内置谱包加载；
    /// 该谱包内各谱面的哈希由编辑器菜单「生成内置谱面哈希」写入，不应手工编辑</para>
    /// <para>内置谱包打进资源包后取不到谱面文件的路径、修改时间与大小，无法参与哈希缓存比对，
    /// 因此内置谱面的哈希随本资产提供，也不进入玩家设备上的谱面哈希缓存文件</para>
    /// </remarks>
    [CreateAssetMenu(menuName = "创建内置谱包列表SO文件")]
    public class InternalChartPackListSO : ScriptableObject
    {
        /// <summary>
        /// 本资产的资源路径，内置谱包的唯一来源
        /// </summary>
        public const string AssetPath =
            "Assets/BundleRes/ScriptObjects/InternalMap/InternalMapList.asset";

        [Header("内置谱包列表")]
        public List<InternalChartPackEntry> ChartPacks = new List<InternalChartPackEntry>();

        /// <summary>
        /// 一个内置谱包：索引文件路径与本包各谱面的哈希
        /// </summary>
        [Serializable]
        public class InternalChartPackEntry
        {
            /// <summary>
            /// 谱包索引文件的资源路径，形如 <c>Assets/CysMultimediaAssets/Charts/0-幽灵少女/ChartPack.json</c>
            /// </summary>
            /// <remarks>手维护，此路径的目录部分即谱包的工作区</remarks>
            public string ChartPackFilePath = "";

            /// <summary>
            /// 本谱包内各谱面的哈希，由编辑器生成
            /// </summary>
            public List<ChartHashEntry> ChartHashes = new List<ChartHashEntry>();
        }

        /// <summary>
        /// 一条内置谱面哈希记录
        /// </summary>
        [Serializable]
        public class ChartHashEntry
        {
            /// <summary>
            /// 谱面文件相对谱包目录的路径，与谱包索引文件里的写法一致，形如 <c>Charts/窥星.json</c>
            /// </summary>
            public string ChartFilePath = "";

            /// <summary>
            /// 带版本前缀的谱面哈希文本
            /// </summary>
            public string ChartHash = "";
        }
    }
}
