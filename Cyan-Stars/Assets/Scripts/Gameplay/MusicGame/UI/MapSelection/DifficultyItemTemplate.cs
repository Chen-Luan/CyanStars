#nullable enable

using System;
using CyanStars.Chart;
using CyanStars.Framework.UI;
using CyanStars.Utils.RadioButton;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyanStars.Gameplay.MusicGame
{
    public class DifficultyItemTemplate : BaseUIItem
    {
        [SerializeField]
        private RadioButtonItem radioButton = null!;

        [SerializeField]
        private Image image = null!;

        [SerializeField]
        private TMP_Text text = null!;


        [SerializeField]
        private Sprite kuiXingUnselectedSprite = null!;

        [SerializeField]
        private Sprite kuiXingSelectedSprite = null!;

        [SerializeField]
        private Sprite qiMingUnSelectedSprite = null!;

        [SerializeField]
        private Sprite qiMingSelectedSprite = null!;

        [SerializeField]
        private Sprite tianShuUnselectedSprite = null!;

        [SerializeField]
        private Sprite tianShuSelectedSprite = null!;

        [SerializeField]
        private Sprite wuYinUnselectedSprite = null!;

        [SerializeField]
        private Sprite wuYinSelectedSprite = null!;


        /// <summary>
        /// 创建/取回后初始化内容
        /// </summary>
        /// <param name="radioButtonGroup">由此 radioButtonGroup 控制 radioButtonItem.IsChecked 状态</param>
        /// <param name="difficulty">难度</param>
        /// <param name="isChecked">是否选中</param>
        /// <param name="levelText">定数文本</param>
        public void Init(RadioButtonGroup radioButtonGroup, ChartDifficulty difficulty, bool isChecked, string levelText)
        {
            radioButton.Group = radioButtonGroup;
            radioButton.IsChecked = isChecked;

            text.text = levelText;
            image.sprite = difficulty switch
            {
                ChartDifficulty.KuiXing => isChecked ? kuiXingSelectedSprite : kuiXingUnselectedSprite,
                ChartDifficulty.QiMing => isChecked ? qiMingSelectedSprite : qiMingUnSelectedSprite,
                ChartDifficulty.TianShu => isChecked ? tianShuSelectedSprite : tianShuUnselectedSprite,
                ChartDifficulty.WuYin => isChecked ? wuYinSelectedSprite : wuYinUnselectedSprite,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
            };
        }

        public override void OnRelease()
        {
            radioButton.Group = null;
        }

        /// <summary>
        /// 在首次创建或 UI 大小变动后设置 difficultyItem 的 rectTransform 的高度
        /// </summary>
        /// <param name="height"></param>
        public void SetRectTransformHeight(float height) =>
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);


        /// <summary>
        /// 按照百分比调整 radioButton 物体的横坐标
        /// </summary>
        public void SetRadioButtonObjectPosXByRate(float rate)
        {
            rate = Mathf.Clamp(rate, 0, 1);
            float posX = rate * ((RectTransform)this.transform).rect.width;
            ((RectTransform)radioButton.transform).anchoredPosition =
                new Vector2(posX, ((RectTransform)radioButton.transform).anchoredPosition.y);
        }
    }
}
