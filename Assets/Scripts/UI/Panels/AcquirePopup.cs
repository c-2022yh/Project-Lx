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

    private static readonly Color Accent = new Color(0.56f, 0.82f, 0.90f, 1f);

    // 판 크기와 안쪽 여백. 여백은 창살 모서리 장식을 피할 만큼 둔다.
    private const float PanelWidth = 880f;
    private const float PanelHeight = 420f;
    private const float Inset = 96f;
    private const float TextLeft = Inset + 128f + 28f;
    private static readonly Color PanelColor = new Color(0.05f, 0.09f, 0.11f, 0.97f);
    private static readonly Color ButtonIdle = new Color(0.12f, 0.19f, 0.22f, 1f);
    private static readonly Color TextMain = new Color(0.90f, 0.93f, 0.95f, 1f);
    private static readonly Color TextDim = new Color(0.62f, 0.70f, 0.74f, 1f);
    private static readonly Color TextOnAccent = new Color(0.04f, 0.07f, 0.08f, 1f);

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
            // 계열마다 유물창 칸 테두리와 같은 색으로 칠한다.
            Label = RelicLabels.NameOf(relic.Category),
            Color = RelicLabels.ColorOf(relic.Category),
            IsRelic = true
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
    private TMP_Text confirmText;
    private Image viewBg;
    private TMP_Text viewText;
    private GameObject frameObject;
    private Image iconFrameImage;
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
        string accentHex = ColorUtility.ToHtmlStringRGB(accent);

        labelText.text = entry.Label;
        labelText.color = accent;
        LatticeFrame.SetColor(frameObject, new Color(accent.r, accent.g, accent.b, 0.9f));
        // 아이콘 칸은 계열 색을 아주 어둡게 깐다.
        if (iconFrameImage != null) iconFrameImage.color = Color.Lerp(ButtonIdle, accent, 0.22f);
        messageText.text = $"<color=#{accentHex}>{itemName}</color>{ObjectParticle(itemName)} 획득했습니다";
        descriptionText.text = entry.Description ?? "";
        hintText.text = entry.IsRelic ? "제단에서 장착할 수 있어요" : "";

        bool hasIcon = entry.Icon != null;
        iconFrame.gameObject.SetActive(hasIcon);
        if (hasIcon) iconImage.sprite = entry.Icon;

        float textLeft = hasIcon ? TextLeft : Inset;
        SetLeft(labelText.rectTransform, Inset);
        SetLeft(messageText.rectTransform, textLeft);
        SetLeft(descriptionText.rectTransform, textLeft);

        viewBg.gameObject.SetActive(entry.IsRelic);
        Select(0);

        if (group != null) group.alpha = 0f;
        root.SetActive(true);

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

        Paint(confirmBg, confirmText, selected == 0);
        Paint(viewBg, viewText, selected == 1);
    }

    private void Paint(Image bg, TMP_Text text, bool on)
    {
        if (bg == null) return;

        bg.color = on ? accent : ButtonIdle;
        text.color = on ? TextOnAccent : TextMain;
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
            if (!silent) ShowRelic(relic);
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
        Image dim = NewImage("Dim", root.transform, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);

        // 판
        Image panel = NewImage("Panel", root.transform, PanelColor);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRect.anchoredPosition = new Vector2(0f, 20f);

        // 창살 테두리 (10.19). 색은 Open에서 유물 계열 색으로 바꾼다.
        frameObject = LatticeFrame.Attach(panelRect, PanelHeight, Accent);

        labelText = NewText("Label", panelRect, font, 22f, Accent);
        Place(labelText.rectTransform, Inset, -46f, 500f, 32f);

        Image frame = NewImage("IconFrame", panelRect, ButtonIdle);
        iconFrameImage = frame;
        iconFrame = frame.rectTransform;
        Place(iconFrame, Inset, -96f, 128f, 128f);

        iconImage = NewImage("Icon", iconFrame, Color.white);
        iconImage.preserveAspect = true;
        Stretch(iconImage.rectTransform, 12f);

        messageText = NewText("Message", panelRect, font, 34f, TextMain);
        Place(messageText.rectTransform, TextLeft, -96f, 540f, 56f);

        descriptionText = NewText("Description", panelRect, font, 22f, TextDim);
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        Place(descriptionText.rectTransform, TextLeft, -162f, 540f, 100f);

        hintText = NewText("Hint", panelRect, font, 20f, TextDim);
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = Vector2.zero;
        hintRect.anchoredPosition = new Vector2(Inset, 70f);
        hintRect.sizeDelta = new Vector2(280f, 30f);

        confirmBg = NewButton("Confirm", panelRect, font, "확인", 180f, new Vector2(-Inset, 52f),
                              out confirmText, 0);
        viewBg = NewButton("ViewRelics", panelRect, font, "유물창 보기", 200f, new Vector2(-Inset - 196f, 52f),
                           out viewText, 1);

        TMP_Text keys = NewText("Keys", root.transform, font, 20f, TextDim);
        RectTransform keysRect = keys.rectTransform;
        keysRect.anchorMin = keysRect.anchorMax = new Vector2(0.5f, 0.5f);
        keysRect.pivot = new Vector2(0.5f, 1f);
        keysRect.anchoredPosition = new Vector2(0f, 20f - PanelHeight / 2f - 30f);
        keysRect.sizeDelta = new Vector2(PanelWidth, 30f);
        keys.alignment = TextAlignmentOptions.Center;
        keys.text = "Z · Space  확인        ← →  선택";

        root.SetActive(false);
    }

    private Image NewButton(string name, RectTransform parent, TMP_FontAsset font, string label,
                            float width, Vector2 fromBottomRight, out TMP_Text text, int index)
    {
        Image bg = NewImage(name, parent, ButtonIdle);
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = fromBottomRight;
        rect.sizeDelta = new Vector2(width, 64f);

        Button button = bg.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() =>
        {
            if (current == null || Time.unscaledTime - openedAt < InputGuardSeconds) return;
            Press(index);
        });

        text = NewText("Text", rect, font, 26f, TextMain);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
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
