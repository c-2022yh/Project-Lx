using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 제단에 처음 불을 밝힐 때 석등 안에 파란 불이 붙는 연출. (디자인 시안 17 - A 등불 점화)
///
///   불씨가 반짝 → 불꽃이 솟았다가 자리 잡고 일렁임 → 빛 물결이 퍼지고 바닥이 밝아짐 → 불티가 올라감.
///   석등은 회색에서 원래 색으로 천천히 돌아온다.
///
/// 불은 선 없이 흐린 빛 여러 겹으로만 그린다 (뒤쪽 큰 빛 · 겉 빛 · 몸통 · 끝 불꽃 둘 · 하얀 심지).
/// 그림은 코드로 만든다. 나중에 진짜 그림이 오면 Art 안의 그림만 바꾸면 된다.
///
/// Shrine.cs(관용님 파일)는 고치지 않는다. IsActive / IsPlayerInRange만 읽는다.
/// 씬이 열릴 때 모든 제단에 스스로 붙는다. 프리팹도 건드리지 않는다.
/// UIManager는 IgniteThen으로 "불이 다 붙은 뒤에 유물창 열기"를 맡긴다.
/// 창이 열려 게임이 멈춰도 불은 계속 일렁이도록 실제 시간(unscaled)으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class ShrineFire : MonoBehaviour
{
    [Header("위치 (제단 그림 기준 0~1)")]
    [Tooltip("불꽃 아래쪽이 놓일 자리. x는 왼→오, y는 아래→위. 기본값은 석등 칸 바닥")]
    [SerializeField] private Vector2 flameBase = new Vector2(0.5f, 0.485f);
    [Tooltip("불꽃 높이 (제단 그림 높이에 대한 비율)")]
    [SerializeField] private float flameHeight = 0.19f;

    [Header("시간 (초)")]
    [Tooltip("불씨가 반짝인 뒤 불이 솟기까지")]
    [SerializeField] private float sparkTime = 0.3f;
    [Tooltip("불이 솟은 뒤 유물창이 열리기까지")]
    [SerializeField] private float holdAfterLit = 0.6f;

    [Header("색")]
    [SerializeField] private Color flameColor = new Color(0.62f, 0.82f, 1f, 0.95f);
    [SerializeField] private Color hazeColor = new Color(0.23f, 0.45f, 1f, 0.55f);
    [SerializeField] private Color glowColor = new Color(0.45f, 0.68f, 1f, 0.35f);

    private static readonly Color TipColorL = new Color(0.66f, 0.84f, 1f, 1f);
    private static readonly Color TipColorR = new Color(0.55f, 0.77f, 1f, 1f);
    private static readonly Color WispColor = new Color(0.86f, 0.94f, 1f, 0.95f);

    // 불이 솟는 모양: 확 솟았다가(1.35) 살짝 내려앉고(0.9) 자리 잡음
    private static readonly float[] GrowX = { 0f, 0.35f, 0.6f, 1f };
    private static readonly float[] GrowSY = { 0f, 1.35f, 0.9f, 1f };
    private static readonly float[] GrowSX = { 0.4f, 0.85f, 1.05f, 1f };
    // 뒤쪽 빛: 커졌다가 조금 줄어듦
    private static readonly float[] GlowX = { 0f, 0.4f, 1f };
    private static readonly float[] GlowS = { 0.3f, 1.25f, 1f };
    private static readonly float[] GlowA = { 0f, 1f, 0.85f };

    private enum State { Off, Igniting, Lit, Fading }

    private Shrine shrine;
    private SpriteRenderer shrineSr;
    private Color inactiveColor = Color.gray;
    private Color activeColor = Color.white;
    private bool ready;

    private SpriteRenderer halo, ground, haze, body, tipL, tipR, core;
    private Vector3 basePos;     // 제단 그림 안에서 불꽃 아래쪽 (로컬)
    private Vector3 groundPos;   // 제단 발밑 (로컬)
    private float h;             // 불꽃 높이 (로컬)
    private float shrineW;

    private State state = State.Off;
    private float t;             // 불붙이기 시작부터 흐른 시간
    private float clock;         // 일렁임용 시계
    private float fade = 1f;     // 꺼질 때 1 → 0
    private float emberTimer;
    private Action pending;
    private bool pendingDone;

    // ── 붙이기 ─────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

    private static void AttachAll()
    {
        foreach (Shrine s in FindObjectsByType<Shrine>())
        {
            if (s != null && s.GetComponent<ShrineFire>() == null) s.gameObject.AddComponent<ShrineFire>();
        }
    }

    /// <summary>
    /// 아직 불이 안 붙은 제단이면 불을 붙이고, 다 붙은 뒤 onLit을 부른다 (true).
    /// 이미 불이 켜진 제단이면 아무것도 안 하고 false. 부른 쪽이 바로 창을 열면 된다.
    /// </summary>
    public static bool IgniteThen(Shrine target, Action onLit)
    {
        if (target == null) return false;
        ShrineFire fire = target.GetComponent<ShrineFire>();
        if (fire == null || !fire.ready || !fire.isActiveAndEnabled) return false;

        switch (fire.state)
        {
            case State.Lit:
                return false;
            case State.Igniting:
                if (fire.pendingDone) return false;
                fire.pending = onLit;   // 불붙는 중에 또 누르면 한 번만 연다
                return true;
            default:
                fire.StartIgnite();
                fire.pending = onLit;
                return true;
        }
    }

    // ── 준비 ───────────────────────────────

    private void Start()
    {
        shrine = GetComponent<Shrine>();
        if (shrine == null) { enabled = false; return; }

        shrineSr = ReadField<SpriteRenderer>("spriteRenderer", null);
        if (shrineSr == null) shrineSr = GetComponentInChildren<SpriteRenderer>();
        if (shrineSr == null || shrineSr.sprite == null) { enabled = false; return; }

        inactiveColor = ReadField("inactiveColor", Color.gray);
        activeColor = ReadField("activeColor", Color.white);

        Bounds b = shrineSr.sprite.bounds;
        float fx = shrineSr.flipX ? 1f - flameBase.x : flameBase.x;
        basePos = new Vector3(Mathf.Lerp(b.min.x, b.max.x, fx), Mathf.Lerp(b.min.y, b.max.y, flameBase.y), 0f);
        groundPos = new Vector3(b.center.x, b.min.y + b.size.y * 0.05f, 0f);
        h = b.size.y * flameHeight;
        shrineW = b.size.x;

        int o = shrineSr.sortingOrder;
        halo = Layer("Fire_Halo", BossFxArt.SoftDot, o - 1);    // 제단 뒤
        ground = Layer("Fire_Ground", BossFxArt.SoftDot, o - 1);
        haze = Layer("Fire_Haze", BossFxArt.SoftDot, o + 1);
        body = Layer("Fire_Body", Art.Flame, o + 2);
        tipL = Layer("Fire_TipL", Art.Flame, o + 3);
        tipR = Layer("Fire_TipR", Art.Flame, o + 3);
        core = Layer("Fire_Core", BossFxArt.SoftDot, o + 4);

        ready = true;
        if (shrine.IsActive) LightNow();   // 이어하기로 이미 켜진 제단
    }

    private SpriteRenderer Layer(string name, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(shrineSr.transform, false);
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sharedMaterial = BossFxArt.Unlit;
        r.sortingLayerID = shrineSr.sortingLayerID;
        r.sortingOrder = order;
        r.enabled = false;
        return r;
    }

    private T ReadField<T>(string name, T fallback)
    {
        FieldInfo f = typeof(Shrine).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null && f.GetValue(shrine) is T v) return v;
        return fallback;
    }

    // ── 상태 ───────────────────────────────

    private void StartIgnite()
    {
        state = State.Igniting;
        t = 0f;
        fade = 1f;
        pending = null;
        pendingDone = false;
        emberTimer = sparkTime + 0.2f;

        Transform tr = shrineSr.transform;
        float unit = Mathf.Abs(tr.lossyScale.y);
        string layer = SortingLayer.IDToName(shrineSr.sortingLayerID);
        int o = shrineSr.sortingOrder + 5;

        // 작은 불씨가 반짝
        BossFxSprite spark = BossFxSprite.Create("Fire_Spark", BossFxArt.SoftDot, tr.TransformPoint(basePos + new Vector3(0f, h * 0.15f, 0f)), layer, o);
        spark.color = new Color(0.9f, 0.97f, 1f, 1f);
        spark.scaleFrom = Vector3.one * (h * unit * 0.05f);
        spark.scaleTo = Vector3.one * (h * unit * 0.35f);
        spark.alphaFrom = 1f;
        spark.alphaTo = 0.3f;
        spark.life = sparkTime + 0.3f;
        spark.unscaled = true;

        // 불이 솟는 순간 퍼지는 빛 물결 (선 없이 흐린 띠)
        BossFxSprite wave = BossFxSprite.Create("Fire_Wave", Art.SoftRing, tr.TransformPoint(basePos + new Vector3(0f, h * 0.4f, 0f)), layer, o);
        wave.delay = sparkTime;
        wave.color = new Color(0.67f, 0.84f, 1f, 0.6f);
        wave.scaleFrom = Vector3.one * (h * unit * 0.4f);
        wave.scaleTo = Vector3.one * (h * unit * 2.8f);
        wave.alphaFrom = 0.9f;
        wave.alphaTo = 0f;
        wave.life = 0.9f;
        wave.unscaled = true;
    }

    private void LightNow()
    {
        state = State.Lit;
        t = 99f;
        fade = 1f;
    }

    private void LateUpdate()
    {
        if (!ready) return;
        float dt = Time.unscaledDeltaTime;
        bool active = shrine.IsActive;

        if (state == State.Off || state == State.Fading)
        {
            if (active)
            {
                // 플레이어가 직접 밝힌 것만 연출. 씬을 열 때 이미 켜져 있던 건 바로 켠다.
                if (shrine.IsPlayerInRange && Time.timeSinceLevelLoad > 1f) StartIgnite();
                else LightNow();
            }
        }
        else if (state == State.Lit && !active)
        {
            state = State.Fading;   // 다른 제단을 밝히면 꺼진다
            fade = 1f;
        }

        if (state == State.Off) return;
        clock += dt;

        if (state == State.Igniting)
        {
            t += dt;
            // 석등 색: 불씨 뒤로 회색 → 원래 색
            shrineSr.color = Color.Lerp(inactiveColor, activeColor, Mathf.Clamp01((t - sparkTime * 0.6f) / 0.8f));

            if (!pendingDone && t >= sparkTime + holdAfterLit)
            {
                pendingDone = true;
                Action cb = pending;
                pending = null;
                cb?.Invoke();
            }

            if (t >= sparkTime + 1f)
            {
                if (active) { state = State.Lit; shrineSr.color = activeColor; }
                else { state = State.Fading; fade = 1f; }
            }
        }

        if (state == State.Fading)
        {
            fade -= dt / 0.5f;
            if (fade <= 0f)
            {
                fade = 0f;
                state = State.Off;
                HideAll();
                return;
            }
        }

        Animate();
        Wisps(dt);
    }

    private void OnDisable()
    {
        if (!ready) return;
        state = State.Off;
        pending = null;
        HideAll();
    }

    // ── 그리기 ─────────────────────────────

    private void Animate()
    {
        bool igniting = state == State.Igniting;
        float g = igniting ? Mathf.Clamp01((t - sparkTime) / 0.7f) : 1f;
        float sy = Keys(g, GrowX, GrowSY);
        float sx = Keys(g, GrowX, GrowSX);
        float ga = Mathf.Clamp01(g / 0.35f) * fade;

        float gl = igniting ? Mathf.Clamp01((t - sparkTime) / 0.9f) : 1f;
        float glowS = Keys(gl, GlowX, GlowS);
        float glowA = Keys(gl, GlowX, GlowA) * fade;

        float sway = Mathf.Sin(clock * 5.7f);
        float sway2 = Mathf.Sin(clock * 3.1f + 1.3f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 3.9f);
        float l = 0.5f + 0.5f * Mathf.Sin(clock * 4.8f);
        float r = 0.5f + 0.5f * Mathf.Sin(clock * 3.7f + 2f);

        // 제단 뒤로 번지는 큰 빛 + 발밑 빛
        Set(halo, basePos + new Vector3(0f, h * 0.45f, 0f), new Vector2(h * 3.4f, h * 3.4f) * glowS, glowColor, glowA * Mathf.Lerp(0.75f, 1f, pulse));
        Set(ground, groundPos, new Vector2(shrineW * 1.8f, shrineW * 0.3f) * glowS, glowColor, glowA * 0.8f);

        // 불꽃 (아래 기준)
        Set(haze, basePos + new Vector3(0f, h * 0.4f * sy, 0f), new Vector2(h * 0.8f * sx, h * 0.85f * sy), hazeColor, ga);
        Set(body, basePos + new Vector3(h * 0.02f * sway2, 0f, 0f), new Vector2(h * 0.55f * sx * (1f - 0.03f * sway), h * sy * (1f + 0.06f * sway)), flameColor, ga);
        Set(tipL, basePos + new Vector3(-h * 0.07f - h * 0.04f * l, h * (0.72f + 0.08f * l) * sy, 0f), new Vector2(h * 0.16f, h * 0.36f) * Mathf.Lerp(1f, 0.75f, l) * sy, TipColorL, Mathf.Lerp(0.8f, 0.35f, l) * ga);
        Set(tipR, basePos + new Vector3(h * 0.1f + h * 0.04f * r, h * (0.52f - 0.04f * r) * sy, 0f), new Vector2(h * 0.13f, h * 0.3f) * Mathf.Lerp(0.8f, 1f, r) * sy, TipColorR, Mathf.Lerp(0.45f, 0.85f, r) * ga);
        Set(core, basePos + new Vector3(0f, h * 0.3f * sy, 0f), new Vector2(h * 0.32f, h * 0.48f * sy) * Mathf.Lerp(1f, 1.08f, pulse), Color.white, Mathf.Lerp(0.85f, 1f, pulse) * ga);
    }

    // 불꽃 끝에서 흐린 불티가 올라가다 사라짐
    private void Wisps(float dt)
    {
        if (state == State.Fading) return;
        if (state == State.Igniting && t < sparkTime + 0.15f) return;
        emberTimer -= dt;
        if (emberTimer > 0f) return;
        emberTimer = UnityEngine.Random.Range(0.16f, 0.32f);

        Transform tr = shrineSr.transform;
        float hw = h * Mathf.Abs(tr.lossyScale.y);
        Vector3 p = tr.TransformPoint(basePos + new Vector3(UnityEngine.Random.Range(-0.12f, 0.12f) * h, UnityEngine.Random.Range(0.45f, 0.8f) * h, 0f));
        BossFxSprite fx = BossFxSprite.Create("Fire_Wisp", BossFxArt.SoftDot, p, SortingLayer.IDToName(shrineSr.sortingLayerID), shrineSr.sortingOrder + 5);
        float size = hw * UnityEngine.Random.Range(0.09f, 0.16f);
        fx.scaleFrom = Vector3.one * size;
        fx.scaleTo = Vector3.one * (size * 0.25f);
        fx.color = WispColor;
        fx.alphaFrom = 0.9f;
        fx.alphaTo = 0f;
        fx.fadeIn = 0.2f;
        fx.life = UnityEngine.Random.Range(1.1f, 1.9f);
        fx.velocity = new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f) * hw, UnityEngine.Random.Range(0.6f, 1f) * hw);
        fx.drag = 0.3f;
        fx.unscaled = true;
    }

    private static void Set(SpriteRenderer r, Vector3 localPos, Vector2 size, Color c, float a)
    {
        bool on = a > 0.003f && size.x > 0.0001f && size.y > 0.0001f;
        r.enabled = on;
        if (!on) return;
        r.transform.localPosition = localPos;
        Vector3 s = r.sprite.bounds.size;
        r.transform.localScale = new Vector3(size.x / s.x, size.y / s.y, 1f);
        c.a *= a;
        r.color = c;
    }

    private void HideAll()
    {
        if (halo != null) halo.enabled = false;
        if (ground != null) ground.enabled = false;
        if (haze != null) haze.enabled = false;
        if (body != null) body.enabled = false;
        if (tipL != null) tipL.enabled = false;
        if (tipR != null) tipR.enabled = false;
        if (core != null) core.enabled = false;
    }

    // 키 사이를 부드럽게 잇는 값
    private static float Keys(float x, float[] xs, float[] ys)
    {
        if (x <= xs[0]) return ys[0];
        for (int i = 1; i < xs.Length; i++)
        {
            if (x > xs[i]) continue;
            float k = (x - xs[i - 1]) / (xs[i] - xs[i - 1]);
            k = k * k * (3f - 2f * k);
            return Mathf.Lerp(ys[i - 1], ys[i], k);
        }
        return ys[ys.Length - 1];
    }

    // ── 임시 그림 (코드로 그림, 전부 흰색 · 가장자리 흐림) ──

    private static class Art
    {
        private static Sprite flame, softRing;

        private delegate float Pixel(float u, float v);   // u, v: -1 ~ 1 (v는 아래 -1 → 위 1)

        /// <summary>물방울을 뒤집은 불꽃 모양. 테두리 없이 가장자리로 갈수록 흐려진다. 피벗 아래, 가로 0.5 × 세로 1.</summary>
        public static Sprite Flame => flame != null ? flame : flame = Make("ShrineFire_Flame", 64, 128, new Vector2(0.5f, 0f), 128f, (u, v) =>
        {
            float y = (v + 1f) * 0.5f;                       // 아래 0 → 위 1
            float half = y < 0.3f
                ? Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow((0.3f - y) / 0.3f, 2f)))
                : Mathf.Pow(Mathf.Clamp01(1f - (y - 0.3f) / 0.7f), 1.15f);
            half *= 0.92f;
            if (half <= 0.001f) return 0f;
            float across = Mathf.Clamp01(1f - Mathf.Abs(u) / half);
            float a = Mathf.Pow(across, 0.9f);
            a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(y / 0.12f));
            a *= Mathf.Lerp(1f, 0.55f, y);
            return a;
        });

        /// <summary>넓고 흐린 빛 띠 고리. 선처럼 보이지 않게 두껍고 부드럽다.</summary>
        public static Sprite SoftRing => softRing != null ? softRing : softRing = Make("ShrineFire_SoftRing", 128, 128, new Vector2(0.5f, 0.5f), 128f, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float k = (d - 0.72f) / 0.17f;
            return Mathf.Exp(-k * k) * Mathf.Clamp01((1f - d) * 6f);
        });

        private static Sprite Make(string name, int w, int h, Vector2 pivot, float ppu, Pixel pixel)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(pixel(u, v)) * 255f);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }
    }
}
