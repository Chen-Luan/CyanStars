using CyanStars.Chart;
using CyanStars.ChartEditor.Model;
using CyanStars.ChartEditor.View;
using CyanStars.Framework;
using CyanStars.Framework.FSM;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyanStars.ChartEditor.Procedure
{
    [ProcedureState(true)]
    public class ChartEditorProcedure : BaseState
    {
        private const string ChartEditorScenePath = "Assets/BundleRes/Scenes/ChartEditor.unity";
        private Scene scene;
        private GameObject chartEditorMainCanva;

        public async override void OnEnter()
        {
            GameRoot.MainCamera.gameObject.SetActive(false);
            scene = await GameRoot.Asset.LoadSceneAsync(ChartEditorScenePath);
            if (scene != default)
            {
                // 创建 model 并为既有的 views 绑定 model，动态生成的 views 将在生成时由父级 view 传入 model
                chartEditorMainCanva = GameObject.Find("ChartEditorMainCanva");
                // ChartPackData chartPackData = GameRoot.Chart.SelectedChartPack.ChartPackData;
                // ChartData chartData = GameRoot.Chart.SelectedChart.ChartData;
                EditorModel model = new EditorModel(new ChartPackData(""), new ChartData());
                BaseView[] views = chartEditorMainCanva.GetComponentsInChildren<BaseView>(true);
                foreach (BaseView view in views)
                {
                    view.Bind(model);
                }
            }
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        public override void OnExit()
        {
            GameRoot.MainCamera.gameObject.SetActive(true);
        }
    }
}
