using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 스토리 대화창 (10.29 대화창 시안). DialogueBox.Play(대화)로 띄운다.
///
/// 대화창(Box)
///   화면 아래 자개 테두리 창. 위에 이름표, 왼쪽에 동그란 초상화, 글자는 한 글자씩 나온다.
/// 내레이션(Narration)
///   화면이 어두워지고 가운데에 글이 한 줄씩 떠오른다. 앞 줄은 위에 흐리게 남는다.
///
/// 조작
///   Z / Space / Enter / 클릭 : 글자가 나오는 중이면 한 줄을 다 보여주고, 다 나왔으면 다음 줄
///   Esc                      : 대화 건너뛰기
///   뜬 직후 0.25초는 입력을 받지 않는다. 공격하던 Z가 그대로 넘어오지 않게.
///
/// 떠 있는 동안 게임을 멈춘다. 획득 알림창·유물 선택창이 떠 있으면 닫힐 때까지 기다렸다가 뜬다.
/// 씬에도 프리팹에도 붙이지 않는다. 게임이 시작되면 스스로 생기고 화면도 코드로 만든다.
/// </summary>
public class DialogueBox : MonoBehaviour
{
    private const float InputGuardSeconds = 0.25f;
    private const float FadeSeconds = 0.2f;
    private const float CharsPerSecond = 30f;
    private const float NarrationFadeSeconds = 0.7f;

    // 1920×1080 기준 크기 (시안 1280×720의 1.5배)
    private const float BoxSide = 180f;
    private const float BoxBottom = 66f;
    private const float BoxHeight = 250f;
    private const float PortraitSize = 186f;
    private const float TextLeftWithPortrait = 270f;
    private const float TextLeftNoPortrait = 60f;

    private static readonly Color PanelColor = new Color(18f / 255f, 22f / 255f, 28f / 255f, 0.95f);
    private static readonly Color InnerLine = new Color(220f / 255f, 215f / 255f, 232f / 255f, 0.16f);
    private static readonly Color HintColor = new Color(236f / 255f, 233f / 255f, 228f / 255f, 0.5f);
    private static readonly Color DefaultAccent = new Color(229f / 255f, 222f / 255f, 243f / 255f, 1f);

    private static DialogueBox instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        GameObject go = new GameObject("[DialogueBox]");
        instance = go.AddComponent<DialogueBox>();
        DontDestroyOnLoad(go);
    }

    // ── 밖에서 쓰는 것 ───────────────────────

    /// <summary>대화가 떠 있는지.</summary>
    public static bool IsOpen => instance != null && instance.current != null;

    /// <summary>떠 있거나, 뜨려고 기다리는 대화가 있는지. 보스방처럼 대화가 끝날 때까지 기다릴 때 쓴다.</summary>
    public static bool IsBusy => instance != null && (instance.current != null || instance.queue.Count > 0);

    /// <summary>이번 프레임 키 입력을 대화창이 가져갔는지. 닫히는 프레임도 포함한다.</summary>
    public static bool BlocksInput => IsOpen || (instance != null && instance.closedFrame == Time.frameCount);

    /// <summary>대화를 띄운다. 이미 떠 있으면 줄 서서 다음에 뜬다.</summary>
    /// <param name="onFinished">마지막 줄을 넘기거나 건너뛰면 불린다.</param>
    /// <param name="delaySeconds">이만큼(실제 시간) 기다렸다가 뜬다.</param>
    public static void Play(DialogueData dialogue, Action onFinished = null, float delaySeconds = 0f)
    {
        if (dialogue == null || dialogue.IsEmpty)
        {
            onFinished?.Invoke();
            return;
        }

        if (instance == null) Bootstrap();
        instance.queue.Enqueue(new Request
        {
            Data = dialogue,
            OnFinished = onFinished,
            StartAt = Time.unscaledTime + Mathf.Max(0f, delaySeconds)
        });
    }

    // ── 내부 ────────────────────────────────

    private class Request
    {
        public DialogueData Data;
        public Action OnFinished;
        public float StartAt;
    }

    private readonly Queue<Request> queue = new Queue<Request>();
    private Request current;
    private int lineIndex;
    private float openedAt;
    private float lineStartedAt;
    private int closedFrame = -1;
    private int charCount;
    private bool lineDone;
    private float savedTimeScale = 1f;
    private bool holdingTime;

    // 화면
    private GameObject root;
    private CanvasGroup group;
    private TMP_FontAsset font;

    private GameObject boxRoot;
    private Image frameImage;
    private RectTransform plate;
    private Image plateImage;
    private TMP_Text plateText;
    private GameObject portraitRoot;
    private Image portraitRing;
    private Image portraitFill;
    private Image portraitImage;
    private TMP_Text boxText;
    private TMP_Text nextMark;

    private GameObject narrationRoot;
    private TMP_Text narrationPrev;
    private TMP_Text narrationText;
    private TMP_Text narrationNext;

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
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (current == null)
        {
            TryOpenNext();
            return;
        }

        // 다른 곳에서 시간을 되돌려도 떠 있는 동안은 멈춰 둔다.
        if (Time.timeScale != 0f) Time.timeScale = 0f;

        float now = Time.unscaledTime;
        if (group != null) group.alpha = Mathf.Clamp01((now - openedAt) / FadeSeconds);

        Animate(now);

        if (now - openedAt < InputGuardSeconds) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
            kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    private void TryOpenNext()
    {
        if (queue.Count == 0) return;

        // 획득 알림창·유물 선택창이 먼저다.
        if (AcquirePopup.IsOpen || RelicSelectionPanelUI.IsOpen) return;
        if (queue.Peek().StartAt > Time.unscaledTime) return;

        Open(queue.Dequeue());
    }

    private void Open(Request request)
    {
        EnsureView();

        if (!holdingTime)
        {
            savedTimeScale = HitStop.IsActive ? HitStop.RestoreScale : (Time.timeScale > 0f ? Time.timeScale : 1f);
            holdingTime = true;
        }

        current = request;
        openedAt = Time.unscaledTime;
        Time.timeScale = 0f;

        bool narration = request.Data.style == DialogueData.Style.Narration;
        boxRoot.SetActive(!narration);
        narrationRoot.SetActive(narration);
        if (!narration) PaintTint(request.Data.tint);

        narrationPrev.text = "";
        lineIndex = 0;
        ShowLine();

        if (group != null) group.alpha = 0f;
        root.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.RefreshInputBlock();
    }

    private void ShowLine()
    {
        DialogueData.Line line = current.Data.lines[lineIndex] ?? new DialogueData.Line();
        string text = line.text ?? "";
        lineStartedAt = Time.unscaledTime;
        lineDone = false;

        if (current.Data.style == DialogueData.Style.Narration)
        {
            if (lineIndex > 0) narrationPrev.text = current.Data.lines[lineIndex - 1]?.text ?? "";
            narrationText.text = text;
            narrationText.alpha = 0f;
            return;
        }

        // 이름표
        bool hasSpeaker = !string.IsNullOrEmpty(line.speaker);
        plate.gameObject.SetActive(hasSpeaker);
        if (hasSpeaker)
        {
            string accentHex = ColorUtility.ToHtmlStringRGB(AccentOf(current.Data.tint));
            plateText.text = $"<size=60%><color=#{accentHex}>◆</color></size>  {line.speaker}";
            float width = plateText.GetPreferredValues(plateText.text).x + 66f;
            plate.sizeDelta = new Vector2(width, plate.sizeDelta.y);
        }

        // 초상화
        bool hasPortrait = line.portrait != null;
        portraitRoot.SetActive(hasPortrait);
        if (hasPortrait) portraitImage.sprite = line.portrait;

        RectTransform tr = boxText.rectTransform;
        tr.offsetMin = new Vector2(hasPortrait ? TextLeftWithPortrait : TextLeftNoPortrait, tr.offsetMin.y);

        // 한 글자씩
        boxText.text = text;
        boxText.ForceMeshUpdate();
        charCount = boxText.textInfo.characterCount;
        boxText.maxVisibleCharacters = 0;
        nextMark.gameObject.SetActive(false);
    }

    private void Animate(float now)
    {
        float shown = now - lineStartedAt;

        if (current.Data.style == DialogueData.Style.Narration)
        {
            float k = Mathf.Clamp01(shown / NarrationFadeSeconds);
            narrationText.alpha = k;
            if (k >= 1f) lineDone = true;
            narrationNext.alpha = lineDone ? 0.5f + 0.5f * Mathf.Sin(now * 5f) * 0.5f : 0f;
            return;
        }

        if (!lineDone)
        {
            int visible = Mathf.FloorToInt(shown * CharsPerSecond);
            boxText.maxVisibleCharacters = visible;
            if (visible >= charCount) FinishLine();
        }

        if (lineDone)
        {
            // ▼가 살짝 위아래로 움직이며 깜빡인다.
            float bob = 0.5f + 0.5f * Mathf.Sin(now * 6f);
            nextMark.alpha = Mathf.Lerp(0.55f, 1f, bob);
            nextMark.rectTransform.anchoredPosition = new Vector2(-44f, 34f - 4f * bob);
        }
    }

    private void FinishLine()
    {
        lineDone = true;

        if (current.Data.style == DialogueData.Style.Narration)
        {
            narrationText.alpha = 1f;
            return;
        }

        boxText.maxVisibleCharacters = int.MaxValue;
        nextMark.gameObject.SetActive(true);
    }

    private void Advance()
    {
        if (current == null) return;

        if (!lineDone)
        {
            FinishLine();
            return;
        }

        lineIndex++;
        if (lineIndex < current.Data.lines.Length) ShowLine();
        else Close();
    }

    private void Close()
    {
        if (current == null) return;

        Action done = current.OnFinished;
        current = null;
        closedFrame = Time.frameCount;

        // 바로 이어서 뜰 대화가 있으면 시간은 멈춘 채로 넘긴다.
        if (queue.Count > 0 && queue.Peek().StartAt <= Time.unscaledTime)
        {
            done?.Invoke();
            Open(queue.Dequeue());
            return;
        }

        root.SetActive(false);
        holdingTime = false;
        if (Time.timeScale == 0f && !AcquirePopup.IsOpen && !RelicSelectionPanelUI.IsOpen)
            Time.timeScale = savedTimeScale;

        if (UIManager.Instance != null) UIManager.Instance.RefreshInputBlock();

        done?.Invoke();
    }

    // ── 색 ──────────────────────────────────

    private static RelicCategory? CategoryOf(DialogueData.Tint tint)
    {
        return tint switch
        {
            DialogueData.Tint.Sword => RelicCategory.Sword,
            DialogueData.Tint.Boss => RelicCategory.Sword,
            DialogueData.Tint.Orb => RelicCategory.Orb,
            DialogueData.Tint.Body => RelicCategory.Body,
            _ => (RelicCategory?)null
        };
    }

    private static Color AccentOf(DialogueData.Tint tint)
    {
        RelicCategory? c = CategoryOf(tint);
        return c.HasValue ? PearlStyle.CategoryColor(c.Value) : DefaultAccent;
    }

    private void PaintTint(DialogueData.Tint tint)
    {
        RelicCategory? c = CategoryOf(tint);
        float boxWidth = 1920f - BoxSide * 2f;

        frameImage.sprite = PearlStyle.Pearl(c, boxWidth / BoxHeight);
        plateImage.sprite = PearlStyle.Pearl(c, 4f);
        portraitRing.sprite = PearlStyle.PearlCircle();

        portraitFill.color = tint switch
        {
            DialogueData.Tint.Sword => new Color(0.165f, 0.10f, 0.11f, 1f),
            DialogueData.Tint.Boss => new Color(0.165f, 0.10f, 0.11f, 1f),
            DialogueData.Tint.Orb => new Color(0.12f, 0.105f, 0.19f, 1f),
            _ => new Color(0.11f, 0.15f, 0.20f, 1f)
        };

        nextMark.color = AccentOf(tint);
    }

    // ── 화면 만들기 ──────────────────────────

    private void EnsureView()
    {
        if (root != null) return;

        font = TMP_Settings.defaultFontAsset;

        root = new GameObject("DialogueBox_Canvas", typeof(RectTransform), typeof(Canvas),
                              typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 505;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = root.GetComponent<CanvasGroup>();

        BuildBox();
        BuildNarration();

        root.SetActive(false);
    }

    private void BuildBox()
    {
        boxRoot = NewRect("Box", root.transform).gameObject;
        Stretch((RectTransform)boxRoot.transform);

        // 아래쪽만 어둡게
        Image fade = NewImage("BottomFade", boxRoot.transform, new Color(0.024f, 0.035f, 0.047f, 0.78f));
        fade.sprite = PearlStyle.VerticalFade();
        RectTransform fr = fade.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(1f, 0f);
        fr.pivot = new Vector2(0.5f, 0f);
        fr.anchoredPosition = Vector2.zero;
        fr.sizeDelta = new Vector2(0f, 450f);

        // 자개 테두리 → 옻칠 판 → 얇은 안쪽 선
        frameImage = NewImage("Frame", boxRoot.transform, Color.white);
        frameImage.raycastTarget = true;
        RectTransform frame = frameImage.rectTransform;
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(0.5f, 0f);
        frame.offsetMin = new Vector2(BoxSide, BoxBottom);
        frame.offsetMax = new Vector2(-BoxSide, BoxBottom + BoxHeight);

        Button click = frameImage.gameObject.AddComponent<Button>();
        click.transition = Selectable.Transition.None;
        click.navigation = new Navigation { mode = Navigation.Mode.None };
        frameImage.gameObject.AddComponent<NoSelectionBrackets>();
        click.onClick.AddListener(() =>
        {
            if (current != null && Time.unscaledTime - openedAt >= InputGuardSeconds) Advance();
        });

        Image panel = NewImage("Panel", frame, PanelColor);
        Stretch(panel.rectTransform, 3f);

        Image line = NewImage("InnerLine", panel.rectTransform, InnerLine);
        line.sprite = PearlStyle.Outline();
        line.type = Image.Type.Sliced;
        line.fillCenter = false;
        Stretch(line.rectTransform, 9f);

        // 초상화
        portraitRing = NewImage("Portrait", frame, Color.white);
        portraitRoot = portraitRing.gameObject;
        RectTransform pr = portraitRing.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0f, 1f);
        pr.anchoredPosition = new Vector2(42f, -32f);
        pr.sizeDelta = new Vector2(PortraitSize, PortraitSize);

        portraitFill = NewImage("Fill", pr, Color.black);
        portraitFill.sprite = PearlStyle.Circle();
        Stretch(portraitFill.rectTransform, 4f);
        portraitFill.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        portraitImage = NewImage("Image", portraitFill.rectTransform, Color.white);
        portraitImage.preserveAspect = true;
        Stretch(portraitImage.rectTransform, 8f);

        // 대사
        boxText = NewText("Text", frame, 31f, PearlStyle.TextTitle);
        boxText.textWrappingMode = TextWrappingModes.Normal;
        boxText.alignment = TextAlignmentOptions.TopLeft;
        boxText.lineSpacing = 18f;
        RectTransform tr = boxText.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(TextLeftWithPortrait, 40f);
        tr.offsetMax = new Vector2(-90f, -44f);

        nextMark = NewText("Next", frame, 20f, DefaultAccent);
        nextMark.text = "▼";
        nextMark.alignment = TextAlignmentOptions.Center;
        RectTransform nr = nextMark.rectTransform;
        nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(1f, 0f);
        nr.anchoredPosition = new Vector2(-44f, 34f);
        nr.sizeDelta = new Vector2(30f, 30f);

        TMP_Text hint = NewText("Hint", frame, 19f, HintColor);
        hint.text = "Z  다음";
        hint.characterSpacing = 6f;
        hint.alignment = TextAlignmentOptions.Right;
        RectTransform hr = hint.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-84f, 32f);
        hr.sizeDelta = new Vector2(160f, 30f);

        // 이름표: 창 위쪽 가장자리에 걸친다.
        plateImage = NewImage("NamePlate", frame, Color.white);
        plate = plateImage.rectTransform;
        plate.anchorMin = plate.anchorMax = new Vector2(0f, 1f);
        plate.pivot = new Vector2(0f, 0.5f);
        plate.anchoredPosition = new Vector2(60f, 0f);
        plate.sizeDelta = new Vector2(240f, 60f);

        Image plateInner = NewImage("Inner", plate, new Color(18f / 255f, 22f / 255f, 28f / 255f, 1f));
        Stretch(plateInner.rectTransform, 3f);

        plateText = NewText("Name", plate, 27f, PearlStyle.TextTitle);
        plateText.fontStyle = FontStyles.Bold;
        plateText.characterSpacing = 10f;
        plateText.alignment = TextAlignmentOptions.Center;
        Stretch(plateText.rectTransform);
    }

    private void BuildNarration()
    {
        narrationRoot = NewRect("Narration", root.transform).gameObject;
        Stretch((RectTransform)narrationRoot.transform);

        Image bg = NewImage("Dark", narrationRoot.transform, new Color(0.027f, 0.035f, 0.047f, 0.9f));
        bg.raycastTarget = true;
        Stretch(bg.rectTransform);

        Button click = bg.gameObject.AddComponent<Button>();
        click.transition = Selectable.Transition.None;
        click.navigation = new Navigation { mode = Navigation.Mode.None };
        bg.gameObject.AddComponent<NoSelectionBrackets>();
        click.onClick.AddListener(() =>
        {
            if (current != null && Time.unscaledTime - openedAt >= InputGuardSeconds) Advance();
        });

        narrationPrev = NewText("Previous", narrationRoot.transform, 39f, PearlStyle.TextTitle);
        narrationText = NewText("Current", narrationRoot.transform, 39f, PearlStyle.TextTitle);

        foreach (TMP_Text t in new[] { narrationPrev, narrationText })
        {
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.characterSpacing = 6f;
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(1400f, 120f);
        }

        narrationPrev.alpha = 0.4f;
        narrationPrev.rectTransform.anchoredPosition = new Vector2(0f, 70f);
        narrationText.rectTransform.anchoredPosition = new Vector2(0f, -20f);

        narrationNext = NewText("Hint", narrationRoot.transform, 19f, HintColor);
        narrationNext.text = "Z  다음   ·   Esc  건너뛰기";
        narrationNext.characterSpacing = 6f;
        narrationNext.alignment = TextAlignmentOptions.Right;
        RectTransform hr = narrationNext.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 0f);
        hr.anchoredPosition = new Vector2(-48f, 40f);
        hr.sizeDelta = new Vector2(500f, 30f);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        RectTransform r = NewRect(name, parent);
        Image image = r.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text NewText(string name, Transform parent, float size, Color color)
    {
        RectTransform r = NewRect(name, parent);
        TextMeshProUGUI text = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.richText = true;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // ── 에디터 테스트 ────────────────────────

#if UNITY_EDITOR
    [MenuItem("Tools/UI/대화창 테스트")]
    private static void TestBox()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[DialogueBox] 플레이 중에만 띄울 수 있어요.");
            return;
        }

        DialogueData d = ScriptableObject.CreateInstance<DialogueData>();
        d.style = DialogueData.Style.Box;
        d.tint = DialogueData.Tint.Orb;
        d.lines = new[]
        {
            new DialogueData.Line { speaker = "시험용 화자", text = "여기에 대사가 들어가요.\n글자는 한 글자씩 나오고, 다 나오면 오른쪽 아래 ▼가 깜빡여요." },
            new DialogueData.Line { speaker = "시험용 화자", text = "글자가 나오는 중에 Z를 누르면 한 줄이 바로 다 보이고, 한 번 더 누르면 다음 줄로 넘어가요." },
            new DialogueData.Line { speaker = "", text = "이름을 비우면 이름표가 숨고, 초상화를 비우면 대사가 왼쪽으로 붙어요." }
        };
        Play(d);
    }

    [MenuItem("Tools/UI/내레이션 테스트")]
    private static void TestNarration()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[DialogueBox] 플레이 중에만 띄울 수 있어요.");
            return;
        }

        DialogueData d = ScriptableObject.CreateInstance<DialogueData>();
        d.style = DialogueData.Style.Narration;
        d.lines = new[]
        {
            new DialogueData.Line { text = "시험용 내레이션 첫 번째 문장" },
            new DialogueData.Line { text = "두 번째 문장은 천천히 떠오르고, 앞 문장은 위에 흐리게 남아요." },
            new DialogueData.Line { text = "Esc를 누르면 건너뛸 수 있어요." }
        };
        Play(d);
    }
#endif
}
