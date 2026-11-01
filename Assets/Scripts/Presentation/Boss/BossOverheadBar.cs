using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 머리 위에 뜨는 가로 체력바 (11.01). 페이즈 표시 없이 한 줄.
///
///   BossOverheadBar.Show(bossHealth);
///   BossOverheadBar.Hide();
///
/// 골렘이 둘로 갈라지면 두 조각의 체력을 합쳐서 같은 바가 이어서 줄어든다
/// (갈라질 때 남아 있던 비율에서 시작). 바는 살아 있는 조각들 위 가운데를 따라간다.
/// 맞으면 밝은 칸이 바로 줄고, 뒤의 옅은 칸이 잠깐 있다가 따라온다.
/// </summary>
public class BossOverheadBar : MonoBehaviour
{
    private const float Width = 3.6f;          // 월드 단위
    private const float Height = 0.13f;
    private const float AboveHead = 0.7f;      // 콜라이더 위에서 이만큼 위
    private const float PixelsPerUnit = 100f;  // 캔버스 1 = 0.01 월드
    private const float FadeSeconds = 0.35f;
    private const float LagDelay = 0.45f;
    private const float LagSpeed = 0.9f;

    private static readonly Color FrameColor = new Color(0.02f, 0.03f, 0.05f, 0.85f);
    private static readonly Color TrackColor = new Color(0.84f, 0.88f, 0.96f, 0.18f);
    private static readonly Color LagColor = new Color(0.94f, 0.96f, 0.99f, 0.45f);
    private static readonly Color FillColor = new Color(0.91f, 0.93f, 0.98f, 1f);

    private static BossOverheadBar instance;

    private CanvasGroup group;
    private RectTransform fill, lag;
    private EnemyHealth boss;
    private GolemBossController golem;
    private float ratioAtSplit = -1f;   // 갈라지는 순간의 비율
    private float lastMainRatio = 1f;
    private float shown, lagShown, lagWaitUntil;
    private bool visible;
    private bool placed;
    private readonly List<EnemyHealth> parts = new List<EnemyHealth>();

    public static void Show(EnemyHealth target)
    {
        if (target == null) return;
        if (instance == null) instance = Build();
        BossOverheadBar bar = instance;
        bar.boss = target;
        bar.golem = target.GetComponent<GolemBossController>();
        bar.ratioAtSplit = -1f;
        bar.lastMainRatio = Ratio(target);
        bar.shown = bar.lagShown = bar.lastMainRatio;
        bar.placed = false;
        bar.visible = true;
        bar.Apply();
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.visible = false;
        instance.boss = null;
        instance.golem = null;
    }

    private static float Ratio(EnemyHealth h)
        => h == null || h.MaxHp <= 0f ? 0f : Mathf.Clamp01(h.CurrentHp / h.MaxHp);

    private void LateUpdate()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, visible && boss != null ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
        if (!visible || boss == null) return;

        // 지금 체력 (갈라졌으면 조각 합)
        parts.Clear();
        float now;
        if (golem != null && golem.HasSplit)
        {
            if (ratioAtSplit < 0f) ratioAtSplit = lastMainRatio;
            float cur = 0f, max = 0f;
            foreach (GolemBossController f in golem.Fragments)
            {
                if (f == null) continue;
                EnemyHealth h = f.GetComponent<EnemyHealth>();
                if (h == null) continue;
                max += Mathf.Max(0f, h.MaxHp);
                cur += Mathf.Clamp(h.CurrentHp, 0f, h.MaxHp);
                if (f.gameObject.activeInHierarchy && !h.IsDead) parts.Add(h);
            }
            now = max > 0f ? ratioAtSplit * cur / max : 0f;
        }
        else
        {
            now = Ratio(boss);
            lastMainRatio = now;
            if (boss.gameObject.activeInHierarchy) parts.Add(boss);
        }

        if (now < shown) lagWaitUntil = Time.unscaledTime + LagDelay;
        shown = now;
        if (lagShown < shown) lagShown = shown;
        else if (Time.unscaledTime >= lagWaitUntil)
            lagShown = Mathf.MoveTowards(lagShown, shown, LagSpeed * Time.unscaledDeltaTime);
        Apply();

        // 위치: 살아 있는 몸 위 가운데
        if (parts.Count == 0) return;
        bool found = false;
        Bounds b = default;
        foreach (EnemyHealth h in parts)
        {
            Bounds hb = BodyBounds(h);
            if (!found) { b = hb; found = true; }
            else b.Encapsulate(hb);
        }
        Vector3 target = new Vector3(b.center.x, b.max.y + AboveHead, 0f);
        transform.position = placed
            ? Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f))
            : target;
        placed = true;
    }

    // 몸 크기: 트리거가 아닌 콜라이더 → 아무 콜라이더 → 위치
    private static Bounds BodyBounds(EnemyHealth h)
    {
        Collider2D any = null;
        foreach (Collider2D c in h.GetComponentsInChildren<Collider2D>())
        {
            if (!c.enabled) continue;
            if (!c.isTrigger) return c.bounds;
            if (any == null) any = c;
        }
        if (any != null) return any.bounds;
        return new Bounds(h.transform.position + Vector3.up, Vector3.one * 2f);
    }

    private void Apply()
    {
        fill.anchorMax = new Vector2(shown, 1f);
        lag.anchorMax = new Vector2(lagShown, 1f);
    }

    private static BossOverheadBar Build()
    {
        GameObject go = new GameObject("[BossOverheadBar]", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 50;
        RectTransform root = (RectTransform)go.transform;
        root.sizeDelta = new Vector2(Width * PixelsPerUnit, Height * PixelsPerUnit);
        root.localScale = Vector3.one / PixelsPerUnit;

        BossOverheadBar bar = go.AddComponent<BossOverheadBar>();
        bar.group = go.GetComponent<CanvasGroup>();
        bar.group.alpha = 0f;
        bar.group.interactable = bar.group.blocksRaycasts = false;

        // 테두리 → 바탕 → 옅은 칸 → 밝은 칸
        Image frame = PresentationUI.NewImage("Frame", root, FrameColor);
        PresentationUI.Stretch(frame.rectTransform);
        frame.rectTransform.offsetMin = new Vector2(-3f, -3f);
        frame.rectTransform.offsetMax = new Vector2(3f, 3f);

        Image track = PresentationUI.NewImage("Track", root, TrackColor);
        PresentationUI.Stretch(track.rectTransform);

        bar.lag = Bar("Lag", root, LagColor);
        bar.fill = Bar("Fill", root, FillColor);
        return bar;
    }

    private static RectTransform Bar(string name, RectTransform parent, Color color)
    {
        Image img = PresentationUI.NewImage(name, parent, color);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
