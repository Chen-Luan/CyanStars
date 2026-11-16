#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyanStars
{
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
    }
}
