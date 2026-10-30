using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 화면 효과 (달빛 방향).
///
///   BossScreenFx.Darken(0.55f, 0.5f)   방을 어둡게 (등장 때)
///   BossScreenFx.Tint(true, 1.2f)      방을 붉게 물들임 (2페이즈 동안)
///   BossScreenFx.Letterbox(true, 0.5f) 위아래 검은 띠
///   BossScreenFx.Flash(color, 0.4f)    화면 번쩍
///   BossScreenFx.Clear()               전부 끄기
///
/// 어둡게·물들임은 월드 위에 덮는 판이라 플레이어 HUD(체력 등)는 그대로 보인다.
/// 검은 띠·번쩍은 화면 UI로 그린다(HP바·이름 카드보다 아래).
/// 실제 시간으로 흐른다. 씬마다 하나 생기고, 씬이 바뀌면 같이 사라진다.
/// </summary>
public class BossScreenFx : MonoBehaviour
{
    private const float LetterboxHeight = 110f;   // 1920×1080 기준
    private const string OverlayLayer = "UI";       // 월드 정렬 레이어 중 맨 위
    private const int OverlayOrder = -200;

    private static readonly Color DarkColor = new Color(0f, 0f, 0f, 1f);
    private static readonly Color TintEdge = new Color(0.30f, 0.04f, 0.06f, 1f);
    private static readonly Color TintCore = new Color(1f, 0.45f, 0.22f, 1f);

    private static BossScreenFx instance;

    private SpriteRenderer dark, tintEdge, tintCore;
    private RectTransform barTop, barBottom;
    private Image flash;
    private CanvasGroup group;

    private float darkNow, darkTarget, darkSpeed = 1f;
    private float tintNow, tintTarget, tintSpeed = 1f;
    private float barNow, barTarget, barSpeed = 1f;
    private float flashNow, flashSpeed = 1f;

    public static void Darken(float alpha, float seconds)
    {
        BossScreenFx fx = Ensure();
        fx.darkTarget = Mathf.Clamp01(alpha);
        fx.darkSpeed = 1f / Mathf.Max(0.05f, seconds);
    }

    public static void Tint(bool on, float seconds)
    {
        BossScreenFx fx = Ensure();
        fx.tintTarget = on ? 1f : 0f;
        fx.tintSpeed = 1f / Mathf.Max(0.05f, seconds);
    }

    public static void Letterbox(bool on, float seconds)
    {
        BossScreenFx fx = Ensure();
        fx.barTarget = on ? 1f : 0f;
        fx.barSpeed = 1f / Mathf.Max(0.05f, seconds);
    }

    public static void Flash(Color color, float seconds)
    {
        BossScreenFx fx = Ensure();
        fx.flash.color = new Color(color.r, color.g, color.b, 1f);
        fx.flashNow = Mathf.Clamp01(color.a);
        fx.flashSpeed = fx.flashNow / Mathf.Max(0.05f, seconds);
    }

    public static void Clear()
    {
        if (instance == null) return;
        instance.darkTarget = instance.tintTarget = instance.barTarget = 0f;
        instance.darkNow = instance.tintNow = instance.barNow = instance.flashNow = 0f;
        instance.Apply();
    }

    private static BossScreenFx Ensure()
    {
        if (instance != null) return instance;

        Canvas canvas = PresentationUI.NewCanvas("[BossScreenFx]", 405);
        BossScreenFx fx = canvas.gameObject.AddComponent<BossScreenFx>();
        fx.group = canvas.GetComponent<CanvasGroup>();
        fx.group.alpha = 1f;

        fx.flash = PresentationUI.NewImage("Flash", canvas.transform, Color.white);
        PresentationUI.Stretch(fx.flash.rectTransform);

        fx.barTop = Bar(canvas.transform, "LetterboxTop", 1f);
        fx.barBottom = Bar(canvas.transform, "LetterboxBottom", 0f);

        fx.dark = Overlay(fx.transform, "Darken", BossFxArt.White, OverlayOrder);
        fx.tintEdge = Overlay(fx.transform, "TintEdge", BossFxArt.White, OverlayOrder + 1);
        fx.tintCore = Overlay(fx.transform, "TintCore", BossFxArt.SoftDot, OverlayOrder + 2);

        instance = fx;
        fx.Apply();
        return fx;
    }

    private static RectTransform Bar(Transform parent, string name, float y)
    {
        Image img = PresentationUI.NewImage(name, parent, Color.black);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, y);
        rt.anchorMax = new Vector2(1f, y);
        rt.pivot = new Vector2(0.5f, y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        return rt;
    }

    private static SpriteRenderer Overlay(Transform parent, string name, Sprite sprite, int order)
    {
        // 캔버스 아래에 두면 RectTransform이 붙으므로 따로 둔다
        GameObject go = new GameObject("[BossScreenFx] " + name);
        go.transform.SetParent(null);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = OverlayLayer;
        sr.sortingOrder = order;
        sr.enabled = false;
        return sr;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        darkNow = Mathf.MoveTowards(darkNow, darkTarget, darkSpeed * dt);
        tintNow = Mathf.MoveTowards(tintNow, tintTarget, tintSpeed * dt);
        barNow = Mathf.MoveTowards(barNow, barTarget, barSpeed * dt);
        flashNow = Mathf.MoveTowards(flashNow, 0f, flashSpeed * dt);
        Apply();
    }

    private void LateUpdate()
    {
        // 월드 판을 카메라 화면에 맞춘다
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 p = cam.transform.position;
        float h = cam.orthographic ? cam.orthographicSize * 2f : 20f;
        float w = h * cam.aspect;
        Place(dark, p, w * 1.2f, h * 1.2f);
        Place(tintEdge, p, w * 1.2f, h * 1.2f);
        Place(tintCore, p, w * 1.1f, h * 1.3f);
    }

    private static void Place(SpriteRenderer sr, Vector3 camPos, float w, float h)
    {
        if (sr == null) return;
        sr.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        Vector2 size = sr.sprite.bounds.size;
        sr.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
    }

    private void Apply()
    {
        float ease(float x) => x * x * (3f - 2f * x);

        if (dark != null)
        {
            dark.enabled = darkNow > 0.001f;
            dark.color = new Color(DarkColor.r, DarkColor.g, DarkColor.b, darkNow);
        }
        if (tintEdge != null)
        {
            float k = ease(tintNow);
            tintEdge.enabled = tintCore.enabled = k > 0.001f;
            tintEdge.color = new Color(TintEdge.r, TintEdge.g, TintEdge.b, 0.28f * k);
            tintCore.color = new Color(TintCore.r, TintCore.g, TintCore.b, 0.16f * k);
        }

        float bar = LetterboxHeight * ease(barNow);
        barTop.sizeDelta = new Vector2(0f, bar);
        barBottom.sizeDelta = new Vector2(0f, bar);

        Color f = flash.color;
        f.a = flashNow;
        flash.color = f;
        flash.enabled = flashNow > 0.001f;
    }

    private void OnDestroy()
    {
        if (dark != null) Destroy(dark.gameObject);
        if (tintEdge != null) Destroy(tintEdge.gameObject);
        if (tintCore != null) Destroy(tintCore.gameObject);
        if (instance == this) instance = null;
    }
}
