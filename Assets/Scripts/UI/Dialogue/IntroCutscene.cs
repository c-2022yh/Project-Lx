using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 게임 시작 연출 (10.30 시안 10번). 새 게임은 Intro 씬에서 시작하고, 연출이 끝나면 Tutorial로 넘어간다.
///
/// Intro 씬 (그림 동굴, 화면 전체가 UI로 그려진다):
///   1. 내레이션: 까만 화면에 글이 한 줄씩 떠올랐다 사라진다.
///   2. 깨어남: 위아래 검은 띠가 내려오고 동굴이 밝아진다. 무릎 꿇은 캐릭터가 "…차갑다." 뒤에 일어나 걷는다.
///   3. 보주의 목소리: 동굴 안쪽에서 빛나던 보주가 다가와 대화창으로 말을 건다.
///   4. 보주 획득: 획득 알림 → 빛이 터지며 보주가 작아져 캐릭터 곁에 붙는다.
///   5. 동굴 밖으로: 오른쪽 벽이 열리며 빛이 들고, 캐릭터가 걸어 나간다 → Tutorial 씬.
/// Tutorial 씬에 처음 도착하면: 플레이어에게 실제 보주를 주고(시작 지점 옆 줍는 보주는 치운다), 지역 이름 카드를 띄운다.
///
/// 글·대사·이름은 Resources/StoryDialogues 에셋의 "시작 연출" 칸에서 바꾼다.
/// 그림은 Resources/Story 폴더 (동굴 층 그림, 캐릭터 대기·무릎·걷기, 보주).
/// Z·Enter·클릭: 다음 / Esc: 연출 건너뛰기(바로 Tutorial로).
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    public const string IntroSceneName = "Intro";
    public const string NextSceneName = "Tutorial";
    public const string Flag = "story.intro";             // 연출을 다 봤다 (또는 건너뛰었다)
    private const string ArrivedFlag = "story.introArrived"; // Tutorial에서 보주를 받았다

    private const string Folder = "Story/";

    // ── 시간 (초) ──
    private const float LineFadeIn = 1.0f, LineHold = 2.4f, LineFadeOut = 0.7f;
    private const float RevealTime = 2.4f, BarsTime = 0.8f;
    private const float BarHeight = 96f;   // 1920×1080 기준
    private const float FloorY = -300f;    // 화면 가운데 기준 바닥 높이 (시안 560/720)
    private const float ActorHeight = 270f;

    public static bool IsPlaying { get; private set; }

    private Canvas canvas;
    private RectTransform stage;
    private Image curtain, flash, barTop, barBottom, wall, exitLight, glow, burst;
    private CanvasGroup caveGroup;
    private TextMeshProUGUI lineText, skipHint;
    private TMP_FontAsset font;

    private RectTransform actor;
    private Image kneelImg, idleImg, walkA, walkB;
    private bool walking;

    private RectTransform orb;
    private Image orbImg;
    private float orbBobBase;

    private bool skip;
    private bool leaving;

    // ── 시작 ───────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsPlaying = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Begin(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Begin(scene);
    }

    private static void Begin(Scene scene)
    {
        if (scene.name == IntroSceneName)
        {
            if (FindAnyObjectByType<IntroCutscene>() == null)
                new GameObject("[IntroCutscene]").AddComponent<IntroCutscene>();
        }
        else if (scene.name == NextSceneName && RunState.Current.HasFlag(Flag) && !RunState.Current.HasFlag(ArrivedFlag))
        {
            new GameObject("[IntroArrival]").AddComponent<IntroArrival>();
        }
    }

    private void Awake()
    {
        BuildStage();
    }

    private void Start()
    {
        // 이미 본 세이브로 이어하기를 했으면 바로 다음 씬으로
        if (RunState.Current.HasFlag(Flag) || IsDisabledInEditor)
        {
            RunState.Current.SetFlag(Flag);
            Leave();
            return;
        }

        IsPlaying = true;
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        IsPlaying = false;
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;
        if (skipHint.enabled && k != null && k.escapeKey.wasPressedThisFrame) skip = true;

        // 걷는 동안 두 장을 번갈아 보여 준다
        if (walking)
        {
            bool a = Mathf.Repeat(Time.unscaledTime, 0.46f) < 0.23f;
            walkA.enabled = a;
            walkB.enabled = !a;
        }

        // 보주는 늘 살짝 둥실거린다
        if (orbImg != null)
            orbImg.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(Time.unscaledTime * 2.0f) * 12f * orbBobBase);
    }

    // ── 진행 ───────────────────────────────

    private IEnumerator Run()
    {
        // 1) 내레이션
        DialogueData narration = StoryDialogues.IntroNarration;
        if (narration != null && narration.lines != null)
            foreach (DialogueData.Line line in narration.lines)
            {
                if (skip) break;
                yield return ShowLine(line != null ? line.text : "", 0f);
            }
        if (skip) { Leave(); yield break; }

        // 2) 깨어남
        caveGroup.alpha = 1f;
        SetActor(kneel: true);
        PlaceActor(450f);
        SetOrb(1320f, -90f, 105f, 0.35f);
        SetGlow(1320f, -90f, 0.22f);
        StartCoroutine(Bars(true));
        yield return Fade(curtain, 1f, 0f, RevealTime);
        if (skip) { Leave(); yield break; }

        // 캐릭터의 첫마디 ("…차갑다."). 대화창으로 띄운다. 내레이션 모양으로 바꾸면 화면 가운데 글로 나온다.
        DialogueData wake = StoryDialogues.IntroWake;
        if (wake != null && !wake.IsEmpty)
        {
            if (wake.style == DialogueData.Style.Box)
            {
                skipHint.enabled = false;
                DialogueBox.Play(wake);
                yield return null;
                while (DialogueBox.IsBusy) yield return null;
                skipHint.enabled = true;
            }
            else yield return ShowLine(wake.lines[0].text, 165f);
        }
        yield return Wait(0.3f);
        if (skip) { Leave(); yield break; }

        // 일어나 보주 쪽으로 걷는다. 보주가 조금 밝아진다.
        SetActor(kneel: false);
        yield return Wait(0.35f);
        StartCoroutine(MoveOrb(1320f, -90f, 135f, 0.7f, 1.2f));
        StartCoroutine(GlowTo(0.35f, 1.2f));
        yield return Walk(450f, 915f, 2.6f);
        if (skip) { Leave(); yield break; }

        // 3) 보주가 다가와 말을 건다
        StartCoroutine(GlowFollow(1200f, -60f, 0.55f, 1.1f));
        yield return MoveOrb(1200f, -60f, 165f, 1f, 1.1f);
        skipHint.enabled = false;   // 여기부터는 대화창·획득 알림이 키를 받는다

        DialogueData talk = StoryDialogues.IntroOrb;
        if (talk != null && !talk.IsEmpty)
        {
            DialogueBox.Play(talk);
            yield return null;
            while (DialogueBox.IsBusy) yield return null;
        }

        // 4) 획득
        AcquirePopup.Show(orbImg.sprite, StoryDialogues.IntroOrbName, StoryDialogues.IntroOrbDescription);
        float waitOpen = 0f;
        while (!AcquirePopup.IsOpen && waitOpen < 1f) { waitOpen += Time.unscaledDeltaTime; yield return null; }
        while (AcquirePopup.IsOpen) yield return null;

        burst.rectTransform.anchoredPosition = ScreenPos(960f, -90f);
        StartCoroutine(Burst());
        StartCoroutine(GlowFollow(840f, 0f, 0.4f, 1.0f));
        yield return MoveOrb(840f, 0f, 69f, 1f, 1.0f);
        yield return new WaitForSecondsRealtime(0.4f);

        // 5) 동굴 밖으로
        StartCoroutine(Fade(wall, 1f, 0f, 1.5f));
        StartCoroutine(Fade(exitLight, 0f, 1f, 1.8f));
        yield return new WaitForSecondsRealtime(0.8f);
        StartCoroutine(MoveOrb(1650f, 0f, 69f, 1f, 3.0f));
        StartCoroutine(GlowFollow(1650f, 0f, 0.4f, 3.0f));
        yield return Walk(915f, 1725f, 3.0f);

        Leave();
    }

    /// <summary>연출을 끝내고 Tutorial로. 건너뛰기도 여기로 온다.</summary>
    private void Leave()
    {
        if (leaving) return;
        leaving = true;

        RunState.Current.SetFlag(Flag);
        IsPlaying = false;
        SceneFader.LoadScene(NextSceneName);
    }

    // ── 장면 조각 ───────────────────────────

    private IEnumerator ShowLine(string text, float y)
    {
        lineText.text = text;
        lineText.rectTransform.anchoredPosition = new Vector2(0f, y);

        yield return FadeText(0f, 1f, LineFadeIn);
        for (float t = 0f; t < LineHold && !skip; t += Time.unscaledDeltaTime)
        {
            if (t > 0.3f && AdvancePressed()) break;
            yield return null;
        }
        yield return FadeText(lineText.alpha, 0f, skip ? 0.15f : LineFadeOut);
    }

    private IEnumerator FadeText(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            if (skip && to > from) break;
            lineText.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        lineText.alpha = skip && to > from ? 0f : to;
    }

    private IEnumerator Fade(Graphic g, float from, float to, float time)
    {
        g.enabled = true;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            SetAlpha(g, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetAlpha(g, to);
        if (to <= 0f) g.enabled = false;
    }

    private IEnumerator Bars(bool show)
    {
        float from = show ? 0f : BarHeight, to = show ? BarHeight : 0f;
        for (float t = 0f; t < BarsTime; t += Time.unscaledDeltaTime)
        {
            float h = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / BarsTime));
            barTop.rectTransform.sizeDelta = barBottom.rectTransform.sizeDelta = new Vector2(0f, h);
            yield return null;
        }
        barTop.rectTransform.sizeDelta = barBottom.rectTransform.sizeDelta = new Vector2(0f, to);
    }

    private IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime) yield return null;
    }

    private IEnumerator Walk(float fromX, float toX, float time)
    {
        walking = true;
        idleImg.enabled = false;
        for (float t = 0f; t < time && !skip; t += Time.unscaledDeltaTime)
        {
            PlaceActor(Mathf.Lerp(fromX, toX, t / time));
            yield return null;
        }
        PlaceActor(toX);
        walking = false;
        walkA.enabled = walkB.enabled = false;
        idleImg.enabled = true;
    }

    private IEnumerator MoveOrb(float x, float y, float size, float alpha, float time)
    {
        Vector2 fromPos = orb.anchoredPosition, toPos = ScreenPos(x, y);
        float fromSize = orb.sizeDelta.x, fromA = orbImg.color.a;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / time);
            orb.anchoredPosition = Vector2.Lerp(fromPos, toPos, u);
            float s = Mathf.Lerp(fromSize, size, u);
            orb.sizeDelta = new Vector2(s, s);
            SetAlpha(orbImg, Mathf.Lerp(fromA, alpha, u));
            orbBobBase = s / 165f;
            yield return null;
        }
        SetOrb(x, y, size, alpha);
    }

    private IEnumerator GlowTo(float alpha, float time)
    {
        float from = glow.color.a;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            SetAlpha(glow, Mathf.Lerp(from, alpha, t / time));
            yield return null;
        }
        SetAlpha(glow, alpha);
    }

    private IEnumerator GlowFollow(float x, float y, float alpha, float time)
    {
        Vector2 from = glow.rectTransform.anchoredPosition, to = ScreenPos(x, y);
        float fromA = glow.color.a;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / time);
            glow.rectTransform.anchoredPosition = Vector2.Lerp(from, to, u);
            SetAlpha(glow, Mathf.Lerp(fromA, alpha, u));
            yield return null;
        }
    }

    private IEnumerator Burst()
    {
        burst.enabled = true;
        for (float t = 0f; t < 1.1f; t += Time.unscaledDeltaTime)
        {
            float u = t / 1.1f;
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.6f, 1f - (1f - u) * (1f - u));
            SetAlpha(burst, u < 0.25f ? u / 0.25f * 0.95f : 0.95f * (1f - (u - 0.25f) / 0.75f));
            yield return null;
        }
        burst.enabled = false;
    }

    // ── 배치 도우미 (시안 좌표 × 1.5 = 화면 좌표, 왼쪽 위 기준) ──

    /// <summary>x: 화면 왼쪽에서, y: 화면 가운데에서 아래로 내려간 만큼(+)이 아니라 위(+)로.</summary>
    private static Vector2 ScreenPos(float x, float y) => new Vector2(x - 960f, y);

    private void PlaceActor(float x) => actor.anchoredPosition = new Vector2(x - 960f, FloorY);

    private void SetActor(bool kneel)
    {
        kneelImg.enabled = kneel;
        idleImg.enabled = !kneel;
        walkA.enabled = walkB.enabled = false;
    }

    private void SetOrb(float x, float y, float size, float alpha)
    {
        orb.anchoredPosition = ScreenPos(x, y);
        orb.sizeDelta = new Vector2(size, size);
        SetAlpha(orbImg, alpha);
        orbBobBase = size / 165f;
    }

    private void SetGlow(float x, float y, float alpha)
    {
        glow.rectTransform.anchoredPosition = ScreenPos(x, y);
        SetAlpha(glow, alpha);
    }

    // ── 화면 만들기 ──────────────────────────

    private void BuildStage()
    {
        font = TMP_Settings.defaultFontAsset;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;   // 획득 알림(500)·대화창(505) 아래
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 16:9가 아니어도 그림이 다 보이게

        Image back = NewImage("Back", transform, Color.black, null);
        Stretch(back.rectTransform, 400f);

        // 1920×1080 무대. 화면 비율이 달라도 가운데에 이 크기로 놓인다.
        stage = NewRect("Stage", transform);
        stage.sizeDelta = new Vector2(1920f, 1080f);

        GameObject caveGo = NewRect("Cave", stage).gameObject;
        caveGroup = caveGo.AddComponent<CanvasGroup>();
        caveGroup.alpha = 0f;
        RectTransform cave = (RectTransform)caveGo.transform;
        Stretch(cave, 0f);

        Stretch(NewImage("Far", cave, Color.white, Load("Intro_Cave_far")).rectTransform, 0f);

        glow = NewImage("Glow", cave, new Color(0.47f, 0.71f, 1f, 0f), Load("Intro_Cave_glow"));
        glow.rectTransform.sizeDelta = new Vector2(780f, 780f);

        exitLight = NewImage("ExitLight", cave, new Color(1f, 1f, 1f, 0f), Load("Intro_Cave_exit"));
        Stretch(exitLight.rectTransform, 0f);
        exitLight.enabled = false;

        Stretch(NewImage("Near", cave, Color.white, Load("Intro_Cave_near")).rectTransform, 0f);
        wall = NewImage("RightWall", cave, Color.white, Load("Intro_Cave_wall"));
        Stretch(wall.rectTransform, 0f);
        Stretch(NewImage("Vignette", cave, Color.white, Load("Intro_Cave_vignette")).rectTransform, 0f);

        // 보주
        orb = NewRect("Orb", cave);
        orbImg = NewImage("Sprite", orb, new Color(1f, 1f, 1f, 0f), Load("Intro_Orb"));
        orbImg.preserveAspect = true;
        Stretch(orbImg.rectTransform, 0f);

        burst = NewImage("Burst", cave, new Color(0.88f, 0.94f, 1f, 0f), Load("Intro_Cave_glow"));
        burst.rectTransform.sizeDelta = new Vector2(360f, 360f);
        burst.enabled = false;

        // 캐릭터: 발이 바닥에 닿게 아래 가운데를 기준으로
        actor = NewRect("Actor", cave);
        actor.sizeDelta = new Vector2(300f, ActorHeight);
        actor.pivot = new Vector2(0.5f, 0f);
        kneelImg = ActorPart("Kneel", "Intro_Kneel", -15f);
        idleImg = ActorPart("Idle", "Intro_Idle", 0f);
        walkA = ActorPart("WalkA", "Intro_Walk1", 0f);
        walkB = ActorPart("WalkB", "Intro_Walk2", 0f);
        SetActor(kneel: true);

        flash = NewImage("Flash", stage, new Color(0.85f, 0.93f, 1f, 0f), null);
        Stretch(flash.rectTransform, 400f);
        flash.enabled = false;

        barTop = NewImage("BarTop", transform, Color.black, null);
        Edge(barTop.rectTransform, top: true);
        barBottom = NewImage("BarBottom", transform, Color.black, null);
        Edge(barBottom.rectTransform, top: false);

        curtain = NewImage("Curtain", transform, Color.black, null);
        Stretch(curtain.rectTransform, 400f);

        lineText = NewText("Line", stage, "", 40f, new Color(0.91f, 0.92f, 0.94f, 1f));
        lineText.rectTransform.sizeDelta = new Vector2(1700f, 120f);
        lineText.characterSpacing = 6f;
        lineText.alpha = 0f;

        skipHint = NewText("SkipHint", transform, "Z 다음   ·   Esc 건너뛰기", 18f, new Color(0.93f, 0.91f, 0.89f, 0.35f));
        RectTransform hr = skipHint.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 1f);
        hr.sizeDelta = new Vector2(500f, 40f);
        hr.anchoredPosition = new Vector2(-40f, -30f);
        skipHint.alignment = TextAlignmentOptions.TopRight;
    }

    /// <summary>캐릭터 그림 한 장. 모든 그림을 같은 배율로 줄여 발끝을 바닥에 맞춘다.</summary>
    private Image ActorPart(string name, string sprite, float offsetX)
    {
        Sprite s = Load(sprite);
        Image img = NewImage(name, actor, Color.white, s);
        RectTransform r = img.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(offsetX, 0f);
        // 서 있는 그림(대기) 높이 368px이 ActorHeight가 되는 배율
        float scale = ActorHeight / 368f;
        r.sizeDelta = s != null ? new Vector2(s.rect.width, s.rect.height) * scale : new Vector2(100f, ActorHeight);
        return img;
    }

    private static Sprite Load(string name)
    {
        Sprite s = Resources.Load<Sprite>(Folder + name);
        if (s == null) Debug.LogWarning("[IntroCutscene] 그림이 없습니다: Resources/" + Folder + name);
        return s;
    }

    private static bool AdvancePressed()
    {
        Keyboard k = Keyboard.current;
        if (k != null && (k.zKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame ||
                          k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) return true;
        Mouse m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        Gamepad g = Gamepad.current;
        return g != null && g.buttonSouth.wasPressedThisFrame;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    private static Image NewImage(string name, Transform parent, Color color, Sprite sprite)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform r, float bleed)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(-bleed, -bleed);
        r.offsetMax = new Vector2(bleed, bleed);
    }

    private static void Edge(RectTransform r, bool top)
    {
        r.anchorMin = new Vector2(0f, top ? 1f : 0f);
        r.anchorMax = new Vector2(1f, top ? 1f : 0f);
        r.pivot = new Vector2(0.5f, top ? 1f : 0f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(0f, 0f);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    // ── 에디터 테스트용 ───────────────────────

#if UNITY_EDITOR
    private const string DisableKey = "Ludens.UI.DisableIntroCutscene";
    private const string DisableMenu = "Tools/UI/시작 연출 끄기 (테스트용)";

    private static bool IsDisabledInEditor => EditorPrefs.GetBool(DisableKey, false);

    [MenuItem(DisableMenu)]
    private static void ToggleDisabled()
    {
        bool next = !IsDisabledInEditor;
        EditorPrefs.SetBool(DisableKey, next);
        Debug.Log(next ? "[IntroCutscene] 시작 연출: 끔 (Intro 씬을 열면 바로 Tutorial로)" : "[IntroCutscene] 시작 연출: 켬");
    }

    [MenuItem(DisableMenu, true)]
    private static bool ToggleDisabledValidate()
    {
        Menu.SetChecked(DisableMenu, IsDisabledInEditor);
        return true;
    }
#else
    private static bool IsDisabledInEditor => false;
#endif

    // ── Tutorial 도착 ─────────────────────────

    /// <summary>
    /// 연출을 보고 Tutorial에 처음 왔을 때: 실제 보주를 주고, 시작 지점 옆 줍는 보주는 치우고, 지역 이름 카드를 띄운다.
    /// </summary>
    private class IntroArrival : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 플레이어가 스폰 위치에 놓이고 세이브가 반영될 때까지
            yield return null;
            yield return null;

            foreach (OrbPickup pickup in FindObjectsByType<OrbPickup>())
            {
                FieldObjectState.MarkDone(pickup);
                Destroy(pickup.gameObject);
            }

            Player player = FindAnyObjectByType<Player>();
            PlayerEnergy energy = player != null ? player.GetComponent<PlayerEnergy>() : null;
            if (energy != null && !energy.HasOrb) energy.AcquireOrb();

            RunState.Current.SetFlag(ArrivedFlag);

            yield return new WaitForSecondsRealtime(0.9f);
            yield return AreaTitle(this, StoryDialogues.IntroAreaName, StoryDialogues.IntroAreaSubtitle);
            Destroy(gameObject);
        }
    }

    /// <summary>할로우나이트식 지역 이름: 위아래 가는 선 사이에 이름이 퍼지듯 나타났다 사라진다.</summary>
    private static IEnumerator AreaTitle(MonoBehaviour host, string title, string subtitle)
    {
        if (string.IsNullOrEmpty(title)) yield break;

        GameObject root = new GameObject("AreaTitle_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(host.transform, false);
        Canvas c = root.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 470;
        CanvasScaler sc = root.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;

        RectTransform group = NewRect("Group", root.transform);
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.62f);
        group.sizeDelta = new Vector2(1200f, 200f);
        CanvasGroup cg = group.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        Color ink = new Color(0.93f, 0.91f, 0.89f, 0.75f);
        Image top = NewImage("LineTop", group, ink, LineSprite());
        Image bottom = NewImage("LineBottom", group, ink, LineSprite());
        top.rectTransform.sizeDelta = bottom.rectTransform.sizeDelta = new Vector2(540f, 1.5f);
        top.rectTransform.anchoredPosition = new Vector2(0f, 46f);
        bottom.rectTransform.anchoredPosition = new Vector2(0f, -46f);

        TMP_FontAsset f = TMP_Settings.defaultFontAsset;
        TextMeshProUGUI name = MakeText(group, title, 64f, new Color(0.95f, 0.94f, 0.91f, 1f), f);
        name.fontStyle = FontStyles.Bold;
        TextMeshProUGUI sub = MakeText(group, subtitle ?? "", 22f, new Color(0.93f, 0.91f, 0.89f, 0.7f), f);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -84f);
        sub.characterSpacing = 20f;

        const float total = 3.6f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float u = t / total;
            cg.alpha = Mathf.Clamp01(u / 0.22f) * Mathf.Clamp01((1f - u) / 0.22f);
            name.characterSpacing = Mathf.Lerp(70f, 36f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.3f)));
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 0.9f));
            top.rectTransform.localScale = bottom.rectTransform.localScale = new Vector3(grow, 1f, 1f);
            yield return null;
        }
        Object.Destroy(root);
    }

    private static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color, TMP_FontAsset f)
    {
        TextMeshProUGUI t = NewRect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (f != null) t.font = f;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.rectTransform.sizeDelta = new Vector2(1200f, 90f);
        return t;
    }

    private static Sprite lineSprite;

    private static Sprite LineSprite()
    {
        if (lineSprite != null) return lineSprite;
        const int w = 128;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < w; x++)
        {
            float u = Mathf.Abs(x / (w - 1f) - 0.5f) * 2f;
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, 1f - u * u));
        }
        tex.Apply();
        lineSprite = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
        return lineSprite;
    }
}
