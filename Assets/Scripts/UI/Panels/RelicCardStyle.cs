using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유물 선택 카드를 시안(10.28 자개 시안)대로 꾸민다. 프리팹은 건드리지 않고 게임 중에 코드로 바꾼다.
///   - 위쪽: 유물 주제 배경색 + 가운데 은은한 빛 + 아이콘
///   - 아래쪽: 어두운 판에 계열 칩, 이름, 설명
///   - 고른 카드: 자개 테두리, 진하게 / 안 고른 카드: 얇은 흐린 테두리, 살짝 흐리게
/// 크기는 화면(캔버스) 높이 720 기준 시안 값에 맞춰 늘린다.
/// RelicCardUI가 알아서 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class RelicCardStyle : MonoBehaviour
{
    // 시안 값 (캔버스 높이 720 기준)
    private const float DesignHeight = 720f;
    private const float CardWidth = 300f;
    private const float CardHeight = 440f;
    private const float CardGap = 44f;
    private const float ArtHeight = 230f;
    private const float IconSize = 150f;
    private const float Pad = 22f;
    private const float UnselectedAlpha = 0.82f;

    private RectTransform rect;
    private Image frame;
    private RectTransform lacquer;
    private Image art;
    private Image glow;
    private Image chipLine;
    private TMP_Text chipText;
    private CanvasGroup group;

    private Image icon;
    private TMP_Text nameText;
    private TMP_Text descText;

    private float unit = 1f;
    private bool selected;
    private bool built;

    public static RelicCardStyle For(Component card)
    {
        RelicCardStyle style = card.GetComponent<RelicCardStyle>();
        if (style == null) style = card.gameObject.AddComponent<RelicCardStyle>();
        return style;
    }

    /// <summary>카드에 유물을 보여줄 때마다 부른다.</summary>
    public void Show(RelicData relic, Image iconImage, TMP_Text relicName, TMP_Text description)
    {
        icon = iconImage;
        nameText = relicName;
        descText = description;

        Build();
        Layout();

        if (relic == null) return;

        PearlStyle.Theme theme = PearlStyle.ThemeOf(relic);
        art.color = theme.Background;
        Color g = theme.Glow;
        g.a = 0.22f;
        glow.color = g;

        Color cat = PearlStyle.CategoryColor(relic.Category);
        chipText.text = RelicLabels.NameOf(relic.Category);
        chipText.color = cat;
        chipLine.color = new Color(cat.r, cat.g, cat.b, 0.6f);
        FitChip();

        Paint();
    }

    /// <summary>고른 카드인지. 크기 키우기는 RelicCardUI가 한다.</summary>
    public void SetSelected(bool on)
    {
        selected = on;
        if (built) Paint();
    }

    // ── 만들기 ──────────────────────────────

    private void Build()
    {
        if (built) return;
        built = true;

        rect = (RectTransform)transform;

        // 예전 흰 카드의 세로 정렬은 끄고 직접 놓는다.
        VerticalLayoutGroup v = GetComponent<VerticalLayoutGroup>();
        if (v != null) v.enabled = false;

        frame = GetComponent<Image>();
        if (frame == null) frame = gameObject.AddComponent<Image>();
        frame.type = Image.Type.Simple;

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        Image lacquerImage = NewImage("Card_Lacquer", transform, PearlStyle.Lacquer, null);
        lacquer = lacquerImage.rectTransform;
        lacquer.SetSiblingIndex(0);

        art = NewImage("Card_Art", lacquer, Color.black, null);
        glow = NewImage("Card_Glow", art.transform, Color.clear, PearlStyle.Glow());

        chipLine = NewImage("Card_Chip", transform, Color.white, PearlStyle.Outline());
        chipLine.type = Image.Type.Sliced;
        chipText = NewText("Text", chipLine.transform, nameText != null ? nameText.font : null);
        chipText.alignment = TextAlignmentOptions.Center;
        Stretch(chipText.rectTransform, 0f);

        if (icon != null)
        {
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        StyleText(nameText, PearlStyle.TextTitle, TextWrappingModes.NoWrap);
        StyleText(descText, PearlStyle.TextBody, TextWrappingModes.Normal);
        if (descText != null) descText.lineSpacing = 12f;
    }

    private void Layout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        float h = DesignHeight;
        if (canvas != null && canvas.rootCanvas != null)
        {
            float rh = ((RectTransform)canvas.rootCanvas.transform).rect.height;
            if (rh > 1f) h = rh;
        }
        unit = h / DesignHeight;
        float u = unit;

        // 카드 크기 (가로 정렬이 이 값을 쓴다)
        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = CardWidth * u;
        le.preferredHeight = CardHeight * u;
        rect.sizeDelta = new Vector2(CardWidth * u, CardHeight * u);

        if (transform.parent != null)
        {
            HorizontalLayoutGroup row = transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (row != null) row.spacing = CardGap * u;

            RectTransform rowRect = transform.parent as RectTransform;
            if (rowRect != null) rowRect.sizeDelta = new Vector2((CardWidth * 3f + CardGap * 2f) * u, CardHeight * u);
        }

        // 위쪽 그림 칸
        RectTransform artRect = art.rectTransform;
        artRect.anchorMin = new Vector2(0f, 1f);
        artRect.anchorMax = new Vector2(1f, 1f);
        artRect.pivot = new Vector2(0.5f, 1f);
        artRect.anchoredPosition = Vector2.zero;
        artRect.sizeDelta = new Vector2(0f, ArtHeight * u);

        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = glowRect.anchorMax = glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(236f * u, 236f * u);

        if (icon != null)
        {
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 1f);
            ir.anchoredPosition = new Vector2(0f, -(ArtHeight - IconSize) / 2f * u);
            ir.sizeDelta = new Vector2(IconSize * u, IconSize * u);
        }

        // 아래쪽 글자
        float top = ArtHeight + 18f;
        chipText.fontSize = 13f * u;
        chipText.characterSpacing = 8f;
        RectTransform chipRect = chipLine.rectTransform;
        TopLeft(chipRect, Pad * u, top * u, 80f * u, 24f * u);
        chipLine.pixelsPerUnitMultiplier = 2f / Mathf.Max(0.5f, u);

        top += 24f + 10f;
        if (nameText != null)
        {
            nameText.fontSize = 26f * u;
            TopLeft(nameText.rectTransform, Pad * u, top * u, (CardWidth - Pad * 2f) * u, 36f * u);
        }

        top += 36f + 8f;
        if (descText != null)
        {
            descText.fontSize = 15f * u;
            TopLeft(descText.rectTransform, Pad * u, top * u, (CardWidth - Pad * 2f) * u, (CardHeight - top - 16f) * u);
        }
    }

    private void FitChip()
    {
        float width = chipText.GetPreferredValues(chipText.text).x + 18f * unit;
        RectTransform r = chipLine.rectTransform;
        r.sizeDelta = new Vector2(width, r.sizeDelta.y);
    }

    private void Paint()
    {
        float u = unit;
        float line = selected ? 3f : 1f;

        if (selected)
        {
            frame.sprite = PearlStyle.Pearl(null, CardWidth / CardHeight);
            frame.color = Color.white;
        }
        else
        {
            frame.sprite = null;
            frame.color = PearlStyle.DimLine;
        }

        lacquer.anchorMin = Vector2.zero;
        lacquer.anchorMax = Vector2.one;
        lacquer.pivot = new Vector2(0.5f, 0.5f);
        lacquer.offsetMin = new Vector2(line * u, line * u);
        lacquer.offsetMax = new Vector2(-line * u, -line * u);

        group.alpha = selected ? 1f : UnselectedAlpha;
    }

    // ── 도우미 ──────────────────────────────

    private static void StyleText(TMP_Text text, Color color, TextWrappingModes wrap)
    {
        if (text == null) return;

        text.color = color;
        text.enableAutoSizing = false;
        text.textWrappingMode = wrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }

    private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void TopLeft(RectTransform r, float left, float top, float width, float height)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(left, -top);
        r.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }
}

/// <summary>유물 선택창 뒤 배경과 조작 안내를 시안대로 맞춘다. 여러 번 불러도 한 번만 만든다.</summary>
public static class RelicSelectionStyle
{
    private const string HintName = "Style_Hint";

    public static void Apply(Transform panel)
    {
        if (panel == null) return;

        float u = 1f;
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.rootCanvas != null)
        {
            float rh = ((RectTransform)canvas.rootCanvas.transform).rect.height;
            if (rh > 1f) u = rh / 720f;
        }

        Transform bg = panel.Find("Background");
        if (bg != null && bg.TryGetComponent(out Image bgImage))
            bgImage.color = new Color(6f / 255f, 9f / 255f, 12f / 255f, 0.66f);

        TMP_FontAsset font = null;
        TMP_Text any = panel.GetComponentInChildren<TMP_Text>(true);
        if (any != null) font = any.font;

        TMP_Text hint = Ensure(panel, HintName, font);
        hint.text = "← →  고르기   ·   Enter  선택";
        hint.fontSize = 15f * u;
        hint.characterSpacing = 6f;
        hint.color = new Color(236f / 255f, 233f / 255f, 228f / 255f, 0.7f);
        Place(hint.rectTransform, 640f * u, 28f * u);
    }

    private static TMP_Text Ensure(Transform panel, string name, TMP_FontAsset font)
    {
        Transform t = panel.Find(name);
        if (t != null && t.TryGetComponent(out TMP_Text found)) return found;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(panel, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>화면 위에서 top만큼 내려온 곳에 가로 전체로 놓는다.</summary>
    private static void Place(RectTransform r, float top, float height)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -top);
        r.sizeDelta = new Vector2(0f, height);
    }
}
