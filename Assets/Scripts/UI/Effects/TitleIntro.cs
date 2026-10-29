using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 연출 (10.29 타이틀 시안 9번). TitleMenuPanel이 시작할 때 스스로 붙인다. 씬은 고치지 않는다.
///
/// 1) 인트로: 까만 화면에 로고가 왼쪽부터 붓으로 쓰듯 나타남 → 뒤 빛이 켜짐 → 까만 막이 걷히며 배경이 보임
/// 2) "아무 키나 누르세요"가 깜빡임 → 아무 키·클릭·패드 버튼을 누르면 메뉴가 하나씩 떠오름
/// 3) 고른 메뉴 왼쪽에 여우불이 일렁이고, 글자 아래에 먹 붓줄이 그어짐 (마우스를 올려도 골라짐)
/// 그 밖에: 배경이 아주 천천히 다가오고(켄 번스), 오른쪽 위에서 빛줄기가 비치고, 가장자리가 어두워짐(비네트),
///          오른쪽 아래에 버전 글씨.
///
/// 인트로 중에 아무 키나 누르면 건너뛴다. 게임 중에 타이틀로 돌아오면 인트로 없이 짧게 밝아지고 바로 메뉴가 뜬다.
/// 시간 정지(timeScale 0)와 무관하게 돈다.
/// </summary>
[DisallowMultipleComponent]
public class TitleIntro : MonoBehaviour
{
    // ── 시간표 (초) ─────────────────────────
    private const float RevealStart = 0.5f, RevealTime = 2.2f;   // 로고 드러나기
    private const float GlowStart = 1.6f, GlowTime = 1.6f;       // 로고 뒤 빛
    private const float CurtainStart = 2.2f, CurtainTime = 1.4f; // 까만 막 걷힘
    private const float PromptAt = 3.6f;                         // "아무 키나 누르세요"
    private const float QuickStart = 2.2f, QuickMenuAt = 2.9f;   // 두 번째부터는 여기서 시작해서 바로 메뉴
    private const float MenuFade = 0.5f, MenuStagger = 0.08f, MenuRise = 15f;
    private const float MenuReadyAt = 0.25f;                     // 이때부터 누를 수 있다 (같은 키로 바로 눌리지 않게)
    private const float InkTime = 0.35f;

    private static readonly Color CurtainColor = new Color(0.02f, 0.027f, 0.04f, 1f);

    private static bool playedOnce;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => playedOnce = false;
#endif

    private enum Phase { Intro, Press, Menu }

    private class MenuItem
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TextMeshProUGUI text;
        public Vector2 basePos;
    }

    private Phase phase;
    private bool quick;
    private float t;           // 인트로 시계
    private float pressClock;  // "아무 키나" 시계
    private float menuClock;   // 메뉴 시계
    private bool menuReady;

    private RectTransform kenBurns;
    private readonly List<Image> rays = new List<Image>();
    private readonly List<float> rayAlpha = new List<float>();
    private Image curtain;
    private TextMeshProUGUI prompt;
    private TitleLogoLight logoLight;
    private CanvasGroup logoGroup, glowGroup, shineGroup;
    private readonly List<MenuItem> menu = new List<MenuItem>();
    private readonly List<CanvasGroup> icons = new List<CanvasGroup>();

    private RectTransform flame, ink;
    private Image flameImage, inkImage;
    private MenuItem marked;
    private float inkClock;

    private readonly List<Object> made = new List<Object>();

    // ── 준비 ───────────────────────────────

    private void Awake()
    {
        quick = playedOnce;
        t = quick ? QuickStart : 0f;
        phase = Phase.Intro;

        Build();
        if (logoLight != null) logoLight.HoldShine(quick ? 1.5f : PromptAt + 0.8f);
        ApplyIntro(t);
        ApplyMenu();
    }

    private void Build()
    {
        Transform bg = transform.Find("Background");
        Transform glow = transform.Find("GameTitleGlow");
        Transform logo = transform.Find("GameTitle");
        Transform shine = transform.Find("GameTitleShine");

        TMP_FontAsset font = null;
        Material fontMaterial = null;

        // 메뉴 글자 3개
        foreach (string n in new[] { "Button_Play", "Button_Guide", "Button_Quit" })
        {
            Transform b = transform.Find(n);
            if (b == null) continue;
            MenuItem item = new MenuItem
            {
                rect = (RectTransform)b,
                group = GetOrAdd<CanvasGroup>(b.gameObject),
                text = b.GetComponent<TextMeshProUGUI>(),
            };
            item.basePos = item.rect.anchoredPosition;
            if (font == null && item.text != null)
            {
                font = item.text.font;
                fontMaterial = item.text.fontSharedMaterial;
            }
            AddHoverSelect(b.gameObject, item.group);
            menu.Add(item);
        }

        foreach (string n in new[] { "Button_Settings", "Button_ExitIcon" })
        {
            Transform b = transform.Find(n);
            if (b != null) icons.Add(GetOrAdd<CanvasGroup>(b.gameObject));
        }

        // 배경을 감싸서 아주 천천히 다가오게 (배경 자체는 화면 비율 맞춤이 붙어 있어 직접 움직이지 않는다)
        int index = 0;
        if (bg != null)
        {
            index = bg.GetSiblingIndex();
            kenBurns = NewRect("KenBurns", transform);
            Stretch(kenBurns);
            kenBurns.pivot = new Vector2(0.62f, 0.55f);
            kenBurns.SetSiblingIndex(index);
            bg.SetParent(kenBurns, false);
        }

        // 오른쪽 위에서 비스듬히 비치는 빛줄기 3개 (시안 1280 기준 × 1.5)
        RectTransform rayRoot = NewRect("GodRays", transform);
        Stretch(rayRoot);
        rayRoot.SetSiblingIndex(index + 1);
        Sprite raySprite = MakeRaySprite();
        AddRay(rayRoot, raySprite, new Vector2(1282f, -390f), new Vector2(170f, 1140f), 0.16f);
        AddRay(rayRoot, raySprite, new Vector2(1380f, -360f), new Vector2(100f, 1080f), 0.14f);
        AddRay(rayRoot, raySprite, new Vector2(1552f, -405f), new Vector2(220f, 1170f), 0.12f);

        // 가장자리 어둡게
        Image vignette = NewImage("Vignette", transform, MakeVignetteSprite(), Color.white);
        Stretch(vignette.rectTransform);
        vignette.transform.SetSiblingIndex(index + 2);

        // 버전 글씨 (오른쪽 아래, 아주 옅게)
        TextMeshProUGUI version = NewText("Version", transform, font, fontMaterial, "v" + Application.version, 18f,
            new Color(0.925f, 0.914f, 0.894f, 0.4f), TextAlignmentOptions.BottomRight);
        RectTransform vr = version.rectTransform;
        vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(1f, 0f);
        vr.sizeDelta = new Vector2(400f, 40f);
        vr.anchoredPosition = new Vector2(-45f, 33f);
        version.characterSpacing = 12f;
        version.transform.SetSiblingIndex(index + 3);

        // 까만 막 (배경·빛줄기·버전 위, 로고 아래)
        curtain = NewImage("IntroCurtain", transform, null, CurtainColor);
        Stretch(curtain.rectTransform);
        curtain.transform.SetSiblingIndex(index + 4);

        // 로고·뒤 빛·지나가는 빛
        if (logo != null)
        {
            logoGroup = GetOrAdd<CanvasGroup>(logo.gameObject);
            logoLight = logo.GetComponent<TitleLogoLight>();
        }
        if (glow != null) glowGroup = GetOrAdd<CanvasGroup>(glow.gameObject);
        if (shine != null) shineGroup = GetOrAdd<CanvasGroup>(shine.gameObject);
        foreach (CanvasGroup g in new[] { logoGroup, glowGroup, shineGroup })
            if (g != null) { g.interactable = false; g.blocksRaycasts = false; }

        // "아무 키나 누르세요" (맨 위)
        prompt = NewText("PressAnyKey", transform, font, fontMaterial, "아무 키나 누르세요", 28f,
            new Color(0.90f, 0.91f, 0.96f, 0f), TextAlignmentOptions.Center);
        RectTransform pr = prompt.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0f);
        pr.sizeDelta = new Vector2(1200f, 60f);
        pr.anchoredPosition = new Vector2(0f, 172f);
        prompt.characterSpacing = 30f;
        prompt.transform.SetAsLastSibling();

        // 여우불 + 먹 붓줄 (고른 메뉴에 붙인다)
        Sprite fireSprite = Resources.Load<Sprite>("Title/Title_FoxFire");
        Sprite inkSprite = Resources.Load<Sprite>("Title/Title_InkLine");
        if (fireSprite != null)
        {
            flameImage = NewImage("FoxFire", transform, fireSprite, Color.white);
            flame = flameImage.rectTransform;
            flame.anchorMin = flame.anchorMax = new Vector2(0f, 0.5f);
            flame.pivot = new Vector2(0.5f, 0.264f); // 불꽃 밑동을 기준으로 일렁인다
            flame.sizeDelta = new Vector2(54f, 66f);
            flame.gameObject.SetActive(false);
        }
        if (inkSprite != null)
        {
            inkImage = NewImage("InkLine", transform, inkSprite, Color.white);
            inkImage.type = Image.Type.Filled;
            inkImage.fillMethod = Image.FillMethod.Horizontal;
            inkImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            ink = inkImage.rectTransform;
            ink.anchorMin = ink.anchorMax = new Vector2(0f, 0.5f);
            ink.pivot = new Vector2(0f, 0.5f);
            ink.gameObject.SetActive(false);
        }
    }

    // ── 매 프레임 ───────────────────────────

    private void OnEnable()
    {
        // 저장 슬롯·설정에서 돌아왔을 때: 연출 없이 메뉴 그대로, 고른 항목만 다시 잡는다.
        if (phase == Phase.Menu)
        {
            menuClock = Mathf.Max(menuClock, MenuReadyAt + MenuFade + MenuStagger * menu.Count);
            marked = null;
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateAmbient();

        switch (phase)
        {
            case Phase.Intro:
                t += dt;
                if (AnyPressed()) t = Mathf.Max(t, quick ? QuickMenuAt : PromptAt);
                ApplyIntro(t);
                ClearSelection();
                if (quick && t >= QuickMenuAt) StartMenu();
                else if (!quick && t >= PromptAt)
                {
                    phase = Phase.Press;
                    pressClock = 0f;
                    playedOnce = true;
                }
                break;

            case Phase.Press:
                pressClock += dt;
                ApplyIntro(PromptAt);
                ClearSelection();
                // 깜빡임: 처음엔 서서히 나타나고, 그다음 2.2초마다 옅어졌다 진해진다
                float blink = 0.575f - 0.325f * Mathf.Cos(pressClock / 2.2f * Mathf.PI * 2f);
                SetAlpha(prompt, blink * Mathf.Clamp01(pressClock / 0.6f));
                if (pressClock > 0.15f && AnyPressed()) StartMenu();
                break;

            case Phase.Menu:
                menuClock += dt;
                if (t < PromptAt) { t = Mathf.Min(t + dt, PromptAt); ApplyIntro(t); }
                if (prompt != null && prompt.color.a > 0f)
                    SetAlpha(prompt, Mathf.MoveTowards(prompt.color.a, 0f, dt / 0.3f));
                ApplyMenu();
                UpdateMarker(dt);
                break;
        }
    }

    private void StartMenu()
    {
        phase = Phase.Menu;
        menuClock = 0f;
        menuReady = false;
        playedOnce = true;
    }

    /// <summary>인트로 시계 time에 맞춰 로고·빛·까만 막을 놓는다.</summary>
    private void ApplyIntro(float time)
    {
        float reveal = quick ? 1f : Smooth((time - RevealStart) / RevealTime);
        if (logoGroup != null)
        {
            // 셰이더로 왼쪽부터 드러낸다. 셰이더가 없으면 그냥 서서히 나타난다.
            bool wiped = logoLight != null && logoLight.SetReveal(reveal >= 1f ? 2f : Mathf.Lerp(-0.15f, 1.15f, reveal));
            logoGroup.alpha = wiped ? 1f : reveal;
        }
        if (shineGroup != null) shineGroup.alpha = reveal;
        if (glowGroup != null) glowGroup.alpha = EaseOut((time - GlowStart) / GlowTime);

        float lift = EaseOut((time - CurtainStart) / CurtainTime);
        if (curtain != null)
        {
            SetAlpha(curtain, 1f - lift);
            curtain.enabled = lift < 1f;
        }
        foreach (CanvasGroup icon in icons) icon.alpha = lift;
    }

    /// <summary>메뉴 글자가 하나씩 아래에서 살짝 떠오르며 나타난다.</summary>
    private void ApplyMenu()
    {
        bool shown = phase == Phase.Menu;
        bool ready = shown && menuClock >= MenuReadyAt;

        for (int i = 0; i < menu.Count; i++)
        {
            MenuItem item = menu[i];
            float a = shown ? EaseOut((menuClock - i * MenuStagger) / MenuFade) : 0f;
            item.group.alpha = a;
            item.group.interactable = ready;
            item.group.blocksRaycasts = ready;
            item.rect.anchoredPosition = item.basePos + new Vector2(0f, -(1f - a) * MenuRise);
        }
        foreach (CanvasGroup icon in icons)
        {
            icon.interactable = ready;
            icon.blocksRaycasts = ready;
        }

        if (ready && !menuReady)
        {
            menuReady = true;
            if (menu.Count > 0) Select(menu[0].rect.gameObject);
        }
    }

    // ── 여우불 + 먹 붓줄 ─────────────────────

    private void UpdateMarker(float dt)
    {
        if (!menuReady) return;

        EventSystem es = EventSystem.current;
        GameObject sel = es != null ? es.currentSelectedGameObject : null;

        // 빈 곳을 눌러 선택이 풀리면 키보드로 다시 움직일 수 있게 되돌린다
        if (sel == null || !sel.activeInHierarchy)
        {
            MenuItem back = marked ?? (menu.Count > 0 ? menu[0] : null);
            if (back != null) Select(back.rect.gameObject);
            sel = back != null ? back.rect.gameObject : null;
        }

        MenuItem found = null;
        foreach (MenuItem item in menu)
            if (item.rect.gameObject == sel) { found = item; break; }

        if (found != null && found != marked) Mark(found);

        if (marked == null) return;

        if (ink != null && inkImage != null)
        {
            inkClock += dt;
            inkImage.fillAmount = EaseOut(inkClock / InkTime);
        }

        if (flame != null)
        {
            // 0.9초마다 길쭉해졌다 납작해졌다 하며 일렁인다
            float p = Time.unscaledTime / 0.9f * Mathf.PI * 2f;
            float s = Mathf.Sin(p) + 0.35f * Mathf.Sin(p * 2.3f + 1f);
            flame.localScale = new Vector3(1f - 0.05f * s, 1f + 0.06f * s, 1f);
            SetAlpha(flameImage, 0.9f + 0.1f * Mathf.Sin(p * 1.7f));
        }
    }

    private void Mark(MenuItem item)
    {
        marked = item;
        inkClock = 0f;

        if (flame != null)
        {
            flame.SetParent(item.rect, false);
            flame.anchoredPosition = new Vector2(-38f, -15.6f);
            flame.gameObject.SetActive(true);
        }

        if (ink != null)
        {
            float textW = item.text != null ? item.text.preferredWidth : 180f;
            ink.SetParent(item.rect, false);
            ink.sizeDelta = new Vector2(textW + 14f, 14f);
            ink.anchoredPosition = new Vector2(-6f, -28f);
            inkImage.fillAmount = 0f;
            ink.gameObject.SetActive(true);
        }
    }

    // ── 배경 움직임 ─────────────────────────

    private void UpdateAmbient()
    {
        float now = Time.unscaledTime;

        if (kenBurns != null)
        {
            // 40초에 걸쳐 1.04 → 1.09배로 다가갔다가 다시 돌아온다
            float k = Smooth(Mathf.PingPong(now / 40f, 1f));
            float s = Mathf.Lerp(1.04f, 1.09f, k);
            kenBurns.localScale = new Vector3(s, s, 1f);
            kenBurns.anchoredPosition = new Vector2(-21f * k, 9f * k);
        }

        // 빛줄기: 7초마다 옅어졌다 진해진다 (줄기마다 박자를 조금 어긋나게)
        float[] offsets = { 0f, 2f, 4.5f };
        for (int i = 0; i < rays.Count; i++)
        {
            float w = 0.55f - 0.2f * Mathf.Cos((now + offsets[i % offsets.Length]) / 7f * Mathf.PI * 2f);
            SetAlpha(rays[i], rayAlpha[i] * w);
        }
    }

    // ── 입력 ───────────────────────────────

    private static bool AnyPressed()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) return true;

        Mouse m = Mouse.current;
        if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;

        Gamepad g = Gamepad.current;
        if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame ||
                          g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame ||
                          g.startButton.wasPressedThisFrame))
            return true;

        return false;
    }

    private static void ClearSelection()
    {
        EventSystem es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject != null) es.SetSelectedGameObject(null);
    }

    private static void Select(GameObject go)
    {
        EventSystem es = EventSystem.current;
        if (es != null && go != null) es.SetSelectedGameObject(go);
    }

    /// <summary>마우스를 올리면 그 메뉴가 골라진다 (키보드로 고른 것과 같은 모양).</summary>
    private static void AddHoverSelect(GameObject go, CanvasGroup group)
    {
        EventTrigger trigger = GetOrAdd<EventTrigger>(go);
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ =>
        {
            if (group.interactable) Select(go);
        });
        trigger.triggers.Add(entry);
    }

    // ── 만들기 도우미 ───────────────────────

    private void AddRay(RectTransform root, Sprite sprite, Vector2 center, Vector2 size, float alpha)
    {
        Image ray = NewImage("Ray", root, sprite, new Color(0.77f, 0.82f, 1f, 0f));
        RectTransform r = ray.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = center;
        r.localEulerAngles = new Vector3(0f, 0f, -28f);
        rays.Add(ray);
        rayAlpha.Add(alpha);
    }

    /// <summary>위가 밝고 아래로 사라지는, 양옆이 흐린 빛줄기.</summary>
    private Sprite MakeRaySprite()
    {
        const int w = 32, h = 256;
        Texture2D tex = NewTexture(w, h);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float fromTop = 1f - y / (h - 1f);
            float v = Mathf.Clamp01(1f - fromTop / 0.8f);
            v *= Mathf.Clamp01(fromTop / 0.05f); // 위 끝도 살짝 부드럽게
            for (int x = 0; x < w; x++)
            {
                float u = (x / (w - 1f) - 0.5f) / 0.24f;
                float a = v * Mathf.Exp(-u * u);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        return FinishSprite(tex, px);
    }

    /// <summary>가운데(살짝 오른쪽)는 투명하고 가장자리로 갈수록 어두워지는 그림.</summary>
    private Sprite MakeVignetteSprite()
    {
        const int w = 160, h = 90;
        const float cx = 0.55f, cy = 0.5f;
        float rx = Mathf.Max(cx, 1f - cx) * 1.4142f;
        float ry = Mathf.Max(cy, 1f - cy) * 1.4142f;
        Texture2D tex = NewTexture(w, h);
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x / (w - 1f) - cx) / rx, dy = (y / (h - 1f) - cy) / ry;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = 0.72f * Mathf.Clamp01((d - 0.45f) / 0.55f);
            px[y * w + x] = new Color32(5, 7, 10, (byte)(a * 255f));
        }
        return FinishSprite(tex, px);
    }

    private Texture2D NewTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave,
        };
        made.Add(tex);
        return tex;
    }

    private Sprite FinishSprite(Texture2D tex, Color32[] px)
    {
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontSave;
        made.Add(s);
        return s;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform r = NewRect(name, parent);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, Material mat,
        string text, float size, Color color, TextAlignmentOptions align)
    {
        RectTransform r = NewRect(name, parent);
        TextMeshProUGUI tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        if (mat != null) tmp.fontSharedMaterial = mat;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    private static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        float r = 1f - x;
        return 1f - r * r * r;
    }

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private void OnDestroy()
    {
        foreach (Object o in made)
            if (o != null) Destroy(o);
    }
}
