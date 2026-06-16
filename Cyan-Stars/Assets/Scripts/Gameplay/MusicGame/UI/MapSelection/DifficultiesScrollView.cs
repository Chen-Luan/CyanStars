#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CyanStars.Chart;
using CyanStars.Framework;
using CyanStars.Framework.UI;
using CyanStars.Utils.RadioButton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyanStars.Gameplay.MusicGame
{
    [RequireComponent(typeof(ScrollRect))]
    public class DifficultiesScrollView : UIBehaviour
    {
        [SerializeField]
        private ScrollRect scrollRect = null!;

        [SerializeField]
        private RectTransform scrollContentRect = null!;

        [SerializeField]
        private VerticalLayoutGroup verticalLayoutGroup = null!;

        [SerializeField]
        private RadioButtonGroup radioButtonGroup = null!;

        [SerializeField]
        private GameObject difficultyItemTemplate = null!;


        private int validDifficultyCounts = 0; // 当前谱包内可游玩的谱面计数
        private static UIManager UIManager => GameRoot.UI;
        private readonly List<DifficultyItemTemplate> DifficultyItems = new List<DifficultyItemTemplate>();


        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            UpdateContentHeight(validDifficultyCounts);
            RefreshButtonsHeight();
        }


        /// <summary>
        /// 清除旧难度按钮列表并生成新难度列表
        /// </summary>
        /// <param name="runtimeChartPack">运行时谱包</param>
        /// <exception cref="InvalidDataException">没有可游玩难度</exception>
        public async Task SetDifficultiesAsync(RuntimeChartPack runtimeChartPack)
        {
            var metaDatas = runtimeChartPack.ChartPackData.ChartMetaDatas;
            var validMetaDatas = metaDatas.Where(metaData => metaData.Difficulty != null).ToList();
            validDifficultyCounts = validMetaDatas.Count;

            // 更新 scrollContent 总高度
            UpdateContentHeight(validDifficultyCounts);

            // 释放旧难度按钮
            List<BaseUIItem> baseItems = DifficultyItems.Cast<BaseUIItem>().ToList();
            UIManager.ReleaseUIItems(baseItems);
            DifficultyItems.Clear();

            // 取回新难度按钮，注入参数，设置高度和横坐标
            List<Task<DifficultyItemTemplate>> tasks = new();
            for (int i = 0; i < validDifficultyCounts; i++)
            {
                var valueTask = UIManager.GetUIItemAsync<DifficultyItemTemplate>(difficultyItemTemplate, scrollContentRect);
                tasks.Add(valueTask.AsTask());
            }

            await Task.WhenAll(tasks);

            for (int i = 0; i < tasks.Count; i++)
            {
                var difficultyItem = tasks[i].Result;
                var metadata = validMetaDatas[i];
                // TODO: 记住玩家上次在这个谱包内选择的是哪一张谱面，下次直接打开
                // TODO: 内置谱 text 改为难度+定数（国际化字段），玩家谱包 text 改为谱面标题
                difficultyItem.Init(radioButtonGroup, metadata.Difficulty!.Value, i == 0, "TODO");
                DifficultyItems.Add(difficultyItem);
            }

            RefreshButtonsHeight();
            // TODO: 初始化完毕后更新一次物体横坐标，当滚动 ScrollRect 时也要每帧更新。
        }


        /// <summary>
        /// 更新 Content 高度
        /// </summary>
        private void UpdateContentHeight(int difficultyCounts)
        {
            switch (difficultyCounts)
            {
                case <= 0:
                    throw new InvalidDataException("谱包不存在任何可游玩难度，无法更新难度。");
                case <= 4:
                {
                    float height = ((RectTransform)scrollRect.transform).rect.height;
                    scrollContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    break;
                }
                default:
                {
                    float buttonHeight = CalculateButtonHeight();

                    float contentHeight = 0;
                    contentHeight += verticalLayoutGroup.padding.top;
                    contentHeight += verticalLayoutGroup.padding.bottom;
                    contentHeight += difficultyCounts * buttonHeight;
                    contentHeight += (difficultyCounts - 1) * verticalLayoutGroup.spacing;
                    scrollContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
                    break;
                }
            }
        }

        /// <summary>
        /// 计算单个按钮高度，确保 4 个按钮高度 + 3 个间距 + 上下 padding 正好填满一页
        /// </summary>
        /// <returns>单个按钮高度</returns>
        private float CalculateButtonHeight()
        {
            float scrollHeight = ((RectTransform)scrollRect.transform).rect.height;
            float buttonHeight = ((scrollHeight - verticalLayoutGroup.padding.top - verticalLayoutGroup.padding.bottom) -
                                  (verticalLayoutGroup.spacing * 3))
                                 / 4;
            return buttonHeight;
        }

        /// <summary>
        /// 在设置完难度或 Canvas 高度变化时，更新所有难度按钮的高度
        /// </summary>
        private void RefreshButtonsHeight()
        {
            var height = CalculateButtonHeight();

            foreach (var item in DifficultyItems)
                item.SetRectTransformHeight(height);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContentRect);
        }
    }
}
