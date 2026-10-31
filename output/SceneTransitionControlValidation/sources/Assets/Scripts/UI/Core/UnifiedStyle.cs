using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 10.31 UI 통일 기준 (시안 12번). 바탕은 밤 수묵, 테두리는 격자(LatticeFrame) 하나, 고른 건 먹 붓줄.
///
/// 색·그림·자주 쓰는 조각을 한곳에 모았다. 저장 슬롯·일시정지·설정·조작법이 모두 여기서 꺼내 쓴다.
/// 그림은 전부 코드로 만든다 (붓줄·빛 번짐·가는 테두리). 프리팹·씬은 고치지 않고 실행 중에 입힌다.
/// </summary>
public static class UnifiedStyle
{
    // ── 색 ──
    public static readonly Color Ink = new Color(0.020f, 0.027f, 0.039f, 1f);            // 먹 바탕
    public static readonly Color Panel = new Color(0.043f, 0.059f, 0.082f, 0.92f);      // 창 바탕
    public static readonly Color Line = new Color(0.88f, 0.90f, 0.97f, 0.18f);          // 테두리·구분선
    public static readonly Color LineStrong = new Color(0.84f, 0.87f, 0.95f, 0.55f);    // 고른 테두리·강조 버튼
    public static readonly Color Text = new Color(0.925f, 0.914f, 0.894f, 1f);          // 글씨
    public static readonly Color TextMuted = new Color(0.925f, 0.914f, 0.894f, 0.5f);   // 흐린 글씨
    public static readonly Color MenuIdle = new Color(0.62f, 0.64f, 0.70f, 1f);         // 안 고른 메뉴
    public static readonly Color Moon = new Color(0.84f, 0.87f, 0.95f, 1f);             // 달빛 (강조)
    public static readonly Color MoonText = new Color(0.945f, 0.953f, 0.98f, 1f);       // 고른 글씨
    public static readonly Color Glow = new Color(0.63f, 0.71f, 1f, 0.18f);             // 고른 것 뒤 번짐
    public static readonly Color Danger = new Color(0.85f, 0.63f, 0.58f, 1f);           // 지우기
    public static readonly Color DangerLine = new Color(0.79f, 0.54f, 0.49f, 0.45f);

    public enum ButtonKind { Normal, Primary, Danger }

    // ── 그림 (코드로 한 번만 만든다) ──

    private static Sprite inkSprite, glowSprite, borderSprite, diamondSprite, fadeLineSprite;

    /// <summary>먹 붓줄: 왼쪽이 진하고 오른쪽으로 갈수록 마르는 가로 붓질.</summary>
    public static Sprite InkSprite()
    {
        if (inkSprite != null) return inkSprite;
        const int w = 256, h = 16;
        Texture2D tex = NewTexture(w, h);
        System.Random rnd = new System.Random(7);
        float[] edge = new float[w];
        for (int x = 0; x < w; x++) edge[x] = (float)rnd.NextDouble();
        for (int x = 1; x < w - 1; x++) edge[x] = (edge[x - 1] + edge[x] + edge[x + 1]) / 3f;

        Color32[] px = new Color32[w * h];
        for (int x = 0; x < w; x++)
        {
            float u = x / (w - 1f);
            float thick = Mathf.Lerp(5.5f, 2.2f, u) + (edge[x] - 0.5f) * 1.6f;
            float center = h * 0.5f + Mathf.Sin(u * 3.1f + 0.4f) * 1.2f;
            float fade = u < 0.7f ? Mathf.Lerp(1f, 0.33f, u / 0.7f) : 0.33f * (1f - (u - 0.7f) / 0.3f);
            fade *= Mathf.Clamp01(u * 40f);   // 시작이 뭉툭하지 않게
            for (int y = 0; y < h; y++)
            {
                float d = Mathf.Abs(y + 0.5f - center) - thick * 0.5f;
                float a = Mathf.Clamp01(0.5f - d) * fade;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        inkSprite = Finish(tex, px, Vector4.zero);
        return inkSprite;
    }

    /// <summary>가운데가 진하고 바깥으로 사라지는 둥근 빛.</summary>
    public static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int s = 64;
        Texture2D tex = NewTexture(s, s);
        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        glowSprite = Finish(tex, px, Vector4.zero);
        return glowSprite;
    }

    /// <summary>1px 선 상자 (9-slice). 버튼·카드 테두리.</summary>
    public static Sprite BorderSprite()
    {
        if (borderSprite != null) return borderSprite;
        const int s = 8;
        Texture2D tex = NewTexture(s, s);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool edge = x == 0 || y == 0 || x == s - 1 || y == s - 1;
                px[y * s + x] = edge ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        borderSprite = Finish(tex, px, new Vector4(2, 2, 2, 2));
        return borderSprite;
    }

    /// <summary>슬라이더 손잡이용 작은 마름모 (테두리만 밝다).</summary>
    public static Sprite DiamondSprite()
    {
        if (diamondSprite != null) return diamondSprite;
        const int s = 32;
        Texture2D tex = NewTexture(s, s);
        Color32[] px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Abs(x + 0.5f - s / 2f) + Mathf.Abs(y + 0.5f - s / 2f);
                float outer = Mathf.Clamp01(s / 2f - 1f - d + 0.5f);
                float inner = Mathf.Clamp01(s / 2f - 4f - d + 0.5f);
                // 테두리는 흰색, 안은 먹색
                Color c = Color.Lerp(new Color(1f, 1f, 1f, outer), new Color(0.04f, 0.06f, 0.08f, outer), inner);
                px[y * s + x] = c;
            }
        diamondSprite = Finish(tex, px, Vector4.zero);
        return diamondSprite;
    }

    /// <summary>가운데가 진하고 양 끝으로 사라지는 가는 선.</summary>
    public static Sprite FadeLineSprite()
    {
        if (fadeLineSprite != null) return fadeLineSprite;
        const int w = 128;
        Texture2D tex = NewTexture(w, 1);
        Color32[] px = new Color32[w];
        for (int x = 0; x < w; x++)
        {
            float u = Mathf.Abs(x / (w - 1f) - 0.5f) * 2f;
            px[x] = new Color32(255, 255, 255, (byte)((1f - u * u) * 255f));
        }
        fadeLineSprite = Finish(tex, px, Vector4.zero);
        return fadeLineSprite;
    }

    private static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }

    private static Sprite Finish(Texture2D tex, Color32[] px, Vector4 border)
    {
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, border);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }

    // ── 조각 ──

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : 5;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }

    public static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, string text,
        float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    public static void Stretch(RectTransform r, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>왼쪽 위 기준으로 놓는다 (1920×1080 화면 기준 좌표).</summary>
    public static void PlaceTopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
    }

    /// <summary>제목 + 먹 붓줄. 창 제목은 전부 이 모양.</summary>
    public static TextMeshProUGUI Title(Transform parent, TMP_FontAsset font, string text, float x, float y, float size = 44f)
    {
        TextMeshProUGUI t = NewText("Title", parent, font, text, size, MoonText);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 18f;
        PlaceTopLeft(t.rectTransform, x, y, 900f, size * 1.4f);

        Image ink = NewImage("TitleInk", parent, new Color(0.88f, 0.90f, 0.97f, 0.8f), InkSprite());
        PlaceTopLeft(ink.rectTransform, x - 6f, y + size * 1.32f, Mathf.Max(160f, t.GetPreferredValues().x + 30f), 9f);
        return t;
    }

    /// <summary>화면 아래 조작 안내 한 줄.</summary>
    public static TextMeshProUGUI Hint(Transform parent, TMP_FontAsset font, string text, float x, float bottom = 44f)
    {
        TextMeshProUGUI t = NewText("Hint", parent, font, text, 18f, new Color(Text.r, Text.g, Text.b, 0.45f));
        t.characterSpacing = 8f;
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(x, bottom);
        r.sizeDelta = new Vector2(1400f, 30f);
        return t;
    }

    /// <summary>
    /// 버튼을 "선 하나짜리 상자 + 명조 글씨"로 바꾼다. 강조(Primary)는 한 화면에 하나, 위험(Danger)은 지우기만.
    /// 원래 버튼의 배경 그림을 가는 테두리로 바꾸고, 고르면 테두리·글씨가 달빛으로 밝아진다.
    /// </summary>
    public static void StyleButton(Button button, ButtonKind kind = ButtonKind.Normal, TMP_FontAsset font = null)
    {
        if (button == null) return;

        Image bg = button.targetGraphic as Image;
        if (bg == null) bg = button.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = BorderSprite();
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 2f;   // 선을 1px로
            bg.color = Color.white;
            button.targetGraphic = bg;

            // 안을 아주 옅게 채우는 판 (강조 버튼만 달빛이 살짝)
            Transform fill = button.transform.Find("Fill");
            Image f = fill != null ? fill.GetComponent<Image>() : NewImage("Fill", button.transform,
                kind == ButtonKind.Primary ? new Color(0.63f, 0.71f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.25f));
            Stretch(f.rectTransform, 1f, 1f, 1f, 1f);
            f.transform.SetSiblingIndex(0);
        }

        Color idle = kind == ButtonKind.Primary ? LineStrong : kind == ButtonKind.Danger ? DangerLine : Line;
        Color hot = kind == ButtonKind.Danger ? Danger : Moon;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = idle;
        cb.highlightedColor = hot;
        cb.selectedColor = hot;
        cb.pressedColor = new Color(hot.r * 0.8f, hot.g * 0.8f, hot.b * 0.8f, 1f);
        cb.disabledColor = new Color(idle.r, idle.g, idle.b, 0.25f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        foreach (TextMeshProUGUI t in button.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (font != null) t.font = font;
            t.color = kind == ButtonKind.Danger ? Danger : kind == ButtonKind.Primary ? MoonText : new Color(0.79f, 0.80f, 0.83f, 1f);
            t.characterSpacing = 10f;
            t.fontStyle = FontStyles.Normal;
        }
    }

    private static Sprite bandSprite;

    /// <summary>왼쪽이 달빛으로 옅게 차 있고 오른쪽으로 사라지는 띠 (설정 줄을 고를 때).</summary>
    public static Sprite BandSprite()
    {
        if (bandSprite != null) return bandSprite;
        const int w = 64;
        Texture2D tex = NewTexture(w, 1);
        Color32[] px = new Color32[w];
        for (int x = 0; x < w; x++)
        {
            float u = x / (w - 1f);
            px[x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - u / 0.7f));
        }
        bandSprite = Finish(tex, px, Vector4.zero);
        return bandSprite;
    }

    /// <summary>뒤를 덮는 어두운 판 (오른쪽으로 갈수록 조금 옅게). 화면 전체를 넉넉히 덮는다.</summary>
    public static Image Backdrop(Transform parent, float left = 0.9f, float right = 0.72f)
    {
        Image img = NewImage("UnifiedBackdrop", parent, Color.white, GradientSprite(left, right));
        Stretch(img.rectTransform, -400f, -400f, -400f, -400f);
        img.raycastTarget = true;   // 뒤의 것을 못 누르게
        img.transform.SetSiblingIndex(0);
        return img;
    }

    private static readonly Dictionary<int, Sprite> gradients = new Dictionary<int, Sprite>();

    private static Sprite GradientSprite(float left, float right)
    {
        int key = Mathf.RoundToInt(left * 100) * 1000 + Mathf.RoundToInt(right * 100);
        if (gradients.TryGetValue(key, out Sprite s) && s != null) return s;
        const int w = 64;
        Texture2D tex = NewTexture(w, 1);
        Color32[] px = new Color32[w];
        for (int x = 0; x < w; x++)
        {
            float a = Mathf.Lerp(left, right, x / (w - 1f));
            px[x] = new Color(Ink.r, Ink.g, Ink.b, a);
        }
        s = Finish(tex, px, Vector4.zero);
        gradients[key] = s;
        return s;
    }

    /// <summary>안에 있는 아무 TMP 글씨의 폰트 (제주명조로 통일돼 있다).</summary>
    public static TMP_FontAsset FontOf(Component root)
    {
        TextMeshProUGUI any = root != null ? root.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        return any != null ? any.font : TMP_Settings.defaultFontAsset;
    }
}
