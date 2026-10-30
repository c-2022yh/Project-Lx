using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 게임 시작 연출 (10.30 시안 10번). Tutorial 씬에 처음 들어오면 한 번만 나온다.
///
///   1. 내레이션: 까만 화면에 글이 한 줄씩 떠올랐다 사라진다.
///   2. 깨어남: 위아래 검은 띠가 내려오고 동굴이 밝아진다. 무릎 꿇고 있던 캐릭터가 "…차갑다." 뒤에 일어선다.
///   3. 보주의 목소리: 씬에 놓인 보주(OrbPickup)가 빛나며 떠올라 캐릭터 앞으로 온다 → 대화창.
///   4. 보주 획득: 보주가 캐릭터에게 스며들고(실제 보주 획득), 획득 알림이 뜬다.
///   5. 검은 띠가 걷히고 조작이 돌아온다. 지역 이름 카드가 잠깐 뜬다.
///
/// 글·대사·이름은 전부 Resources/StoryDialogues 에셋의 "시작 연출" 칸에서 바꾼다.
/// 씬과 플레이어 프리팹은 고치지 않는다. 스스로 생겨서 끝나면 사라진다.
/// 다 보면 RunState에 story.intro 플래그를 남겨 다시 안 나온다 (세이브에 같이 저장).
/// Z·Enter·클릭: 내레이션 다음 줄 / Esc: 내레이션·깨어남 건너뛰기.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    public const string SceneName = "Tutorial";
    public const string Flag = "story.intro";

    private const string KneelSpritePath = "Story/Intro_Kneel";

    // ── 시간 (초) ──
    private const float LineFadeIn = 1.0f, LineHold = 2.4f, LineFadeOut = 0.7f;
    private const float RevealTime = 2.4f, BarsTime = 0.8f;
    private const float BarHeight = 96f;   // 1920×1080 기준

    /// <summary>연출 중이면 true. UIManager가 이걸 보고 조작·창을 막는다.</summary>
    public static bool IsPlaying { get; private set; }

    private static readonly Color Black = new Color(0f, 0f, 0f, 1f);
    private static readonly Color TextColor = new Color(0.91f, 0.92f, 0.94f, 1f);

    private Canvas canvas;
    private Image curtain, flash, barTop, barBottom;
    private TextMeshProUGUI lineText, skipHint;
    private TMP_FontAsset font;

    private Player player;
    private PlayerEnergy energy;
    private OrbPickup pickup;
    private SpriteRenderer kneel;
    private bool playerHidden;

    private bool skip;
    private bool finished;

    // ── 시작 ───────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsPlaying = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryStart(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) TryStart(scene);
    }

    private static void TryStart(Scene scene)
    {
        if (scene.name != SceneName || IsDisabledInEditor) return;
        if (RunState.Current.HasFlag(Flag)) return;
        if (FindAnyObjectByType<IntroCutscene>() != null) return;

        new GameObject("[IntroCutscene]").AddComponent<IntroCutscene>();
    }

    private void Awake()
    {
        // 첫 프레임부터 까맣게 (맵이 한 번 보였다가 가려지지 않게)
        BuildOverlay();
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    // ── 진행 ───────────────────────────────

    private IEnumerator Run()
    {
        // 스폰이 플레이어를 제자리에 놓을 때까지 두 프레임 기다린다. 세이브 불러오기도 이때 끝난다.
        yield return null;
        yield return null;

        player = FindAnyObjectByType<Player>();
        energy = player != null ? player.GetComponent<PlayerEnergy>() : null;

        if (player == null || RunState.Current.HasFlag(Flag) || (energy != null && energy.HasOrb))
        {
            // 볼 필요가 없으면 (이어하기 등) 조용히 빠진다.
            Destroy(gameObject);
            yield break;
        }

        pickup = FindAnyObjectByType<OrbPickup>();
        if (pickup != null)
        {
            // 연출이 대신 줄 것이니, ↑ 안내가 뜨지 않게 줍기는 꺼 둔다.
            foreach (Collider2D c in pickup.GetComponents<Collider2D>()) c.enabled = false;
        }

        IsPlaying = true;
        RefreshInput();
        Stop(player);
        ShowKneel(true);

        // 1) 내레이션
        DialogueData narration = StoryDialogues.IntroNarration;
        if (narration != null && narration.lines != null)
        {
            foreach (DialogueData.Line line in narration.lines)
            {
                if (skip) break;
                yield return ShowLine(line != null ? line.text : "", 0f);
            }
        }

        // 2) 깨어남: 검은 띠 + 화면 밝아짐
        StartCoroutine(Bars(true));
        yield return Fade(curtain, 1f, 0f, skip ? 0.4f : RevealTime);

        if (!skip)
        {
            DialogueData wake = StoryDialogues.IntroWake;
            string wakeText = wake != null && !wake.IsEmpty ? wake.lines[0].text : "";
            if (!string.IsNullOrEmpty(wakeText)) yield return ShowLine(wakeText, 250f);
            yield return Wait(0.3f);
        }

        yield return StandUp(skip ? 0.1f : 0.45f);

        // 3) 보주가 깨어나 다가온다
        if (pickup != null) yield return OrbApproach(skip);

        skipHint.enabled = false;   // 여기부터는 대화창·획득 알림이 키를 받는다

        DialogueData orbTalk = StoryDialogues.IntroOrb;
        if (orbTalk != null && !orbTalk.IsEmpty && !skip)
        {
            DialogueBox.Play(orbTalk);
            yield return null;
            while (DialogueBox.IsBusy) yield return null;
        }

        // 4) 획득
        yield return Absorb();
        Sprite orbIcon = energy != null && energy.CurrentOrb != null && energy.CurrentOrb.SpriteRenderer != null
            ? energy.CurrentOrb.SpriteRenderer.sprite : null;
        AcquirePopup.Show(orbIcon, StoryDialogues.IntroOrbName, StoryDialogues.IntroOrbDescription);

        float waitOpen = 0f;
        while (!AcquirePopup.IsOpen && waitOpen < 1f) { waitOpen += Time.unscaledDeltaTime; yield return null; }
        while (AcquirePopup.IsOpen) yield return null;

        // 5) 조작 돌려주기
        yield return Bars(false);
        Finish();

        yield return AreaTitle(StoryDialogues.IntroAreaName, StoryDialogues.IntroAreaSubtitle);
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // 무릎 꿇은 동안은 애니메이션이 스프라이트를 다시 켜도 계속 숨긴다.
        if (playerHidden && player != null && player.sr != null && player.sr.enabled) player.sr.enabled = false;
        if (player != null && player.rb != null && player.rb.linearVelocity.sqrMagnitude > 0.0001f) Stop(player);

        Keyboard k = Keyboard.current;
        if (skipHint != null && skipHint.enabled && k != null && k.escapeKey.wasPressedThisFrame) skip = true;
    }

    private void OnDestroy()
    {
        Finish();
    }

    /// <summary>연출을 끝내고 원래대로 돌린다. 여러 번 불려도 된다.</summary>
    private void Finish()
    {
        if (finished) return;
        finished = true;

        ShowKneel(false);
        if (curtain != null) curtain.enabled = false;
        if (skipHint != null) skipHint.enabled = false;

        RunState.Current.SetFlag(Flag);
        IsPlaying = false;
        RefreshInput();
    }

    private static void RefreshInput()
    {
        UIManager ui = UIManager.Instance;
        if (ui != null) ui.RefreshInputBlock();
    }

    private static void Stop(Player p)
    {
        if (p != null && p.rb != null) p.rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, p.rb.linearVelocity.y));
        if (p != null) p.moveInput = Vector2.zero;
    }

    // ── 장면 조각 ───────────────────────────

    /// <summary>글 한 줄: 떠오르고, 잠깐 머물고(누르면 바로 넘어감), 사라진다. y는 화면 가운데에서 위로 올리는 정도.</summary>
    private IEnumerator ShowLine(string text, float y)
    {
        lineText.text = text;
        lineText.rectTransform.anchoredPosition = new Vector2(0f, y);

        yield return FadeText(0f, 1f, LineFadeIn);
        float t = 0f;
        while (t < LineHold && !skip)
        {
            if (t > 0.3f && AdvancePressed()) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return FadeText(lineText.alpha, 0f, skip ? 0.15f : LineFadeOut);
    }

    private IEnumerator FadeText(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / time);
            lineText.alpha = Mathf.Lerp(from, to, u);
            // 떠오를 때 아래에서 살짝 올라온다
            if (to > from) lineText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1f, u);
            if (skip && to > from) break;
            yield return null;
        }
        lineText.alpha = to;
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
            barTop.rectTransform.sizeDelta = new Vector2(0f, h);
            barBottom.rectTransform.sizeDelta = new Vector2(0f, h);
            yield return null;
        }
        barTop.rectTransform.sizeDelta = new Vector2(0f, to);
        barBottom.rectTransform.sizeDelta = new Vector2(0f, to);
    }

    private IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime) yield return null;
    }

    /// <summary>무릎 꿇은 그림 → 원래 캐릭터로 서서히 바꾼다.</summary>
    private IEnumerator StandUp(float time)
    {
        if (kneel == null || player == null || player.sr == null)
        {
            ShowKneel(false);
            yield break;
        }

        playerHidden = false;
        player.sr.enabled = true;
        Color pc = player.sr.color;
        Color kc = kneel.color;

        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = t / time;
            player.sr.color = new Color(pc.r, pc.g, pc.b, pc.a * u);
            kneel.color = new Color(kc.r, kc.g, kc.b, kc.a * (1f - u));
            yield return null;
        }

        player.sr.color = pc;
        ShowKneel(false);
    }

    /// <summary>씬의 보주가 맥박치듯 밝아졌다가, 떠올라 캐릭터 앞에 와서 둥실거린다.</summary>
    private IEnumerator OrbApproach(bool quick)
    {
        Transform orb = pickup.transform;
        Vector3 start = orb.position;
        Vector3 baseScale = orb.localScale;
        float dir = player.isFacingRight ? 1f : -1f;
        Vector3 target = player.transform.position + new Vector3(1.1f * dir, 1.2f, 0f);

        if (!quick)
        {
            // 맥박 두 번
            for (float t = 0f; t < 1.4f && !skip; t += Time.unscaledDeltaTime)
            {
                float p = 1f + 0.18f * Mathf.Max(0f, Mathf.Sin(t / 0.7f * Mathf.PI * 2f));
                orb.localScale = baseScale * p;
                yield return null;
            }
        }

        float travel = quick ? 0.2f : 1.6f;
        for (float t = 0f; t < travel; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / travel);
            Vector3 p = Vector3.Lerp(start, target, u);
            p.y += Mathf.Sin(u * Mathf.PI) * 0.6f;   // 살짝 위로 휘어 날아온다
            orb.position = p;
            orb.localScale = baseScale * Mathf.Lerp(1f, 0.9f, u);
            yield return null;
        }
        orb.position = target;
        StartCoroutine(Hover(orb, target));
    }

    private IEnumerator Hover(Transform orb, Vector3 at)
    {
        float t = 0f;
        while (orb != null && !absorbing)
        {
            t += Time.unscaledDeltaTime;
            orb.position = at + Vector3.up * (Mathf.Sin(t * 2.2f) * 0.12f);
            yield return null;
        }
    }

    private bool absorbing;

    /// <summary>보주가 캐릭터에게 스며들고 화면이 번쩍인다. 이때 실제로 보주를 얻는다.</summary>
    private IEnumerator Absorb()
    {
        absorbing = true;

        if (pickup != null)
        {
            Transform orb = pickup.transform;
            Vector3 from = orb.position, scale = orb.localScale;
            Vector3 to = player.transform.position + Vector3.up * 0.9f;
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                orb.position = Vector3.Lerp(from, to, u);
                orb.localScale = scale * (1f - u);
                yield return null;
            }

            FieldObjectState.MarkDone(pickup);
            Destroy(pickup.gameObject);
            pickup = null;
        }

        if (energy != null && !energy.HasOrb) energy.AcquireOrb();

        yield return Fade(flash, 0.55f, 0f, 0.6f);
    }

    /// <summary>할로우나이트식 지역 이름: 위아래 가는 선 사이에 이름이 퍼지듯 나타났다 사라진다.</summary>
    private IEnumerator AreaTitle(string title, string subtitle)
    {
        if (string.IsNullOrEmpty(title)) yield break;

        yield return new WaitForSecondsRealtime(0.6f);

        RectTransform group = NewRect("AreaTitle", canvas.transform);
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.62f);
        group.sizeDelta = new Vector2(1200f, 200f);
        CanvasGroup cg = group.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        Image top = NewImage("LineTop", group, new Color(0.93f, 0.91f, 0.89f, 0.75f));
        Image bottom = NewImage("LineBottom", group, new Color(0.93f, 0.91f, 0.89f, 0.75f));
        foreach (Image line in new[] { top, bottom })
        {
            line.rectTransform.sizeDelta = new Vector2(540f, 1.5f);
            line.sprite = LineSprite();
        }
        top.rectTransform.anchoredPosition = new Vector2(0f, 46f);
        bottom.rectTransform.anchoredPosition = new Vector2(0f, -46f);

        TextMeshProUGUI name = NewText("Name", group, title, 64f, new Color(0.95f, 0.94f, 0.91f, 1f));
        name.fontStyle = FontStyles.Bold;
        TextMeshProUGUI sub = NewText("Sub", group, subtitle ?? "", 22f, new Color(0.93f, 0.91f, 0.89f, 0.7f));
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
        Destroy(group.gameObject);
    }

    // ── 무릎 꿇은 그림 ───────────────────────

    private void ShowKneel(bool show)
    {
        if (show)
        {
            Sprite sprite = Resources.Load<Sprite>(KneelSpritePath);
            if (sprite == null || player == null || player.sr == null) return;

            SpriteRenderer src = player.sr;
            GameObject go = new GameObject("[IntroKneel]");
            go.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            go.transform.localScale = src.transform.lossyScale;
            kneel = go.AddComponent<SpriteRenderer>();
            kneel.sprite = sprite;
            kneel.flipX = src.flipX;
            kneel.sharedMaterial = src.sharedMaterial;
            kneel.sortingLayerID = src.sortingLayerID;
            kneel.sortingOrder = src.sortingOrder;
            kneel.color = src.color;

            src.enabled = false;
            playerHidden = true;
            return;
        }

        playerHidden = false;
        if (player != null && player.sr != null) player.sr.enabled = true;
        if (kneel != null) Destroy(kneel.gameObject);
        kneel = null;
    }

    // ── 화면 ────────────────────────────────

    private void BuildOverlay()
    {
        font = TMP_Settings.defaultFontAsset;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;   // 획득 알림(500)·대화창(505) 아래
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        barTop = NewImage("BarTop", transform, Black);
        Anchor(barTop.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        barBottom = NewImage("BarBottom", transform, Black);
        Anchor(barBottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

        curtain = NewImage("Curtain", transform, Black);
        Stretch(curtain.rectTransform);

        flash = NewImage("Flash", transform, new Color(0.85f, 0.93f, 1f, 0f));
        Stretch(flash.rectTransform);
        flash.enabled = false;

        lineText = NewText("Line", transform, "", 40f, TextColor);
        lineText.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        lineText.characterSpacing = 6f;
        lineText.alpha = 0f;

        skipHint = NewText("SkipHint", transform, "Z 다음   ·   Esc 건너뛰기", 18f, new Color(0.93f, 0.91f, 0.89f, 0.35f));
        RectTransform hr = skipHint.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 1f);
        hr.sizeDelta = new Vector2(500f, 40f);
        hr.anchoredPosition = new Vector2(-40f, -30f);
        skipHint.alignment = TextAlignmentOptions.TopRight;
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

    private static Sprite lineSprite;

    /// <summary>가운데가 진하고 양 끝으로 사라지는 가는 선.</summary>
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

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
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
        t.rectTransform.sizeDelta = new Vector2(1600f, 100f);
        return t;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pivot)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.pivot = pivot;
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
        Debug.Log(next ? "[IntroCutscene] 시작 연출: 끔" : "[IntroCutscene] 시작 연출: 켬");
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
}
