using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 로고 라이팅.
/// 1) 글자 뒤 푸른 빛(glow)이 천천히 숨 쉬듯 밝아졌다 어두워진다.
/// 2) 몇 초마다 빛이 한 번 지나간다.
///    - pathShine이 있으면: 여우 모양을 따라 머리 → 앞다리 → 아래 선 → 꼬리 순서로 흐른다 (10.27, UI/LogoPathShine 셰이더).
///    - 없으면: 예전처럼 비스듬한 빛줄기(shineBar)가 왼쪽에서 오른쪽으로 지나간다.
/// 시간 정지(timeScale 0)와 무관하게 돈다.
/// </summary>
public class TitleLogoLight : MonoBehaviour
{
    [Header("글자 뒤 빛")]
    [SerializeField] private Graphic glow;
    [Tooltip("밝기가 오르내리는 정도. 0.3이면 원래 밝기의 ±30%")]
    [SerializeField, Range(0f, 1f)] private float glowPulse = 0.2f;
    [Tooltip("한 번 숨 쉬는 데 걸리는 시간(초)")]
    [SerializeField] private float glowPeriod = 4f;

    [Header("지나가는 빛")]
    [Tooltip("여우 모양을 따라 흐르는 빛 (UI/LogoPathShine 재질을 쓴 그래픽). 있으면 이쪽을 쓴다.")]
    [SerializeField] private Graphic pathShine;
    [Tooltip("예전 방식: 왼쪽 → 오른쪽으로 지나가는 빛줄기")]
    [SerializeField] private RectTransform shineBar;
    [Tooltip("빛줄기가 다시 지나가기까지의 간격(초)")]
    [SerializeField] private float shineInterval = 6f;
    [Tooltip("빛줄기가 로고를 가로지르는 시간(초)")]
    [SerializeField] private float shineDuration = 1.4f;
    [Tooltip("처음 빛줄기가 지나가기 전 대기(초)")]
    [SerializeField] private float firstDelay = 1.2f;
    [Tooltip("빛줄기가 움직이는 거리 (왼쪽 끝 ~ 오른쪽 끝, 픽셀)")]
    [SerializeField] private float shineTravel = 900f;

    private float glowBaseAlpha;
    private Graphic shineGraphic;
    private Material pathMaterial;
    private float startTime;
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int WidthId = Shader.PropertyToID("_Width");

    private void Awake()
    {
        if (glow != null) glowBaseAlpha = glow.color.a;
        if (shineBar != null) shineGraphic = shineBar.GetComponent<Graphic>();

        // 재질 파일을 직접 바꾸지 않도록 복사본을 쓴다.
        if (pathShine != null && pathShine.material != null)
        {
            pathMaterial = new Material(pathShine.material);
            pathShine.material = pathMaterial;
            pathMaterial.SetFloat(ProgressId, -1f);
        }
    }

    private void OnDestroy()
    {
        if (pathMaterial != null) Destroy(pathMaterial);
    }

    private void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    private void Update()
    {
        float t = Time.unscaledTime - startTime;

        if (glow != null)
        {
            Color c = glow.color;
            c.a = glowBaseAlpha * (1f + glowPulse * Mathf.Sin(t / glowPeriod * Mathf.PI * 2f));
            glow.color = c;
        }

        float cycle = t - firstDelay;

        if (pathMaterial != null)
        {
            bool on = cycle >= 0f && Mathf.Repeat(cycle, shineInterval) <= shineDuration;
            if (!on)
            {
                pathMaterial.SetFloat(ProgressId, -1f);
                return;
            }

            float q = Mathf.Repeat(cycle, shineInterval) / shineDuration;
            q = q * q * (3f - 2f * q);
            float w = pathMaterial.GetFloat(WidthId);
            pathMaterial.SetFloat(ProgressId, Mathf.Lerp(-w, 1f + w, q));
            return;
        }

        if (shineBar == null) return;

        bool visible = cycle >= 0f && Mathf.Repeat(cycle, shineInterval) <= shineDuration;
        if (shineGraphic != null) shineGraphic.enabled = visible;
        if (!visible) return;

        float p = Mathf.Repeat(cycle, shineInterval) / shineDuration;
        p = p * p * (3f - 2f * p); // 천천히 출발해서 천천히 멈춘다
        shineBar.anchoredPosition = new Vector2(Mathf.Lerp(-shineTravel * 0.5f, shineTravel * 0.5f, p), 0f);
    }
}
