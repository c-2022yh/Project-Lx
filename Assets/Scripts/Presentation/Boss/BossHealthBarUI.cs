using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 아래 보스 HP바. EnemyHealth의 체력을 매 프레임 읽어서 보여준다.
///
///   BossHealthBarUI.Show(bossHealth, "늙은 늑대");
///   BossHealthBarUI.Hide();
///
/// 맞으면 빨간 칸이 바로 줄고, 뒤의 밝은 칸이 잠깐 있다가 따라 줄어든다.
/// 씬마다 하나 생기고, 씬이 바뀌면 같이 사라진다.
/// 보스 머리 위의 작은 체력바(EnemyHealthBar)는 보스 프리팹에서 빼 두는 게 좋다.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    private const float BarWidth = 1100f;
    private const float BarHeight = 14f;
    private const float FadeSeconds = 0.4f;
    private const float LagDelay = 0.45f;     // 밝은 칸이 따라오기 전에 기다리는 시간
    private const float LagSpeed = 0.9f;      // 초당 줄어드는 비율

    private static readonly Color FrameColor = new Color(0.04f, 0.05f, 0.06f, 0.85f);
    private static readonly Color BackColor = new Color(0.12f, 0.12f, 0.13f, 0.9f);
    private static readonly Color FillColor = new Color(0.68f, 0.14f, 0.13f, 1f);
    private static readonly Color LagColor = new Color(0.93f, 0.86f, 0.78f, 0.9f);

    private static BossHealthBarUI instance;

    private CanvasGroup group;
    private TextMeshProUGUI nameText;
    private RectTransform fill;
    private RectTransform lag;

    private EnemyHealth target;
    private float shown;          // 빨간 칸 비율
    private float lagShown;       // 밝은 칸 비율
    private float lagWaitUntil;
    private bool visible;
    private Coroutine fade;
    private RectTransform barRoot;
    private BossHealthBarUI splitBar;

    public static void Show(EnemyHealth boss, string bossName)
    {
        if (boss == null) return;
        if (instance == null) instance = Build();

        BossHealthBarUI ui = instance;
        if (ui.splitBar != null) ui.splitBar.SetVisible(false);
        ui.barRoot.anchoredPosition = new Vector2(0f, 64f);
        ui.target = boss;
        ui.nameText.text = bossName ?? "";
        ui.shown = ui.lagShown = Ratio(boss);
        ui.Apply();
        ui.SetVisible(true);
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
        if (!visible || target == null) return;

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
        fill.anchorMax = new Vector2(shown, 1f);
        lag.anchorMax = new Vector2(lagShown, 1f);
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
        root.anchoredPosition = new Vector2(0f, 64f);
        root.sizeDelta = new Vector2(BarWidth, BarHeight);

        ui.nameText = PresentationUI.NewText("Name", root, 30f, PresentationUI.TextMain, TextAlignmentOptions.BottomLeft);
        RectTransform nameRt = ui.nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.anchoredPosition = new Vector2(4f, 8f);
        nameRt.sizeDelta = new Vector2(0f, 40f);
        ui.nameText.characterSpacing = 6f;

        Image frame = PresentationUI.NewImage("Frame", root, FrameColor);
        RectTransform frameRt = frame.rectTransform;
        PresentationUI.Stretch(frameRt);
        frameRt.offsetMin = new Vector2(-3f, -3f);
        frameRt.offsetMax = new Vector2(3f, 3f);

        Image back = PresentationUI.NewImage("Back", root, BackColor);
        PresentationUI.Stretch(back.rectTransform);

        ui.lag = PresentationUI.NewImage("Lag", root, LagColor).rectTransform;
        PresentationUI.Stretch(ui.lag);

        ui.fill = PresentationUI.NewImage("Fill", root, FillColor).rectTransform;
        PresentationUI.Stretch(ui.fill);

        // 위쪽 1px 하이라이트
        Image shine = PresentationUI.NewImage("Shine", ui.fill, new Color(1f, 1f, 1f, 0.18f));
        RectTransform shineRt = shine.rectTransform;
        shineRt.anchorMin = new Vector2(0f, 0.65f);
        shineRt.anchorMax = Vector2.one;
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;

        return ui;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
