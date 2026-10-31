using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유물 UI 시안(10.28 자개 시안)의 색과 그림을 한곳에 모았다.
///   - 자개 그라데이션, 둥근 자개 고리, 퍼지는 빛, 얇은 테두리를 코드로 그려서 쓴다. 그림 파일이 필요 없다.
///   - 계열 색은 시안의 덜 쨍한 색이다. 유물창 칸 테두리(RelicLabels)는 그대로 둔다.
///   - 유물마다 주제 배경색(불, 구름, 물, 달, 번개, 새벽, 그림자)을 고른다.
/// 획득 알림창(AcquirePopup)과 유물 선택 카드(RelicCardStyle)가 같이 쓴다.
/// </summary>
public static class PearlStyle
{
    // ── 색 ──────────────────────────────────

    /// <summary>판 바탕 (옻칠 느낌의 아주 어두운 남색).</summary>
    public static readonly Color Lacquer = Hex(0x12161c);
    public static readonly Color TextTitle = Hex(0xece9e4);
    public static readonly Color TextBody = Hex(0xa3acb1);
    public static readonly Color TextButton = Hex(0xd9d6d1);
    public static readonly Color ButtonLine = new Color(225f / 255f, 220f / 255f, 236f / 255f, 0.45f);
    public static readonly Color DimLine = new Color(220f / 255f, 215f / 255f, 232f / 255f, 0.32f);
    public static readonly Color Neutral = Hex(0xcfe2e3);

    public static readonly Color SwordColor = Hex(0xc99088);   // 연주홍
    public static readonly Color OrbColor = Hex(0xa99fc9);     // 연보라
    public static readonly Color BodyColor = Hex(0x8fb5a8);    // 청자

    private static readonly Color[] WhitePearl =
    {
        Hex(0xece6f2), Hex(0xcfe2e3), Hex(0xf1e2e6), Hex(0xdce8d9), Hex(0xe5def3), Hex(0xcde0e6), Hex(0xefe7ef)
    };
    private static readonly float[] WhiteStops = { 0f, 0.18f, 0.36f, 0.54f, 0.72f, 0.90f, 1f };

    private static readonly float[] TintStops = { 0f, 0.28f, 0.52f, 0.78f, 1f };
    private static readonly Color[] SwordPearl = Rgb(236, 214, 210, 201, 144, 136, 242, 224, 220, 185, 124, 116, 230, 202, 197);
    private static readonly Color[] OrbPearl = Rgb(224, 219, 240, 169, 159, 201, 234, 230, 246, 150, 139, 190, 216, 210, 236);
    private static readonly Color[] BodyPearl = Rgb(216, 234, 228, 143, 181, 168, 228, 242, 237, 122, 163, 150, 207, 228, 221);

    /// <summary>계열 색 (덜 쨍한 시안 색).</summary>
    public static Color CategoryColor(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => SwordColor,
            RelicCategory.Orb => OrbColor,
            _ => BodyColor
        };
    }

    /// <summary>한지 위에 쓰는 계열 글자 색 (밝은 종이에서도 읽히게 진하게).</summary>
    public static Color CategoryInk(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => Hex(0x8e4f47),
            RelicCategory.Orb => Hex(0x63598f),
            _ => Hex(0x4d7a6b)
        };
    }

    /// <summary>바깥 자개 테두리 안쪽에 반투명하게 까는 창살 색.</summary>
    public static Color LatticeColor(RelicCategory? category)
    {
        if (category == null) return new Color(0.91f, 0.89f, 0.94f, 0.34f);

        Color c = category switch
        {
            RelicCategory.Sword => Hex(0xd4a39b),
            RelicCategory.Orb => Hex(0xb9b0d8),
            _ => Hex(0xa3c6ba)
        };
        c.a = 0.55f;
        return c;
    }

    // ── 유물 주제 배경 ───────────────────────

    public struct Theme
    {
        public Color Background;    // 어두운 바탕 (획득창 메달)
        public Color Glow;          // 은은한 빛
        public Color Silk;          // 족자 테두리 비단
        public Color Line;          // 족자 안쪽 테두리선
        public string Wash;         // 족자 수채화 그림 (Resources/UIFrame/Scroll/Scroll_WC_이름)
    }

    private static readonly Theme Fire = new Theme
    {
        Background = Hex(0x2a1a1c), Glow = Rgb1(214, 160, 132),
        Silk = Hex(0xa87f75), Line = new Color(110f / 255f, 62f / 255f, 54f / 255f, 0.75f), Wash = "Fire"
    };
    private static readonly Theme Cloud = new Theme
    {
        Background = Hex(0x1c2633), Glow = Rgb1(169, 194, 212),
        Silk = Hex(0x879db2), Line = new Color(58f / 255f, 78f / 255f, 100f / 255f, 0.75f), Wash = "Cloud"
    };
    private static readonly Theme Water = new Theme
    {
        Background = Hex(0x152a2d), Glow = Rgb1(159, 192, 191),
        Silk = Hex(0x81a19d), Line = new Color(48f / 255f, 88f / 255f, 84f / 255f, 0.75f), Wash = "Water"
    };
    private static readonly Theme Moon = new Theme
    {
        Background = Hex(0x1e1b30), Glow = Rgb1(179, 171, 210),
        Silk = Hex(0x9c95b5), Line = new Color(78f / 255f, 70f / 255f, 112f / 255f, 0.75f), Wash = "Moon"
    };
    private static readonly Theme Thunder = new Theme
    {
        Background = Hex(0x1b1d33), Glow = Rgb1(199, 195, 234),
        Silk = Hex(0x8e8ab0), Line = new Color(70f / 255f, 66f / 255f, 112f / 255f, 0.75f), Wash = "Thunder"
    };
    private static readonly Theme Dawn = new Theme
    {
        Background = Hex(0x2b2230), Glow = Rgb1(208, 174, 163),
        Silk = Hex(0xb09191), Line = new Color(110f / 255f, 70f / 255f, 78f / 255f, 0.75f), Wash = "Dawn"
    };
    private static readonly Theme Shade = new Theme
    {
        Background = Hex(0x18171f), Glow = Rgb1(150, 142, 170),
        Silk = Hex(0x7d7986), Line = new Color(52f / 255f, 48f / 255f, 60f / 255f, 0.75f), Wash = "Shade"
    };

    // 유물 에셋 이름(영어)에 들어 있는 낱말로 고른다. 새 유물은 여기 한 줄 넣으면 된다.
    private static readonly (string key, Theme theme)[] ThemeKeys =
    {
        ("thunder", Thunder),
        ("fire", Fire), ("burning", Fire), ("berserk", Fire), ("reddrop", Fire), ("giantheart", Fire),
        ("feather", Cloud), ("cloud", Cloud), ("wind", Cloud), ("fan", Cloud),
        ("mirror", Water), ("hairpin", Water),
        ("moon", Moon), ("reflux", Moon),
        ("dawn", Dawn), ("twilight", Dawn), ("whiteflower", Dawn), ("lamp", Dawn),
        ("shadow", Shade), ("devour", Shade),
    };

    public static Theme ThemeOf(RelicData relic)
    {
        if (relic == null) return Cloud;

        string n = relic.name.ToLowerInvariant();
        foreach ((string key, Theme theme) in ThemeKeys)
            if (n.Contains(key)) return theme;

        return relic.Category switch
        {
            RelicCategory.Sword => Thunder,
            RelicCategory.Orb => Moon,
            _ => Cloud
        };
    }

    // ── 그림 ────────────────────────────────

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>자개 그라데이션 판. 붙일 칸의 가로·세로 비율을 주면 무늬가 찌그러지지 않는다.</summary>
    public static Sprite Pearl(RelicCategory? category, float aspect)
    {
        int w = 256;
        int h = Mathf.Clamp(Mathf.RoundToInt(w / Mathf.Max(0.1f, aspect)), 16, 512);
        string key = $"pearl_{category?.ToString() ?? "white"}_{w}x{h}";

        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;

        Color[] colors;
        float[] stops;
        float alpha = 1f;

        switch (category)
        {
            case RelicCategory.Sword: colors = SwordPearl; stops = TintStops; alpha = 0.85f; break;
            case RelicCategory.Orb: colors = OrbPearl; stops = TintStops; alpha = 0.85f; break;
            case RelicCategory.Body: colors = BodyPearl; stops = TintStops; alpha = 0.85f; break;
            default: colors = WhitePearl; stops = WhiteStops; break;
        }

        s = Make(key, w, h, (x, y) =>
        {
            Color c = Gradient(x, y, w, h, 120f, colors, stops);
            c.a = alpha;
            return c;
        });
        return s;
    }

    /// <summary>자개 동그라미 (메달 고리용).</summary>
    public static Sprite PearlCircle()
    {
        const int size = 256;
        return Make("pearl_circle", size, size, (x, y) =>
        {
            Color c = Gradient(x, y, size, size, 140f, WhitePearl, WhiteStops);
            c.a = CircleAlpha(x, y, size);
            return c;
        });
    }

    /// <summary>흰 동그라미. 색은 Image.color로 입힌다.</summary>
    public static Sprite Circle()
    {
        const int size = 256;
        return Make("circle", size, size, (x, y) => new Color(1f, 1f, 1f, CircleAlpha(x, y, size)));
    }

    /// <summary>가운데가 밝고 가장자리로 갈수록 사라지는 빛. 색과 세기는 Image.color로.</summary>
    public static Sprite Glow()
    {
        const int size = 128;
        return Make("glow", size, size, (x, y) =>
        {
            float dx = (x + 0.5f) / size * 2f - 1f;
            float dy = (y + 0.5f) / size * 2f - 1f;
            float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            return new Color(1f, 1f, 1f, 1f - d);
        });
    }

    /// <summary>위는 투명하고 아래로 갈수록 진해지는 띠. 색은 Image.color로.</summary>
    public static Sprite VerticalFade()
    {
        const int h = 64;
        return Make("vfade", 4, h, (x, y) => new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, (float)y / (h - 1))));
    }

    /// <summary>Resources에 둔 그림을 한 번만 읽어 둔다. 없으면 null.</summary>
    public static Sprite Load(string path)
    {
        string key = "res_" + path;
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;

        s = Resources.Load<Sprite>(path);
        if (s == null) Debug.LogWarning("[PearlStyle] 그림이 없습니다: Resources/" + path);
        else cache[key] = s;
        return s;
    }

    /// <summary>얇은 테두리만 있는 칸 (9-slice). 칩·버튼 테두리용.</summary>
    public static Sprite Outline()
    {
        string key = "outline";
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;

        const int size = 16;
        const int line = 2;
        var tex = NewTexture(size, size);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool edge = x < line || y < line || x >= size - line || y >= size - line;
            px[y * size + x] = new Color(1f, 1f, 1f, edge ? 1f : 0f);
        }
        tex.SetPixels(px);
        tex.Apply(false, true);

        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                          SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        s.name = key;
        cache[key] = s;
        return s;
    }

    // ── 내부 ────────────────────────────────

    private delegate Color Pixel(int x, int y);

    private static Sprite Make(string key, int w, int h, Pixel pixel)
    {
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;

        Texture2D tex = NewTexture(w, h);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[y * w + x] = pixel(x, y);
        tex.SetPixels(px);
        tex.Apply(false, true);

        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = key;
        cache[key] = s;
        return s;
    }

    private static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    /// <summary>CSS linear-gradient(각도)와 같은 방향으로 색을 고른다. 0도 = 위로, 90도 = 오른쪽으로.</summary>
    private static Color Gradient(int x, int y, int w, int h, float angle, Color[] colors, float[] stops)
    {
        float a = angle * Mathf.Deg2Rad;
        // 텍스처는 y가 위로 증가한다. CSS는 아래로 증가하므로 y 성분 부호가 바뀐다.
        Vector2 dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        float length = Mathf.Abs(w * dir.x) + Mathf.Abs(h * dir.y);
        Vector2 p = new Vector2(x + 0.5f - w / 2f, y + 0.5f - h / 2f);
        float t = Mathf.Clamp01(Vector2.Dot(p, dir) / length + 0.5f);

        for (int i = 1; i < stops.Length; i++)
        {
            if (t > stops[i]) continue;
            float k = Mathf.InverseLerp(stops[i - 1], stops[i], t);
            return Color.Lerp(colors[i - 1], colors[i], k);
        }

        return colors[colors.Length - 1];
    }

    private static float CircleAlpha(int x, int y, int size)
    {
        float r = size / 2f;
        float dx = x + 0.5f - r;
        float dy = y + 0.5f - r;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Clamp01(r - 1f - d + 0.5f);
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
    }

    private static Color Rgb1(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static Color[] Rgb(params int[] v)
    {
        var colors = new Color[v.Length / 3];
        for (int i = 0; i < colors.Length; i++) colors[i] = Rgb1(v[i * 3], v[i * 3 + 1], v[i * 3 + 2]);
        return colors;
    }
}
