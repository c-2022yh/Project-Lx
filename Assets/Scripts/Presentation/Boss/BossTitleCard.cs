using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스가 나올 때 화면 아래쪽에 뜨는 이름 카드 (달빛 방향, 11.01).
///
///   BossTitleCard.Show("숲 입구를 지키는 돌", "이끼 거인", 2.6f);
///   BossTitleCard.ShowDefeat("이끼 거인", 1.8f);   // "격파" 카드 (아래에 보스 이름)
///
/// 어두운 띠 → 칭호(작게) → 이름이 넓게 퍼졌다 좁혀지며 → 달빛 줄이 가운데서 퍼지고 → 먹 붓줄이 그어진다.
/// 실제 시간으로 흘러서 슬로모션과 상관없다. 씬마다 하나 생기고, 씬이 바뀌면 같이 사라진다.
/// </summary>
public class BossTitleCard : MonoBehaviour
{
    private const float FadeIn = 0.6f;
    private const float FadeOut = 0.7f;
    private const float LineWidth = 1140f;
    private const float InkWidth = 520f;

    private static readonly Color TitleColor = new Color(0.80f, 0.85f, 0.94f, 0.78f);
    private static readonly Color NameColor = new Color(0.93f, 0.95f, 0.98f, 1f);
    private static readonly Color GlowColor = new Color(0.59f, 0.75f, 1f, 0.16f);
    private static readonly Color LineColor = new Color(0.86f, 0.91f, 1f, 0.9f);
    private static readonly Color InkColor = new Color(0.88f, 0.90f, 0.97f, 0.7f);

    private static BossTitleCard instance;

    private CanvasGroup group;
    private CanvasGroup titleGroup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI subText;
    private RectTransform line;
    private Image ink;
    private Coroutine routine;

    public static void Show(string title, string bossName, float seconds)
    {
        Begin(title, bossName, null, seconds);
    }

    /// <summary>"격파" 카드. 큰 글씨는 격파, 줄 아래 작게 보스 이름.</summary>
    public static void ShowDefeat(string bossName, float seconds)
    {
        Begin(null, "격파", bossName, seconds);
    }

    private static void Begin(string title, string big, string sub, float seconds)
    {
        if (instance == null) instance = Build();
        if (instance.routine != null) instance.StopCoroutine(instance.routine);
        instance.routine = instance.StartCoroutine(instance.Play(title, big, sub, Mathf.Max(seconds, FadeIn + FadeOut)));
    }

    public static void Hide()
    {
        if (instance == null) return;
        if (instance.routine != null) instance.StopCoroutine(instance.routine);
        instance.routine = null;
        instance.group.alpha = 0f;
    }

    private IEnumerator Play(string title, string big, string sub, float seconds)
    {
        titleText.text = title ?? "";
        titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        nameText.text = big ?? "";
        subText.text = sub ?? "";
        subText.gameObject.SetActive(!string.IsNullOrEmpty(sub));
        subText.alpha = 0f;
        ink.fillAmount = 0f;
        line.localScale = new Vector3(0f, 1f, 1f);

        float hold = seconds - FadeIn - FadeOut;
        float t = 0f;
        float total = Mathf.Max(FadeIn, 1.3f);   // 줄·붓줄은 페이드보다 조금 길게

        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / FadeIn);
            float ease = 1f - (1f - k) * (1f - k);
            group.alpha = ease;

            // 칭호는 살짝 먼저, 이름은 넓게 퍼졌다가 좁혀진다
            titleGroup.alpha = Mathf.Clamp01(t / 0.4f);
            float s = Mathf.Clamp01(t / 0.9f);
            nameText.characterSpacing = Mathf.Lerp(46f, 18f, 1f - (1f - s) * (1f - s));

            float lk = Mathf.Clamp01((t - 0.3f) / 0.9f);
            line.localScale = new Vector3(1f - (1f - lk) * (1f - lk) * (1f - lk), 1f, 1f);

            ink.fillAmount = Mathf.Clamp01((t - 0.6f) / 0.5f);
            if (subText.gameObject.activeSelf) subText.alpha = Mathf.Clamp01((t - 0.7f) / 0.5f);
            yield return null;
        }

        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, hold - (total - FadeIn)));

        t = 0f;
        while (t < FadeOut)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / FadeOut);
            yield return null;
        }

        group.alpha = 0f;
        routine = null;
    }

    private static BossTitleCard Build()
    {
        Canvas canvas = PresentationUI.NewCanvas("[BossTitleCard]", 420);
        BossTitleCard card = canvas.gameObject.AddComponent<BossTitleCard>();
        card.group = canvas.GetComponent<CanvasGroup>();

        RectTransform root = PresentationUI.NewRect("Card", canvas.transform);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.3f);
        root.sizeDelta = new Vector2(1920f, 300f);

        // 위아래로 흐려지는 어두운 띠 (화면 가로 전체)
        Image band = PresentationUI.NewImage("Band", root, new Color(0.01f, 0.02f, 0.03f, 0.78f));
        band.sprite = BossFxArt.BandV;
        RectTransform bandRt = band.rectTransform;
        bandRt.anchorMin = new Vector2(0f, 0f);
        bandRt.anchorMax = new Vector2(1f, 1f);
        bandRt.offsetMin = new Vector2(-200f, 0f);
        bandRt.offsetMax = new Vector2(200f, 0f);

        // 이름 뒤 은은한 달빛
        Image glow = PresentationUI.NewImage("Glow", root, GlowColor);
        glow.sprite = BossFxArt.SoftDot;
        RectTransform glowRt = glow.rectTransform;
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.52f);
        glowRt.sizeDelta = new Vector2(1000f, 220f);

        RectTransform titleRoot = PresentationUI.NewRect("TitleGroup", root);
        titleRoot.anchorMin = titleRoot.anchorMax = new Vector2(0.5f, 0.8f);
        titleRoot.sizeDelta = new Vector2(1400f, 40f);
        card.titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
        card.titleGroup.interactable = card.titleGroup.blocksRaycasts = false;

        card.titleText = PresentationUI.NewText("Title", titleRoot, 27f, TitleColor, TextAlignmentOptions.Center);
        PresentationUI.Stretch(card.titleText.rectTransform);
        card.titleText.characterSpacing = 30f;

        card.nameText = PresentationUI.NewText("Name", root, 92f, NameColor, TextAlignmentOptions.Center);
        RectTransform nameRt = card.nameText.rectTransform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0.52f);
        nameRt.sizeDelta = new Vector2(1600f, 120f);
        card.nameText.fontStyle = FontStyles.Bold;

        // 가운데서 양옆으로 퍼지는 달빛 줄
        Image lineImg = PresentationUI.NewImage("Line", root, LineColor);
        lineImg.sprite = BossFxArt.FadeLine;
        card.line = lineImg.rectTransform;
        card.line.anchorMin = card.line.anchorMax = new Vector2(0.5f, 0.24f);
        card.line.sizeDelta = new Vector2(LineWidth, 3f);

        // 줄 바로 아래 짧은 먹 붓줄 (메뉴 고른 표시와 같은 붓줄)
        card.ink = PresentationUI.NewImage("Ink", root, InkColor);
        card.ink.sprite = BossFxArt.Ink;
        card.ink.type = Image.Type.Filled;
        card.ink.fillMethod = Image.FillMethod.Horizontal;
        card.ink.fillOrigin = (int)Image.OriginHorizontal.Left;
        RectTransform inkRt = card.ink.rectTransform;
        inkRt.anchorMin = inkRt.anchorMax = new Vector2(0.5f, 0.24f);
        inkRt.anchoredPosition = new Vector2(0f, -12f);
        inkRt.sizeDelta = new Vector2(InkWidth, 34f);   // 붓 자국 그림 비율

        // 격파 카드에서 줄 아래에 나오는 보스 이름
        card.subText = PresentationUI.NewText("Sub", root, 27f, TitleColor, TextAlignmentOptions.Center);
        RectTransform subRt = card.subText.rectTransform;
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.24f);
        subRt.anchoredPosition = new Vector2(0f, -50f);
        subRt.sizeDelta = new Vector2(1400f, 40f);
        card.subText.characterSpacing = 20f;
        card.subText.gameObject.SetActive(false);

        return card;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
