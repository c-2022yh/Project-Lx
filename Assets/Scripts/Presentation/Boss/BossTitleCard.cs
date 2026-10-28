using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스가 나올 때 화면 아래쪽에 뜨는 이름 카드.
///
///   BossTitleCard.Show("숲의 주인", "늙은 늑대", 2.4f);
///
/// 칭호(작게) → 이름(크게) → 가로선이 퍼지며 나타났다가 사라진다. 실제 시간으로 흘러서 슬로모션과 상관없다.
/// 씬마다 하나 생기고, 씬이 바뀌면 같이 사라진다.
/// </summary>
public class BossTitleCard : MonoBehaviour
{
    private const float FadeIn = 0.6f;
    private const float FadeOut = 0.7f;
    private const float LineWidth = 760f;

    private static BossTitleCard instance;

    private CanvasGroup group;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI nameText;
    private RectTransform line;
    private Coroutine routine;

    public static void Show(string title, string bossName, float seconds)
    {
        if (instance == null) instance = Build();
        if (instance.routine != null) instance.StopCoroutine(instance.routine);
        instance.routine = instance.StartCoroutine(instance.Play(title, bossName, Mathf.Max(seconds, FadeIn + FadeOut)));
    }

    public static void Hide()
    {
        if (instance == null) return;
        if (instance.routine != null) instance.StopCoroutine(instance.routine);
        instance.routine = null;
        instance.group.alpha = 0f;
    }

    private IEnumerator Play(string title, string bossName, float seconds)
    {
        titleText.text = title ?? "";
        titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        nameText.text = bossName ?? "";

        float hold = seconds - FadeIn - FadeOut;
        float t = 0f;

        while (t < FadeIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / FadeIn);
            float ease = 1f - (1f - k) * (1f - k);
            group.alpha = ease;
            nameText.characterSpacing = Mathf.Lerp(40f, 10f, ease);
            line.sizeDelta = new Vector2(LineWidth * ease, line.sizeDelta.y);
            yield return null;
        }

        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(hold);

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
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.28f);
        root.sizeDelta = new Vector2(1200f, 220f);

        // 글자 뒤를 살짝 어둡게 해서 배경에 묻히지 않게
        Image shade = PresentationUI.NewImage("Shade", root, new Color(0f, 0f, 0f, 0.35f));
        RectTransform shadeRt = shade.rectTransform;
        shadeRt.anchorMin = new Vector2(0f, 0.15f);
        shadeRt.anchorMax = new Vector2(1f, 0.85f);
        shadeRt.offsetMin = shadeRt.offsetMax = Vector2.zero;

        card.titleText = PresentationUI.NewText("Title", root, 28f, PresentationUI.TextDim, TextAlignmentOptions.Center);
        RectTransform titleRt = card.titleText.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.78f);
        titleRt.sizeDelta = new Vector2(1200f, 40f);
        card.titleText.characterSpacing = 12f;

        card.nameText = PresentationUI.NewText("Name", root, 78f, PresentationUI.TextMain, TextAlignmentOptions.Center);
        RectTransform nameRt = card.nameText.rectTransform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0.45f);
        nameRt.sizeDelta = new Vector2(1200f, 100f);

        Image lineImg = PresentationUI.NewImage("Line", root, new Color(0.85f, 0.86f, 0.84f, 0.8f));
        card.line = lineImg.rectTransform;
        card.line.anchorMin = card.line.anchorMax = new Vector2(0.5f, 0.12f);
        card.line.sizeDelta = new Vector2(0f, 2f);

        return card;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
