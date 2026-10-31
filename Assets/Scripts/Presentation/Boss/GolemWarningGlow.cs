using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 골렘 공격 경고(빨간 네모)를 빛나는 경고로 바꾼다.
///
///   네모 채움  → 바닥 쪽이 밝고 위로 갈수록 옅어지는 빛 (양옆도 부드럽게)
///   테두리     → 위·양옆 테두리는 끄고, 바닥 선만 밝게
///   빛         → 경고 자리에 붉은 Light2D를 켜서 주변 바닥을 물들임 (경고가 짙어질수록 밝게, 공격 순간 번쩍)
///
/// GolemBossArea·GolemBossController(관용 쪽 코드)는 고치지 않는다.
/// 실행 중에 "Golem Warning" 오브젝트를 찾아 모양만 바꾼다. 색은 원래 경고 색을 그대로 쓴다.
/// </summary>
public class GolemWarningGlow : MonoBehaviour
{
    private SpriteRenderer fill, bottom;
    private readonly List<SpriteRenderer> others = new List<SpriteRenderer>();
    private Light2D glow;
    private float width;
    private GolemBossArea area;
    private bool landed;

    private static Sprite gradient;
    private static float nextScan;

    // ── 찾기 ───────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("GolemWarningGlow (Runtime)") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<Scanner>();
    }

    private class Scanner : MonoBehaviour
    {
        private float nextBossCheck;
        private bool hasBoss;

        private void Update()
        {
            // 골렘이 있는 씬에서만 찾는다 (1초에 한 번 확인)
            if (Time.unscaledTime >= nextBossCheck)
            {
                nextBossCheck = Time.unscaledTime + 1f;
                hasBoss = FindAnyObjectByType<GolemBossController>() != null;
            }
            if (!hasBoss || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.03f;
            foreach (GolemBossArea area in FindObjectsByType<GolemBossArea>())
                if (area.GetComponent<GolemWarningGlow>() == null) area.gameObject.AddComponent<GolemWarningGlow>();
        }
    }

    // ── 모양 바꾸기 ─────────────────────────

    private void Start()
    {
        area = GetComponent<GolemBossArea>();
        width = area != null ? area.Size.x : 2f;
        float height = area != null ? area.Size.y : 1f;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.name == "Area") fill = sr;
            else if (sr.name == "Border") others.Add(sr);
        }
        if (fill != null)
        {
            Sprite old = fill.sprite;
            fill.sprite = Gradient(old);   // 같은 크기 그림이라 원래 크기 설정이 그대로 맞는다
        }

        // 바닥 테두리 하나만 남긴다 (가장 아래에 있는 가로 테두리)
        float lowest = float.MaxValue;
        foreach (SpriteRenderer b in others)
        {
            Bounds bb = b.bounds;
            if (bb.size.x > bb.size.y && bb.center.y < lowest) { lowest = bb.center.y; bottom = b; }
        }
        foreach (SpriteRenderer b in others) if (b != bottom) b.enabled = false;
        others.Remove(bottom);

        // 붉은 빛
        GameObject l = new GameObject("WarningLight");
        l.transform.SetParent(transform, false);
        l.transform.localPosition = new Vector3(0f, -height * 0.5f + 0.3f, 0f);
        glow = l.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.pointLightOuterRadius = Mathf.Max(2.5f, width * 0.75f);
        glow.pointLightInnerRadius = 0f;
        glow.falloffIntensity = 0.65f;
        glow.shadowsEnabled = false;
        glow.intensity = 0f;
        // 빛은 바닥·캐릭터 쪽만 (배경·UI 제외)
        List<int> ids = new List<int>();
        foreach (SortingLayer sl in SortingLayer.layers)
            if (sl.name != "UI" && sl.name != "BackGround") ids.Add(sl.id);
        glow.targetSortingLayers = ids.ToArray();
        Update();
    }

    private void Update()
    {
        // 공격이 땅에 닿는 순간 (경고 → 실제 공격): 돌 파편·먼지·작은 균열·흔들림
        if (!landed && area != null && area.IsCommitted)
        {
            landed = true;
            Vector3 ground = new Vector3(area.Center.x, area.Center.y - area.Size.y * 0.5f, 0f);
            float w = Mathf.Max(1.5f, area.Size.x);
            BossStageFx.Debris(ground + Vector3.up * 0.2f, Mathf.Clamp(Mathf.RoundToInt(w * 1.5f), 4, 12), w * 0.8f + 1f, 6f);
            BossFog.LandingDust(ground, w);
            BossStageFx.Crack(ground, ground.y, w * 1.1f, 1.2f);
            CameraShake.Shake(CameraShake.Light);
        }
        if (fill == null) return;
        Color c = fill.color;
        float a = c.a;   // 경고가 진행될수록 GolemBossArea가 0.18 → 0.55로 올림, 공격 순간 0.65
        float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 12f);

        if (bottom != null && bottom.enabled)
        {
            Color b = Color.Lerp(c, Color.white, 0.35f);
            b.a = Mathf.Clamp01(a * 1.8f) * pulse;
            bottom.color = b;
        }

        if (glow != null)
        {
            glow.color = new Color(Mathf.Max(c.r, 0.3f), c.g * 0.8f + 0.1f, c.b * 0.8f + 0.08f, 1f);
            glow.intensity = Mathf.Clamp01((a - 0.1f) / 0.5f) * 1.6f * pulse;
        }
    }

    // 원래 네모와 같은 크기의 그림: 바닥이 밝고 위로 옅어짐, 양옆 부드럽게
    private static Sprite Gradient(Sprite like)
    {
        if (gradient != null && like != null && Mathf.Approximately(gradient.bounds.size.x, like.bounds.size.x)) return gradient;
        const int W = 64, H = 64;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            name = "GolemWarningGradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        Color32[] px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float v = (y + 0.5f) / H;                       // 0 = 바닥
            float up = Mathf.Pow(1f - v, 1.6f);             // 바닥이 진함
            float base_ = 0.25f + 0.75f * up;
            float floorLine = Mathf.Exp(-v * 22f) * 0.6f;   // 바닥에 붙은 밝은 띠
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                float side = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) / 0.12f);
                float a = Mathf.Clamp01((base_ + floorLine) * side);
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        float sx = like != null ? like.bounds.size.x : 1f;
        gradient = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W / Mathf.Max(0.001f, sx), 0, SpriteMeshType.FullRect);
        gradient.name = "GolemWarningGradient";
        gradient.hideFlags = HideFlags.DontSave;
        return gradient;
    }

    private void OnDestroy()
    {
        if (glow != null) Destroy(glow.gameObject);
    }
}
