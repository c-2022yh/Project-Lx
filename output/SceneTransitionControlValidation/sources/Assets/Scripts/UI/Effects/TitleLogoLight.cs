using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 로고 라이팅.
/// 1) 글자 뒤 푸른 빛(glow)이 천천히 숨 쉬듯 밝아졌다 어두워진다.
/// 2) 몇 초마다 빛이 한 번 지나간다.
///    - pathShine이 있으면: 여우 모양을 따라 머리 → 앞다리 → 아래 선 → 꼬리 순서로 흐른다 (10.27, UI/LogoPathShine 셰이더).
///    - 없으면: 예전처럼 비스듬한 빛줄기(shineBar)가 왼쪽에서 오른쪽으로 지나간다.
/// 3) 여우 꼬리가 살랑살랑 흔들린다 (10.29, UI/LogoTailSway). 로고·뒤 빛·지나가는 빛이 같이 흔들린다.
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

    [Header("꼬리 흔들기")]
    [SerializeField] private bool tailSway = true;
    [Tooltip("꼬리 끝이 흔들리는 각도(라디안). 0.07 ≈ 4도")]
    [SerializeField, Range(0f, 0.2f)] private float tailAmp = 0.07f;

    private float glowBaseAlpha;
    private Material logoMat;
    private Material glowMat;
    private static readonly int SwayTimeId = Shader.PropertyToID("_SwayTime");
    private static readonly int AmpId = Shader.PropertyToID("_Amp");
    private static readonly int MapId = Shader.PropertyToID("_MapST");
    private static readonly int UseMaskId = Shader.PropertyToID("_UseMask");
    private Graphic shineGraphic;
    private Material pathMaterial;
    private float startTime;
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int WidthId = Shader.PropertyToID("_Width");
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private float shineHoldUntil;

    /// <summary>
    /// 인트로용: 로고를 왼쪽부터 x(로고 기준 0~1)까지만 보이게 한다. 2 이상이면 전부 보인다.
    /// 꼬리 흔들기 재질이 없으면 아무것도 안 하고 false.
    /// </summary>
    public bool SetReveal(float x)
    {
        if (logoMat == null) return false;
        logoMat.SetFloat(RevealId, x);
        return true;
    }

    /// <summary>지나가는 빛을 지금부터 seconds초 동안 멈춘다 (인트로 중에 빛이 지나가지 않게).</summary>
    public void HoldShine(float seconds)
    {
        shineHoldUntil = Time.unscaledTime + seconds;
    }

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

    private void Start()
    {
        if (tailSway) SetupTailSway();
    }

    /// <summary>로고·뒤 빛·지나가는 빛에 꼬리 흔들기 재질을 입힌다. 재질이 없으면 그냥 넘어간다.</summary>
    private void SetupTailSway()
    {
        Material baseMat = Resources.Load<Material>("Title/LogoTailSway");
        Graphic logo = GetComponent<Graphic>();
        if (baseMat == null || logo == null) return;

        RectTransform logoRect = logo.rectTransform;

        logoMat = new Material(baseMat);
        logoMat.SetFloat(AmpId, tailAmp);
        logoMat.SetFloat(UseMaskId, 1f);
        logoMat.SetVector(MapId, new Vector4(1f, 1f, 0f, 0f));
        logo.material = logoMat;

        if (glow != null)
        {
            glowMat = new Material(baseMat);
            glowMat.SetFloat(AmpId, tailAmp);
            glowMat.SetFloat(UseMaskId, 0f);   // 빛은 번져 있어서 오른쪽을 통째로 움직인다
            glowMat.SetVector(MapId, MapOf(glow.rectTransform, logoRect));
            glow.material = glowMat;
        }

        if (pathMaterial != null && pathShine != null)
        {
            pathMaterial.SetFloat(AmpId, tailAmp);
            pathMaterial.SetVector(MapId, MapOf(pathShine.rectTransform, logoRect));
        }
    }

    /// <summary>그래픽 g의 UV를 로고 UV로 바꾸는 값. 로고 UV = UV * xy + zw.</summary>
    private static Vector4 MapOf(RectTransform g, RectTransform logo)
    {
        Vector3[] a = new Vector3[4], b = new Vector3[4];
        g.GetWorldCorners(a);
        logo.GetWorldCorners(b);
        Vector2 gMin = a[0], gSize = a[2] - a[0];
        Vector2 lMin = b[0], lSize = b[2] - b[0];
        if (lSize.x <= 0f || lSize.y <= 0f) return new Vector4(1f, 1f, 0f, 0f);
        return new Vector4(gSize.x / lSize.x, gSize.y / lSize.y, (gMin.x - lMin.x) / lSize.x, (gMin.y - lMin.y) / lSize.y);
    }

    private void OnDestroy()
    {
        if (pathMaterial != null) Destroy(pathMaterial);
        if (logoMat != null) Destroy(logoMat);
        if (glowMat != null) Destroy(glowMat);
    }

    private void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    private void Update()
    {
        float t = Time.unscaledTime - startTime;

        if (logoMat != null) logoMat.SetFloat(SwayTimeId, t);
        if (glowMat != null) glowMat.SetFloat(SwayTimeId, t);
        if (pathMaterial != null) pathMaterial.SetFloat(SwayTimeId, t);

        if (glow != null)
        {
            Color c = glow.color;
            c.a = glowBaseAlpha * (1f + glowPulse * Mathf.Sin(t / glowPeriod * Mathf.PI * 2f));
            glow.color = c;
        }

        // 멈춰 둔 시간이 끝나면 그때부터 다시 센다 (빛이 중간부터 튀어나오지 않게)
        float cycle = Time.unscaledTime - Mathf.Max(startTime + firstDelay, shineHoldUntil);

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
