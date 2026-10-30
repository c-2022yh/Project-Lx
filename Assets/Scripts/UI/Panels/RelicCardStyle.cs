using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유물 선택 카드를 족자로 꾸민다 (10.29 족자 시안). 프리팹은 건드리지 않고 게임 중에 코드로 바꾼다.
///   - 위: 못에 건 끈 두 가닥 → 종이로 감싼 윗막대
///   - 가운데: 유물 색 비단 테두리 + 한지 + 모서리 사각 무늬 테두리선
///             세로 이름 → 수채화 그림 + 아이콘 → 계열 → 설명
///   - 아래: 종이로 감싼 아랫봉, 양 끝만 나무 마개
///   - 창이 열리면 세 장이 같이 위에서 아래로 펼쳐지고 살짝 흔들리다 멈춘다.
///   - 고른 족자: 또렷하게 + 살짝 내려옴 / 나머지: 흐리게
/// 크기는 화면(캔버스) 높이 720 기준 시안 값에 맞춰 늘린다. RelicCardUI가 알아서 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class RelicCardStyle : MonoBehaviour
{
    // 시안 값 (캔버스 높이 720 기준)
    private const float DesignHeight = 720f;
    private const float BoxW = 300f, BoxH = 580f, Gap = 40f;
    private const float MountX = 40f, MountTop = 76f, MountW = 220f, MountH = 452f;
    private const float PadX = 12f, PadTop = 14f, PadBottom = 16f;
    private const float TopBarX = 36f, TopBarY = 64f, TopBarW = 228f, TopBarH = 16f;
    private const float RodY = 516f, RodX = 38f, RodW = 224f, RodH = 24f, KnobSize = 18f;
    private const float FretCorner = 36f;   // 무늬 테두리 모서리 크기
    private const float Lift = 10f;
    private const float UnselectedAlpha = 0.72f;

    // 모션 (실제 시간, 초)
    private const float AppearSeconds = 0.22f;
    private const float UnrollSeconds = 0.62f;
    private const float SwaySeconds = 0.9f;

    private static readonly Color Ink = new Color(0.17f, 0.15f, 0.13f, 1f);
    private static readonly Color InkSoft = new Color(0.31f, 0.28f, 0.25f, 1f);
    private static readonly Color Cord = new Color(0.23f, 0.17f, 0.14f, 1f);
    private static readonly Color Wood = new Color(0.23f, 0.17f, 0.13f, 1f);

    private RectTransform rect;
    private CanvasGroup group;
    private RectTransform scroll;      // 고르면 살짝 내려오는 부분
    private RectTransform sway;        // 흔들리는 부분 (위 가운데 기준)
    private RectTransform unroll;      // 펼쳐지며 늘어나는 가림막
    private RectMask2D unrollMask;
    private RectTransform rodBottom;
    private Image mount, topBar, rodBar, fret, washFrame, wash;
    private TMP_Text nameText, categoryText, descText;
    private Image icon;

    private float unit = 1f;
    private bool selected;
    private bool built;
    private float appear = 1f;          // 나타나는 중 투명도
    private float appearOffset;         // 나타나는 중 위에서 내려오는 거리
    private float liftNow;
    private Coroutine motion;

    public static RelicCardStyle For(Component card)
    {
        RelicCardStyle style = card.GetComponent<RelicCardStyle>();
        if (style == null) style = card.gameObject.AddComponent<RelicCardStyle>();
        return style;
    }

    /// <summary>카드에 유물을 보여줄 때마다 부른다. 펼치는 모션도 처음부터 다시 한다.</summary>
    public void Show(RelicData relic, Image iconImage, TMP_Text relicName, TMP_Text description)
    {
        icon = iconImage;
        nameText = relicName;
        descText = description;

        Build();
        Layout();

        if (relic != null)
        {
            PearlStyle.Theme theme = PearlStyle.ThemeOf(relic);
            mount.color = theme.Silk;
            topBar.color = theme.Silk;
            rodBar.color = theme.Silk;
            fret.color = theme.Line;
            washFrame.color = theme.Line;
            wash.sprite = PearlStyle.Load("UIFrame/Scroll/Scroll_WC_" + theme.Wash);

            // 세로 쓰기: 글자 사이에 줄바꿈을 넣는다.
            if (nameText != null) nameText.text = Vertical(relic.RelicName);

            categoryText.text = RelicLabels.NameOf(relic.Category);
            categoryText.color = PearlStyle.CategoryInk(relic.Category);
        }

        if (isActiveAndEnabled)
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Unroll());
        }

        Apply();
    }

    /// <summary>고른 족자인지.</summary>
    public void SetSelected(bool on)
    {
        selected = on;
        if (built) Apply();
    }

    private void OnDisable()
    {
        motion = null;
    }

    private void Update()
    {
        if (!built) return;

        // 고른 족자는 살짝 내려온다 (게임이 멈춘 동안에도 움직이게 실제 시간 사용)
        float target = selected ? Lift : 0f;
        float k = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
        liftNow = Mathf.Lerp(liftNow, target, k);
        Apply();
    }

    private void Apply()
    {
        if (!built) return;

        group.alpha = appear * (selected ? 1f : UnselectedAlpha);
        scroll.anchoredPosition = new Vector2(0f, -(liftNow + appearOffset) * unit);
    }

    // ── 모션 ────────────────────────────────

    private IEnumerator Unroll()
    {
        float u = unit;
        float startY = TopBarY;

        // 말린 상태: 가림막 높이 0, 아랫봉은 윗막대 바로 밑
        unrollMask.enabled = true;
        SetUnroll(0f);
        sway.localRotation = Quaternion.identity;

        for (float t = 0f; t < AppearSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / AppearSeconds;
            appear = k;
            appearOffset = -18f * (1f - k);
            Apply();
            yield return null;
        }
        appear = 1f;
        appearOffset = 0f;
        Apply();

        for (float t = 0f; t < UnrollSeconds; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / UnrollSeconds, 3f);
            SetUnroll(k);
            yield return null;
        }
        SetUnroll(1f);

        // 다 펼친 뒤에는 가림막을 끈다 (기울어질 때 가장자리가 잘리지 않게)
        unrollMask.enabled = false;

        for (float t = 0f; t < SwaySeconds; t += Time.unscaledDeltaTime)
        {
            sway.localRotation = Quaternion.Euler(0f, 0f, SwayAngle(t / SwaySeconds));
            yield return null;
        }
        sway.localRotation = Quaternion.identity;
        motion = null;

        void SetUnroll(float k)
        {
            unroll.sizeDelta = new Vector2(MountW * u, MountH * u * k);
            rodBottom.anchoredPosition = new Vector2(0f, -Mathf.Lerp(startY, RodY, k) * u);
        }
    }

    /// <summary>0 → 1도 → -0.55도 → 0. 매달린 종이가 툭 펼쳐진 뒤 흔들리다 멈추는 모양.</summary>
    private static float SwayAngle(float x)
    {
        if (x < 0.35f) return Mathf.SmoothStep(0f, 1f, x / 0.35f);
        if (x < 0.65f) return Mathf.SmoothStep(1f, -0.55f, (x - 0.35f) / 0.3f);
        return Mathf.SmoothStep(-0.55f, 0f, (x - 0.65f) / 0.35f);
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

        // 카드 바탕은 안 보이게 하되 마우스 클릭은 받게 둔다.
        Image bg = GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = null;
            bg.color = Color.clear;
        }

        // 족자에는 「 」 선택 테두리를 그리지 않는다.
        if (GetComponent<NoSelectionBrackets>() == null) gameObject.AddComponent<NoSelectionBrackets>();
        foreach (Selectable s in GetComponentsInChildren<Selectable>(true))
            if (s.GetComponent<NoSelectionBrackets>() == null) s.gameObject.AddComponent<NoSelectionBrackets>();

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        Sprite paper = PearlStyle.Load("UIFrame/Scroll/Scroll_Paper");
        Sprite shade = PearlStyle.Load("UIFrame/Scroll/Scroll_RodShade");
        Sprite circle = PearlStyle.Circle();

        scroll = NewRect("Scroll", transform);
        scroll.SetSiblingIndex(0);
        sway = NewRect("Sway", scroll);

        // 끈과 못
        NewImage("CordLeft", sway, Cord, null);
        NewImage("CordRight", sway, Cord, null);
        NewImage("Nail", sway, Cord, circle);

        // 펼쳐지는 부분 (비단 + 한지)
        unroll = NewRect("Unroll", sway);
        unrollMask = unroll.gameObject.AddComponent<RectMask2D>();

        mount = NewImage("Mount", unroll, Color.white, paper);
        Image paperImage = NewImage("Paper", mount.transform, Color.white, paper);
        fret = NewImage("Fret", paperImage.transform, Color.white, PearlStyle.Load("UIFrame/Scroll/Scroll_Fret"));
        fret.type = Image.Type.Sliced;
        fret.fillCenter = false;

        if (nameText != null) nameText.transform.SetParent(paperImage.transform, false);

        washFrame = NewImage("WashFrame", paperImage.transform, Color.white, PearlStyle.Outline());
        washFrame.type = Image.Type.Sliced;
        washFrame.fillCenter = false;
        wash = NewImage("Wash", washFrame.transform, Color.white, null);
        wash.preserveAspect = false;

        if (icon != null)
        {
            icon.transform.SetParent(wash.transform, false);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        categoryText = NewText("Category", paperImage.transform, nameText != null ? nameText.font : null);
        if (descText != null) descText.transform.SetParent(paperImage.transform, false);

        // 윗막대 (종이로 감쌈)
        topBar = NewImage("TopBar", sway, Color.white, paper);
        Stretch(NewImage("Shade", topBar.transform, Color.white, shade).rectTransform);

        // 아랫봉: 양 끝 나무 마개 + 종이로 감싼 봉
        rodBottom = NewRect("RodBottom", sway);
        NewImage("KnobLeft", rodBottom, Wood, null);
        NewImage("KnobRight", rodBottom, Wood, null);
        rodBar = NewImage("Rod", rodBottom, Color.white, paper);
        Stretch(NewImage("Shade", rodBar.transform, Color.white, shade).rectTransform);

        StyleText(nameText, Ink, TextAlignmentOptions.Top, FontStyles.Bold);
        StyleText(categoryText, Ink, TextAlignmentOptions.Top, FontStyles.Bold);
        StyleText(descText, InkSoft, TextAlignmentOptions.Top, FontStyles.Normal);
        if (descText != null)
        {
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.lineSpacing = 8f;
        }
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

        // 족자 한 장 크기 (가로 정렬이 이 값을 쓴다)
        LayoutElement le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = BoxW * u;
        le.preferredHeight = BoxH * u;
        rect.sizeDelta = new Vector2(BoxW * u, BoxH * u);

        if (transform.parent != null)
        {
            HorizontalLayoutGroup row = transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (row != null) row.spacing = Gap * u;

            RectTransform rowRect = transform.parent as RectTransform;
            if (rowRect != null) rowRect.sizeDelta = new Vector2((BoxW * 3f + Gap * 2f) * u, BoxH * u);
        }

        // 위 가운데를 기준으로 걸어 둔다.
        TopCenter(scroll, 0f, 0f, BoxW * u, BoxH * u);
        TopCenter(sway, 0f, 0f, BoxW * u, BoxH * u);

        // 끈 두 가닥: 못(150, 12) → 윗막대 (66, 70) / (234, 70)
        Cable(sway.Find("CordLeft") as RectTransform, new Vector2(150f, 12f), new Vector2(66f, 70f), 1.8f, u);
        Cable(sway.Find("CordRight") as RectTransform, new Vector2(150f, 12f), new Vector2(234f, 70f), 1.8f, u);
        TopLeft(sway.Find("Nail") as RectTransform, 144f * u, 5f * u, 12f * u, 12f * u);

        // 펼쳐지는 가림막은 위에 붙어서 아래로 늘어난다.
        TopLeft(unroll, MountX * u, MountTop * u, MountW * u, MountH * u);
        TopLeft(mount.rectTransform, 0f, 0f, MountW * u, MountH * u);

        RectTransform paperRect = (RectTransform)mount.transform.Find("Paper");
        TopLeft(paperRect, PadX * u, PadTop * u, (MountW - PadX * 2f) * u, (MountH - PadTop - PadBottom) * u);
        Stretch(fret.rectTransform);
        // 테두리 그림은 모서리 72px = 시안 36 → 배율을 맞춘다.
        fret.pixelsPerUnitMultiplier = 72f / (FretCorner * u);

        float innerW = MountW - PadX * 2f;   // 196

        if (nameText != null)
        {
            nameText.fontSize = 21f * u;
            nameText.lineSpacing = -18f;
            TopLeft(nameText.rectTransform, (innerW - 40f) / 2f * u, 40f * u, 40f * u, 112f * u);
        }

        float washW = 140f, washH = 116f;
        TopLeft(washFrame.rectTransform, (innerW - washW - 8f) / 2f * u, 162f * u, (washW + 8f) * u, (washH + 8f) * u);
        washFrame.pixelsPerUnitMultiplier = 2f / Mathf.Max(0.5f, u);
        Stretch(wash.rectTransform, 4f * u);

        if (icon != null)
        {
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0.5f, 0.5f);
            ir.anchoredPosition = Vector2.zero;
            ir.sizeDelta = new Vector2(92f * u, 92f * u);
        }

        categoryText.fontSize = 12f * u;
        categoryText.characterSpacing = 8f;
        TopLeft(categoryText.rectTransform, 0f, 304f * u, innerW * u, 18f * u);

        if (descText != null)
        {
            descText.fontSize = 12.5f * u;
            TopLeft(descText.rectTransform, 28f * u, 332f * u, (innerW - 56f) * u, 80f * u);
        }

        // 윗막대
        TopLeft(topBar.rectTransform, TopBarX * u, TopBarY * u, TopBarW * u, TopBarH * u);

        // 아랫봉
        TopLeft(rodBottom, 0f, RodY * u, BoxW * u, RodH * u);
        TopLeft(rodBottom.Find("KnobLeft") as RectTransform, 22f * u, 3f * u, KnobSize * u, KnobSize * u);
        TopLeft(rodBottom.Find("KnobRight") as RectTransform, (BoxW - 22f - KnobSize) * u, 3f * u, KnobSize * u, KnobSize * u);
        TopLeft(rodBar.rectTransform, RodX * u, 0f, RodW * u, RodH * u);

        // 그리는 순서: 끈 → 펼친 종이 → 윗막대 → 아랫봉 (봉이 종이 위에 온다)
        unroll.SetAsLastSibling();
        topBar.transform.SetAsLastSibling();
        rodBottom.SetAsLastSibling();
    }

    // ── 도우미 ──────────────────────────────

    private static string Vertical(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length * 2);
        foreach (char c in s.Replace(" ", ""))
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static void Cable(RectTransform r, Vector2 from, Vector2 to, float thickness, float u)
    {
        if (r == null) return;

        // 시안 좌표는 위에서 아래로 증가한다.
        Vector2 a = new Vector2(from.x, -from.y) * u;
        Vector2 b = new Vector2(to.x, -to.y) * u;
        Vector2 d = b - a;

        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = a;
        r.sizeDelta = new Vector2(d.magnitude, thickness * u);
        r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    private static void StyleText(TMP_Text text, Color color, TextAlignmentOptions align, FontStyles style)
    {
        if (text == null) return;

        text.color = color;
        text.enableAutoSizing = false;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = align;
        text.raycastTarget = false;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
    {
        RectTransform r = NewRect(name, parent);
        Image image = r.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font)
    {
        RectTransform r = NewRect(name, parent);
        TextMeshProUGUI text = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.raycastTarget = false;
        return text;
    }

    private static void TopLeft(RectTransform r, float left, float top, float width, float height)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(left, -top);
        r.sizeDelta = new Vector2(width, height);
    }

    private static void TopCenter(RectTransform r, float x, float top, float width, float height)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(x, -top);
        r.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform r, float inset = 0f)
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
        Place(hint.rectTransform, 668f * u, 28f * u);
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
