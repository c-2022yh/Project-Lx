using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 획득 알림창. "ㅁㅁ을 획득했습니다"를 화면 가운데에 띄우고 게임을 잠깐 멈춘다.
///
/// 조작
///   Z / Space / Enter : 고른 버튼 누르기 (처음엔 [확인])
///   ← / →             : 버튼 고르기 (유물일 때만 [유물창 보기]가 있다)
///   Esc / X           : 닫기
///   뜬 직후 0.3초는 입력을 받지 않는다. 상자에서 고르던 키가 그대로 넘어오지 않게.
///
/// 유물은 PlayerRelicManager를 지켜보다가 새로 생기면 알아서 띄운다. 시작 유물은 띄우지 않는다.
/// 다른 것(스킬, 열쇠 등)은 AcquirePopup.Show(아이콘, 이름, 설명)으로 부르면 된다.
/// 여러 개가 한꺼번에 오면 줄 세워서 하나씩 보여준다.
///
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생기고 화면도 코드로 만든다.
/// </summary>
public class AcquirePopup : MonoBehaviour
{
    private const float InputGuardSeconds = 0.3f;
    private const float FadeSeconds = 0.15f;

    // 10.28 자개 시안. 색과 그림은 PearlStyle에 모았다.
    private static readonly Color Accent = PearlStyle.Neutral;
    private static readonly Color TextMain = PearlStyle.TextTitle;
    private static readonly Color TextDim = PearlStyle.TextBody;

    // 판 크기와 안쪽 여백. 여백은 창살 모서리 장식을 피할 만큼 둔다.
    private const float PanelWidth = 1080f;    // 긴 유물 이름도 한 줄에 들어가게
    private const float PanelHeight = 440f;
    private const float FrameLine = 4f;         // 바깥 자개 테두리 두께
    private const float LatticeInset = 12f;     // 창살을 자개 안쪽으로 들이는 거리
    private const float LatticeScale = 0.85f;   // 창살 장식 크기
    private const float Inset = 106f;
    private const float ContentTop = 80f;
    private const float MedallionSize = 176f;
    private const float TextLeft = Inset + MedallionSize + 40f;
    private const float ButtonWidth = 188f;
    private const float ButtonHeight = 58f;
    private const float ButtonGap = 18f;
    private const float ButtonBottom = 55f;

    private static AcquirePopup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[AcquirePopup]");
        instance = go.AddComponent<AcquirePopup>();
        DontDestroyOnLoad(go);
    }

    // ── 밖에서 쓰는 것 ───────────────────────

    /// <summary>알림창이 떠 있는지.</summary>
    public static bool IsOpen => instance != null && instance.current != null;

    /// <summary>
    /// 이번 프레임 키 입력을 알림창이 가져갔는지. 닫히는 프레임도 포함한다.
    /// Esc로 닫자마자 일시정지 창이 같이 열리는 일을 막는다.
    /// </summary>
    public static bool BlocksInput =>
        IsOpen || (instance != null && instance.closedFrame == Time.frameCount);

    /// <summary>획득 알림을 띄운다. 이미 떠 있으면 줄 서서 다음에 뜬다.</summary>
    /// <param name="icon">없으면 null. 아이콘 칸이 빠진다.</param>
    /// <param name="itemName">"ㅁㅁ"에 들어갈 이름. 을/를은 알아서 붙는다.</param>
    /// <param name="description">아래 작은 글씨. 없으면 비워도 된다.</param>
    public static void Show(Sprite icon, string itemName, string description)
    {
        Enqueue(new Entry { Icon = icon, Name = itemName, Description = description, Label = "획득", Color = Accent });
    }

    /// <summary>유물 획득 알림. [유물창 보기] 버튼이 같이 나온다.</summary>
    public static void ShowRelic(RelicData relic)
    {
        if (relic == null) return;

        Enqueue(new Entry
        {
            Icon = relic.Icon,
            Name = relic.RelicName,
            Description = relic.Description,
            // 계열 색은 시안의 덜 쨍한 색 (PearlStyle).
            Label = RelicLabels.NameOf(relic.Category),
            Color = PearlStyle.CategoryColor(relic.Category),
            IsRelic = true,
            Relic = relic
        });
    }

    // ── 내부 ────────────────────────────────

    private class Entry
    {
        public Sprite Icon;
        public string Name;
        public string Description;
        public string Label;
        public Color Color;
        public bool IsRelic;
        public RelicData Relic;
    }

    private readonly Queue<Entry> queue = new Queue<Entry>();
    private Entry current;
    private float openedAt;
    private int closedFrame = -1;
    private int selected;               // 0 = 확인, 1 = 유물창 보기
    private bool openRelicWindowAfter;
    private float savedTimeScale = 1f;
    private bool holdingTime;

    // 화면
    private GameObject root;
    private CanvasGroup group;
    private TMP_Text labelText;
    private TMP_Text messageText;
    private TMP_Text descriptionText;
    private TMP_Text hintText;
    private RectTransform iconFrame;
    private Image iconImage;
    private Image confirmBg;
    private Image confirmLine;
    private TMP_Text confirmText;
    private Image viewBg;
    private Image viewLine;
    private TMP_Text viewText;
    private GameObject frameObject;
    private Image frameImage;
    private Image iconFrameImage;
    private Image medallionGlow;
    private Image chipLine;
    private TMP_Text diamondText;
    private Color accent = Accent;

    // 유물 지켜보기
    private PlayerRelicManager relicManager;
    private readonly HashSet<RelicData> knownRelics = new HashSet<RelicData>();
    private float attachedAt;
    private float nextFindAt;

    private static void Enqueue(Entry entry)
    {
        if (instance == null) Bootstrap();

        if (instance.current == null) instance.Open(entry);
        else instance.queue.Enqueue(entry);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (relicManager != null) relicManager.OnRelicsChanged -= OnRelicsChanged;
        if (instance == this) instance = null;
    }

    private void Update()
    {
        WatchRelics();

        if (current == null) return;

        // 다른 곳에서 시간을 되돌려도 떠 있는 동안은 멈춰 둔다.
        if (Time.timeScale != 0f) Time.timeScale = 0f;

        float shown = Time.unscaledTime - openedAt;
        if (group != null) group.alpha = Mathf.Clamp01(shown / FadeSeconds);

        if (shown < InputGuardSeconds) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (current.IsRelic)
        {
            if (kb.leftArrowKey.wasPressedThisFrame) Select(1);
            if (kb.rightArrowKey.wasPressedThisFrame) Select(0);
        }

        if (kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
            kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            Press(selected);
        }
        else if (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame)
        {
            Press(0);
        }
    }

    private void Open(Entry entry)
    {
        EnsureView();

        if (!holdingTime)
        {
            // 처음 뜰 때만 시간을 저장한다. 줄 선 알림이 이어서 뜰 땐 이미 0이다.
            savedTimeScale = HitStop.IsActive ? HitStop.RestoreScale : (Time.timeScale > 0f ? Time.timeScale : 1f);
            holdingTime = true;
        }

        current = entry;
        openedAt = Time.unscaledTime;
        Time.timeScale = 0f;

        string itemName = string.IsNullOrEmpty(entry.Name) ? "???" : entry.Name;
        accent = entry.Color.a > 0f ? entry.Color : Accent;

        // 유물이면 바깥 자개와 안쪽 창살에 계열 색을 섞는다. 다른 것은 흰 자개.
        RelicCategory? category = entry.Relic != null ? entry.Relic.Category : (RelicCategory?)null;
        frameImage.sprite = PearlStyle.Pearl(category, PanelWidth / PanelHeight);
        LatticeFrame.SetColor(frameObject, PearlStyle.LatticeColor(category));

        // 메달 안은 유물 주제 색 + 은은한 빛
        PearlStyle.Theme theme = PearlStyle.ThemeOf(entry.Relic);
        iconFrameImage.color = theme.Background;
        medallionGlow.color = new Color(theme.Glow.r, theme.Glow.g, theme.Glow.b, 0.28f);

        labelText.text = entry.Label;
        labelText.color = accent;
        chipLine.color = new Color(accent.r, accent.g, accent.b, 0.6f);
        diamondText.color = accent;

        messageText.text = $"{itemName}{ObjectParticle(itemName)} 획득했습니다";
        descriptionText.text = entry.Description ?? "";
        hintText.text = entry.IsRelic ? "제단에서 장착할 수 있어요" : "";

        bool hasIcon = entry.Icon != null;
        iconFrame.gameObject.SetActive(hasIcon);
        if (hasIcon) iconImage.sprite = entry.Icon;

        float textLeft = hasIcon ? TextLeft : Inset;
        SetLeft(messageText.rectTransform, textLeft);
        SetLeft(descriptionText.rectTransform, textLeft);

        // 칩은 글자 길이에 맞추고 ◆는 그 옆에
        float chipWidth = labelText.GetPreferredValues(entry.Label).x + 26f;
        RectTransform chipRect = chipLine.rectTransform;
        chipRect.anchoredPosition = new Vector2(textLeft, chipRect.anchoredPosition.y);
        chipRect.sizeDelta = new Vector2(chipWidth, chipRect.sizeDelta.y);
        RectTransform diamondRect = diamondText.rectTransform;
        diamondRect.anchoredPosition = new Vector2(textLeft + chipWidth + 12f, diamondRect.anchoredPosition.y);

        viewBg.gameObject.SetActive(entry.IsRelic);
        Select(0);

        if (group != null) group.alpha = 0f;
        root.SetActive(true);
        GameAudio.Play(GameSound.UIAcquire);

        if (UIManager.Instance != null) UIManager.Instance.RefreshInputBlock();
    }

    private void Press(int button)
    {
        if (current == null) return;

        if (button == 1 && current.IsRelic) openRelicWindowAfter = true;

        current = null;
        closedFrame = Time.frameCount;

        if (queue.Count > 0)
        {
            Open(queue.Dequeue());
            return;
        }

        root.SetActive(false);
        holdingTime = false;
        if (Time.timeScale == 0f) Time.timeScale = savedTimeScale;

        UIManager ui = UIManager.Instance;
        if (ui != null)
        {
            ui.RefreshInputBlock();

            // 보기만 하는 창이다. 장착은 제단에서만 된다.
            if (openRelicWindowAfter && !ui.IsRelicWindowOpen) ui.ShowWindowTab(UITabBar.RelicTab);
        }

        openRelicWindowAfter = false;
    }

    private void Select(int index)
    {
        selected = current != null && current.IsRelic ? Mathf.Clamp(index, 0, 1) : 0;

        Paint(confirmBg, confirmLine, confirmText, selected == 0);
        Paint(viewBg, viewLine, viewText, selected == 1);
    }

    /// <summary>고른 버튼은 자개 판 + 어두운 글자, 아닌 버튼은 투명 판 + 얇은 테두리.</summary>
    private static void Paint(Image bg, Image line, TMP_Text text, bool on)
    {
        if (bg == null) return;

        // 투명해도 마우스 클릭은 받는다.
        bg.color = on ? Color.white : Color.clear;
        if (line != null) line.enabled = !on;
        text.color = on ? PearlStyle.Lacquer : PearlStyle.TextButton;
    }

    /// <summary>받침이 있으면 "을", 없으면 "를". 한글이 아니면 "을(를)".</summary>
    private static string ObjectParticle(string word)
    {
        string trimmed = word.TrimEnd();
        if (trimmed.Length == 0) return "을(를)";

        char last = trimmed[trimmed.Length - 1];
        if (last < 0xAC00 || last > 0xD7A3) return "을(를)";

        return (last - 0xAC00) % 28 == 0 ? "를" : "을";
    }

    // ── 유물 획득 감지 ───────────────────────

    private void WatchRelics()
    {
        if (relicManager != null) return;
        if (Time.unscaledTime < nextFindAt) return;
        nextFindAt = Time.unscaledTime + 0.5f;

        PlayerRelicManager found = FindAnyObjectByType<PlayerRelicManager>();
        if (found == null) return;

        relicManager = found;
        relicManager.OnRelicsChanged += OnRelicsChanged;

        // 붙는 순간 가진 것(시작 유물)은 알림 없이 기준으로 삼는다.
        knownRelics.Clear();
        foreach (RelicData relic in relicManager.OwnedRelics) knownRelics.Add(relic);
        attachedAt = Time.unscaledTime;
    }

    private void OnRelicsChanged()
    {
        if (relicManager == null) return;

        // 붙자마자 들어오는 건 시작 유물이다. 기준에만 넣는다.
        bool silent = Time.unscaledTime - attachedAt < 0.5f;

        foreach (RelicData relic in relicManager.OwnedRelics)
        {
            if (relic == null || !knownRelics.Add(relic)) continue;
            if (silent) continue;
            ShowRelic(relic);
            // 이 유물에 스토리 대화가 있으면 알림창을 닫은 다음 뜬다.
            StoryDialogues.PlayForRelic(relic);
        }
    }

    // ── 화면 만들기 ──────────────────────────

    private void EnsureView()
    {
        if (root != null) return;

        TMP_FontAsset font = FindFont();

        root = new GameObject("AcquirePopup_Canvas", typeof(RectTransform), typeof(Canvas),
                              typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = root.GetComponent<CanvasGroup>();

        // 뒤를 어둡게
        Image dim = NewImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.6f));
        Stretch(dim.rectTransform);

        // 판 뒤 그림자
        Image shadow = NewImage("Shadow", root.transform, new Color(0f, 0f, 0f, 0.55f));
        shadow.sprite = PearlStyle.Glow();
        shadow.raycastTarget = false;
        RectTransform shadowRect = shadow.rectTransform;
        shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(PanelWidth + 300f, PanelHeight + 300f);
        shadowRect.anchoredPosition = new Vector2(0f, -10f);

        // 바깥 자개 테두리 (10.28 시안). 유물이면 계열 색이 살짝 섞인 자개로 바꾼다.
        frameImage = NewImage("Panel", root.transform, Color.white);
        frameImage.sprite = PearlStyle.Pearl(null, PanelWidth / PanelHeight);
        RectTransform panelRect = frameImage.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRect.anchoredPosition = new Vector2(0f, 20f);

        // 옻칠 판
        Image lacquer = NewImage("Lacquer", panelRect, PearlStyle.Lacquer);
        Stretch(lacquer.rectTransform, FrameLine);

        // 창살 테두리는 자개 안쪽에 반투명하게 (10.28 시안). 색은 Open에서 계열 색으로.
        frameObject = LatticeFrame.Attach(lacquer.rectTransform, PanelHeight - FrameLine * 2f,
                                          PearlStyle.LatticeColor(null), -LatticeInset, LatticeScale);

        // 메달: 자개 고리 + 유물 주제 색 + 은은한 빛 + 아이콘
        Image ring = NewImage("Medallion", panelRect, Color.white);
        ring.sprite = PearlStyle.PearlCircle();
        iconFrame = ring.rectTransform;
        Place(iconFrame, Inset, -ContentTop, MedallionSize, MedallionSize);

        iconFrameImage = NewImage("MedallionFill", iconFrame, PearlStyle.Lacquer);
        iconFrameImage.sprite = PearlStyle.Circle();
        Stretch(iconFrameImage.rectTransform, 4f);

        medallionGlow = NewImage("MedallionGlow", iconFrameImage.rectTransform, Color.clear);
        medallionGlow.sprite = PearlStyle.Glow();
        medallionGlow.raycastTarget = false;
        Stretch(medallionGlow.rectTransform, 6f);

        iconImage = NewImage("Icon", iconFrameImage.rectTransform, Color.white);
        iconImage.preserveAspect = true;
        Stretch(iconImage.rectTransform, 22f);

        // 계열 칩 + ◆
        chipLine = NewImage("Chip", panelRect, Accent);
        chipLine.sprite = PearlStyle.Outline();
        chipLine.type = Image.Type.Sliced;
        chipLine.raycastTarget = false;
        Place(chipLine.rectTransform, TextLeft, -(ContentTop + 8f), 120f, 32f);

        labelText = NewText("Label", chipLine.rectTransform, font, 18f, Accent);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.characterSpacing = 8f;
        Stretch(labelText.rectTransform);

        diamondText = NewText("Diamond", panelRect, font, 18f, Accent);
        diamondText.text = "◆";
        Place(diamondText.rectTransform, TextLeft + 132f, -(ContentTop + 8f), 40f, 32f);

        messageText = NewText("Message", panelRect, font, 38f, TextMain);
        messageText.fontStyle = FontStyles.Bold;
        // 그래도 넘치면 잘리지 않고 글자를 조금 줄인다.
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 28f;
        messageText.fontSizeMax = 38f;
        Place(messageText.rectTransform, TextLeft, -(ContentTop + 58f), PanelWidth - Inset - TextLeft, 52f);

        descriptionText = NewText("Description", panelRect, font, 21f, TextDim);
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.lineSpacing = 10f;
        Place(descriptionText.rectTransform, TextLeft, -(ContentTop + 124f), PanelWidth - Inset - TextLeft, 110f);

        hintText = NewText("Hint", panelRect, font, 18f, TextDim);
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = Vector2.zero;
        hintRect.anchoredPosition = new Vector2(Inset, ButtonBottom + (ButtonHeight - 30f) / 2f);
        hintRect.sizeDelta = new Vector2(280f, 30f);

        confirmBg = NewButton("Confirm", panelRect, font, "확인", new Vector2(-Inset, ButtonBottom),
                              out confirmLine, out confirmText, 0);
        viewBg = NewButton("ViewRelics", panelRect, font, "유물창 보기",
                           new Vector2(-Inset - ButtonWidth - ButtonGap, ButtonBottom),
                           out viewLine, out viewText, 1);

        root.SetActive(false);
    }

    private Image NewButton(string name, RectTransform parent, TMP_FontAsset font, string label,
                            Vector2 fromBottomRight, out Image line, out TMP_Text text, int index)
    {
        // 고르면 자개 판 + 어두운 글자, 안 고르면 투명 판 + 얇은 테두리 (Paint에서 바꾼다)
        Image bg = NewImage(name, parent, Color.white);
        bg.sprite = PearlStyle.Pearl(null, ButtonWidth / ButtonHeight);
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = fromBottomRight;
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        line = NewImage("Line", rect, PearlStyle.ButtonLine);
        line.sprite = PearlStyle.Outline();
        line.type = Image.Type.Sliced;
        line.raycastTarget = false;
        Stretch(line.rectTransform);

        Button button = bg.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() =>
        {
            if (current == null || Time.unscaledTime - openedAt < InputGuardSeconds) return;
            Press(index);
        });

        text = NewText("Text", rect, font, 22f, PearlStyle.TextButton);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 4f;
        text.text = label;

        return bg;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = name != "Icon";
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.richText = true;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    /// <summary>판의 왼쪽 위 기준으로 놓는다.</summary>
    private static void Place(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetLeft(RectTransform rect, float left)
    {
        float right = PanelWidth - Inset;
        rect.anchoredPosition = new Vector2(left, rect.anchoredPosition.y);
        rect.sizeDelta = new Vector2(right - left, rect.sizeDelta.y);
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>
    /// 다른 UI와 같은 폰트를 쓴다. 10.19부터 전부 제주명조라서 TMP 기본 폰트(글꼴 통일 도구가 설정)를 먼저 쓴다.
    /// 기본 폰트가 아직 LiberationSans면 예전처럼 나눔스퀘어를 찾는다.
    /// </summary>
    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset main = TMP_Settings.defaultFontAsset;
        if (main != null && !main.name.StartsWith("LiberationSans")) return main;

        TMP_FontAsset fallback = null;

        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font == null) continue;
            if (font.name.StartsWith("NanumSquare_acB")) return font;
            if (fallback == null && font.name.Contains("NanumSquare")) fallback = font;
        }

        if (fallback != null) return fallback;

        TextMeshProUGUI any = FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any.font != null) return any.font;

        return TMP_Settings.defaultFontAsset;
    }

    // ── 에디터 테스트 ────────────────────────

#if UNITY_EDITOR
    [MenuItem("Tools/UI/획득 팝업 테스트")]
    private static void TestPopup()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[AcquirePopup] 플레이 중에만 띄울 수 있어요.");
            return;
        }

        RelicData[] relics = Resources.FindObjectsOfTypeAll<RelicData>();
        if (relics.Length > 0) ShowRelic(relics[Random.Range(0, relics.Length)]);

        Show(null, "시험용 열쇠", "유물이 아닌 것도 이렇게 띄울 수 있어요. [유물창 보기] 버튼은 나오지 않아요.");
    }
#endif
}
