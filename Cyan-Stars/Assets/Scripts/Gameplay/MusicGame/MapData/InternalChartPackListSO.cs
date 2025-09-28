using System.Collections.Generic;
using UnityEngine;

namespace CyanStars.Gameplay.MusicGame
{
    /// <summary>
    /// 内置谱包列表SO
    /// </summary>
    [CreateAssetMenu(menuName = "创建内置谱包列表SO文件")]
    public class InternalChartPackListSO : ScriptableObject
    {
        /// <example>Assets/CysMultimediaAssets/ChartPacks/ChartPack0/ChartPackData.json</example>
        [Header("内置谱包索引文件相对路径")]
        public List<string> Paths;
    }
}
