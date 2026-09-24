#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyanStars
{
    [DisallowMultipleComponent]
    public class ChartSelectPopup : MonoBehaviour
    {
        [Header("预制体")]
        [SerializeField]
        private GameObject chartPackItemPrefab = null!;

        [SerializeField]
        private GameObject chartItemPrefab = null!;

        [Header("UI 组件")]
        [SerializeField]
        private Canvas popupCanvas = null!;

        [SerializeField]
        private Button closePopupButton = null!;

        [SerializeField]
        private Button createChartPackButton = null!;

        [SerializeField]
        private Button importChartPackButton = null!;

        [SerializeField]
        private Button openChartPackFolderButton = null!;

        [SerializeField]
        private TMP_Text chartPackFolderText = null!;

        [SerializeField]
        private Button copyChartPackButton = null!;

        [SerializeField]
        private Button moveUpChartPackButton = null!;

        [SerializeField]
        private Button moveDownChartPackButton = null!;

        [SerializeField]
        private Button deleteChartPackButton = null!;

        [SerializeField]
        private Button addChartButton = null!;

        [SerializeField]
        private Button copyChartButton = null!;

        [SerializeField]
        private Button moveUpChartButton = null!;

        [SerializeField]
        private Button moveDownChartButton = null!;

        [SerializeField]
        private Button deleteChartButton = null!;

        [SerializeField]
        private Button entryChertEditorButton = null!;
    }
}
