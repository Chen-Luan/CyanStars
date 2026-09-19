#nullable enable

using UnityEngine;

namespace CyanStars.Gameplay.MusicGame
{
    /// <summary>
    /// SDF 打击特效播放器
    /// </summary>
    /// <remarks>
    /// 驱动 HitEffectShaderGraph 的 <c>_Progress</c>（0 → 1），由 Shader 根据进度算出矩形 SDF 描边的扩散与淡出，
    /// </remarks>
    [RequireComponent(typeof(MeshRenderer))]
    public class SdfHitEffectPlayer : MonoBehaviour
    {
        /// <summary>
        /// 特效颜色
        /// </summary>
        [SerializeField]
        private Color color = Color.white;

        /// <summary>
        /// 从进度 0 播放到 1 所需的时长（s）
        /// </summary>
        [SerializeField]
        [Range(0.01f, 10f)]
        private float duration = 0.25f;

        /// <summary>
        /// 是否在被激活时自动从头播放
        /// </summary>
        [SerializeField]
        private bool playOnEnable = true;

        /// <summary>
        /// 播放结束后是否隐藏渲染器
        /// </summary>
        [SerializeField]
        private bool hideOnFinish = true;

        [SerializeField]
        private Renderer targetRenderer = null!;


        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        private readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// 已播放的时间（s）
        /// </summary>
        private float playTime;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        private bool isPlaying;

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 从头开始播放
        /// </summary>
        private void Play()
        {
            playTime = 0;
            isPlaying = true;

            targetRenderer.enabled = true;
            Apply(0f);
        }

        /// <summary>
        /// 停止播放，保持当前进度
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            // 按累计时长换算进度，避免逐帧累加造成的误差
            playTime += Time.deltaTime;
            float progress = duration > 0f ? Mathf.Clamp01(playTime / duration) : 1f;

            Apply(progress);

            if (progress >= 1f)
                Finish();
        }

        /// <summary>
        /// 把进度与颜色写入材质属性块
        /// </summary>
        private void Apply(float progress)
        {
            PropertyBlock.SetColor(ColorId, color);
            PropertyBlock.SetFloat(ProgressId, progress);
            targetRenderer.SetPropertyBlock(PropertyBlock);
        }

        /// <summary>
        /// 播放结束
        /// </summary>
        private void Finish()
        {
            isPlaying = false;

            if (hideOnFinish)
                targetRenderer.enabled = false;
        }
    }
}
