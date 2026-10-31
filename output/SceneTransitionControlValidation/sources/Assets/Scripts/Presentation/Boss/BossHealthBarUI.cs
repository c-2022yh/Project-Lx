using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 아래 보스 HP바 (달빛 방향, 11.01). EnemyHealth의 체력을 매 프레임 읽어서 보여준다.
///
///   BossHealthBarUI.Show(bossHealth, "이끼 거인");          // 절반 표시 없이
///   BossHealthBarUI.Show(bossHealth, "이끼 거인", 0.5f);    // 2페이즈가 되는 자리에 마름모 표시
///   BossHealthBarUI.SetPhaseTwo(true);                      // 달빛 → 주황, 마름모가 켜짐
///   BossHealthBarUI.Hide();
///
/// 가는 선 위에 달빛 칸이 있고, 양끝은 마름모. 맞으면 달빛 칸이 바로 줄고,
/// 뒤의 옅은 칸이 잠깐 있다가 따라 줄어든다. 씬마다 하나 생기고, 씬이 바뀌면 같이 사라진다.
/// 보스 머리 위의 작은 체력바(EnemyHealthBar)는 보스 프리팹에서 빼 두는 게 좋다.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    private const float BarWidth = 1140f;
    private const float FillHeight = 9f;
    private const float FadeSeconds = 0.4f;
    private const float LagDelay = 0.45f;     // 옅은 칸이 따라오기 전에 기다리는 시간
    private const float LagSpeed = 0.9f;      // 초당 줄어드는 비율

    private static readonly Color NameColor = new Color(0.87f, 0.90f, 0.96f, 1f);
    private static readonly Color TrackColor = new Color(0.84f, 0.88f, 0.96f, 0.22f);
    private static readonly Color LagColor = new Color(0.94f, 0.96f, 0.99f, 0.38f);
    private static readonly Color EdgeColor = new Color(0.88f, 0.91f, 0.97f, 0.8f);
    private static readonly Color HoleColor = new Color(0.04f, 0.06f, 0.08f, 1f);

    private static readonly Color MoonFill = new Color(0.91f, 0.93f, 0.98f, 1f);
    private static readonly Color MoonGlow = new Color(0.63f, 0.75f, 1f, 0.5f);
    private static readonly Color WarmFill = new Color(0.94f, 0.55f, 0.33f, 1f);
    private static readonly Color WarmGlow = new Color(1f, 0.55f, 0.31f, 0.6f);
    private static readonly Color MarkLit = new Color(0.94f, 0.63f, 0.38f, 1f);

    private static BossHealthBarUI instance;

    private CanvasGroup group;
    private TextMeshProUGUI nameText;
    private RectTransform fill;
    private RectTransform lag;
    private Image fillImage, glowImage, markHole;
    private RectTransform mark;
    private RectTransform glowFollow;

    private EnemyHealth target;
    private float shown;          // 달빛 칸 비율
    private float lagShown;       // 옅은 칸 비율
    private float lagWaitUntil;
    private bool visible;
    private Coroutine fade;
    private float warm, warmTarget;
    private RectTransform barRoot;          // 바 전체 (갈라졌을 때 위아래로 두 줄)
    private BossHealthBarUI splitBar;       // 골렘이 둘로 갈라지면 두 번째 조각 바

    public static void Show(EnemyHealth boss, string bossName) => Show(boss, bossName, -1f);

    /// <param name="phaseMark">2페이즈가 되는 체력 비율(0~1). 0 이하면 표시 안 함.</param>
    public static void Show(EnemyHealth boss, string bossName, float phaseMark)
    {
        if (boss == null) return;
        if (instance == null) instance = Build();

        BossHealthBarUI ui = instance;
        if (ui.splitBar != null) ui.splitBar.SetVisible(false);
        ui.barRoot.anchoredPosition = new Vector2(0f, 64f);
        ui.target = boss;
        ui.nameText.text = bossName ?? "";
        ui.shown = ui.lagShown = Ratio(boss);
        ui.warm = ui.warmTarget = 0f;

        bool hasMark = phaseMark > 0f && phaseMark < 1f;
        ui.mark.gameObject.SetActive(hasMark);
        if (hasMark)
        {
            ui.mark.anchorMin = ui.mark.anchorMax = new Vector2(phaseMark, 0.5f);
        }

        ui.Apply();
        ui.SetVisible(true);
    }

    public static void SetPhaseTwo(bool on)
    {
        if (instance == null) return;
        instance.warmTarget = on ? 1f : 0f;
        if (instance.splitBar != null) instance.splitBar.warmTarget = instance.warmTarget;
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.SetVisible(false);
        if (instance.splitBar != null) instance.splitBar.SetVisible(false);
    }

    private static float Ratio(EnemyHealth h)
        => h == null || h.MaxHp <= 0f ? 0f : Mathf.Clamp01(h.CurrentHp / h.MaxHp);

    private void Update()
    {
        warm = Mathf.MoveTowards(warm, warmTarget, Time.unscaledDeltaTime / 0.8f);

        if (!visible || target == null)
        {
            ApplyColors();
            return;
        }

        GolemBossController golem = target.GetComponent<GolemBossController>();
        if (this == instance && golem != null && golem.HasSplit && golem.Fragments.Count == 2)
        {
            string label = nameText.text;
            if (splitBar == null) splitBar = Build();
            target = golem.Fragments[0].GetComponent<EnemyHealth>();
            splitBar.target = golem.Fragments[1].GetComponent<EnemyHealth>();
            nameText.text = label + " 1";
            splitBar.nameText.text = label + " 2";
            barRoot.anchoredPosition = new Vector2(0f, 64f);
            splitBar.barRoot.anchoredPosition = new Vector2(0f, 132f);
            shown = lagShown = Ratio(target);
            splitBar.shown = splitBar.lagShown = Ratio(splitBar.target);
            splitBar.mark.gameObject.SetActive(false);   // 2페이즈 표시는 갈라지기 전 바에만
            mark.gameObject.SetActive(false);
            splitBar.warm = warm;
            splitBar.warmTarget = warmTarget;
            splitBar.Apply();
            splitBar.SetVisible(true);
        }

        float now = Ratio(target);
        if (now < shown) lagWaitUntil = Time.unscaledTime + LagDelay;   // 방금 맞음
        shown = now;

        if (lagShown < shown) lagShown = shown;                          // 회복하면 같이 올라감
        else if (Time.unscaledTime >= lagWaitUntil)
            lagShown = Mathf.MoveTowards(lagShown, shown, LagSpeed * Time.unscaledDeltaTime);

        Apply();
    }

    private void Apply()
    {
        fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
        lag.anchorMax = new Vector2(lagShown, lag.anchorMax.y);
        ApplyColors();
    }

    private void ApplyColors()
    {
        float k = warm * warm * (3f - 2f * warm);
        fillImage.color = Color.Lerp(MoonFill, WarmFill, k);
        glowImage.color = Color.Lerp(MoonGlow, WarmGlow, k);
        markHole.color = Color.Lerp(HoleColor, MarkLit, k);
    }

    private void SetVisible(bool on)
    {
        visible = on;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeTo(on ? 1f : 0f));
    }

    private IEnumerator FadeTo(float to)
    {
        float from = group.alpha;
        float t = 0f;
        while (t < FadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / FadeSeconds));
            yield return null;
        }
        group.alpha = to;
        fade = null;
    }

    private static BossHealthBarUI Build()
    {
        Canvas canvas = PresentationUI.NewCanvas("[BossHealthBar]", 410);
        BossHealthBarUI ui = canvas.gameObject.AddComponent<BossHealthBarUI>();
        ui.group = canvas.GetComponent<CanvasGroup>();

        RectTransform root = PresentationUI.NewRect("Bar", canvas.transform);
        ui.barRoot = root;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 70f);
        root.sizeDelta = new Vector2(BarWidth, 18f);

        ui.nameText = PresentationUI.NewText("Name", root, 30f, NameColor, TextAlignmentOptions.BottomLeft);
        RectTransform nameRt = ui.nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.anchoredPosition = new Vector2(2f, 8f);
        nameRt.sizeDelta = new Vector2(0f, 40f);
        ui.nameText.characterSpacing = 18f;
        ui.nameText.fontStyle = FontStyles.Bold;

        // 가는 선 (전체 길이)
        Image track = PresentationUI.NewImage("Track", root, TrackColor);
        Band(track.rectTransform, 2f);

        // 옅은 칸 (방금 깎인 만큼)
        ui.lag = PresentationUI.NewImage("Lag", root, LagColor).rectTransform;
        Band(ui.lag, FillHeight);

        // 달빛 칸 뒤로 번지는 빛 (LateUpdate에서 칸 길이를 따라감)
        ui.glowImage = PresentationUI.NewImage("Glow", root, MoonGlow);
        ui.glowImage.sprite = BossFxArt.SoftRect;
        ui.glowFollow = ui.glowImage.rectTransform;
        ui.glowFollow.pivot = new Vector2(0f, 0.5f);

        // 달빛 칸
        ui.fillImage = PresentationUI.NewImage("Fill", root, MoonFill);
        ui.fill = ui.fillImage.rectTransform;
        Band(ui.fill, FillHeight);

        // 양끝 마름모
        Diamond(root, "EdgeLeft", 0f, 16f, out _);
        Diamond(root, "EdgeRight", 1f, 16f, out _);

        // 2페이즈 자리 마름모
        ui.mark = Diamond(root, "PhaseMark", 0.5f, 20f, out ui.markHole);

        return ui;
    }

    private void LateUpdate()
    {
        // 빛은 달빛 칸과 같은 길이로 따라간다
        if (glowFollow == null) return;
        glowFollow.anchorMin = new Vector2(0f, 0.5f);
        glowFollow.anchorMax = new Vector2(shown, 0.5f);
        glowFollow.sizeDelta = new Vector2(20f, FillHeight + 24f);
        glowFollow.anchoredPosition = new Vector2(-10f, 0f);
    }

    private static void Band(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }

    private static RectTransform Diamond(RectTransform parent, string name, float x, float size, out Image hole)
    {
        Image edge = PresentationUI.NewImage(name, parent, EdgeColor);
        edge.sprite = BossFxArt.Diamond;
        RectTransform rt = edge.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        hole = PresentationUI.NewImage("Hole", rt, HoleColor);
        hole.sprite = BossFxArt.Diamond;
        RectTransform h = hole.rectTransform;
        h.anchorMin = h.anchorMax = new Vector2(0.5f, 0.5f);
        h.sizeDelta = new Vector2(size - 5f, size - 5f);
        return rt;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
