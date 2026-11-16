#nullable enable

using CyanStars.Gameplay.ChartEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyanStars
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(PopupEffectController))]
    public class ChartPackSelectionPopup : MonoBehaviour
    {
        [SerializeField]
        private Button closePopupButton = null!;

        [SerializeField]
        private Button createChartPackButton = null!;

        [SerializeField]
        private Button importChartPackButton = null!;

        [SerializeField]
        private Button openChartPackFolderButton = null!;

        [SerializeField]
        private TMP_Text chartPackPathText = null!;


        [SerializeField]
        private GameObject chartPacksContentObject = null!;

        [SerializeField]
        private GameObject chartsContentObject = null!;


        [SerializeField]
        private Button copyChartPackButton = null!;

        [SerializeField]
        private Button moveUpChartPackButton = null!;

        [SerializeField]
        private Button moveDownChartPackButton = null!;

        [SerializeField]
        private Button deleteChartPackButton = null!;


        [SerializeField]
        private Button createChartButton = null!;

        [SerializeField]
        private Button copyChartButton = null!;

        [SerializeField]
        private Button moveUpChartButton = null!;

        [SerializeField]
        private Button moveDownChartButton = null!;

        [SerializeField]
        private Button deleteButton = null!;

        [SerializeField]
        private Button enterChartEditorButton = null!;

        private Canvas canvas = null!;
        private PopupEffectController popupEffectController = null!;


        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            popupEffectController = GetComponent<PopupEffectController>();
        }

        private void OnEnable() => closePopupButton.onClick.AddListener(CloseCanvas);
        private void OnDisable() => closePopupButton.onClick.RemoveListener(CloseCanvas);

        public async void OpenCanvas()
        {
            if (canvas.enabled)
                return;

            canvas.enabled = true;
            await popupEffectController.ShowPopupCanvas();
        }

        private async void CloseCanvas()
        {
            await popupEffectController.HidePopupCanvas();
            canvas.enabled = false;
        }
    }
}
