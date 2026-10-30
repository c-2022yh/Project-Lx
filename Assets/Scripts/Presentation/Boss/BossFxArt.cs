using UnityEngine;

/// <summary>
/// 보스 연출(달빛 방향)에 쓰는 임시 그림을 코드로 그려 둔다.
/// 그림은 전부 흰색이고, 색은 쓰는 쪽에서 곱해서 입힌다.
/// 나중에 진짜 이펙트 그림이 오면 이 그림 대신 넣으면 된다.
///
///   SoftDot  부드러운 원 (먼지·빛 알갱이·보주 빛)
///   Ring     가는 고리 (충격파·물결)
///   Rays     가운데서 뻗는 빛살 (격파·포효)
///   Beam     위에서 내려오는 빛 기둥 (피벗 위쪽)
///   UpFade   아래가 진하고 위로 옅어지는 판 (공격 경고)
///   FadeLine 가운데가 진하고 양끝이 사라지는 선 (이름 카드)
///   BandV    가운데가 진하고 위아래가 사라지는 띠 (이름 카드 바탕)
///   SoftRect 가장자리가 흐린 네모 (HP바 빛)
///   Diamond  마름모 (HP바 끝·절반 표시)
///   Ink      먹 붓줄 (이름 카드 밑줄)
/// </summary>
public static class BossFxArt
{
    private const string UnlitMaterialPath = "Presentation/M_VfxSprite";

    private static Sprite softDot, ring, rays, beam, upFade, fadeLine, bandV, softRect, diamond, ink, white;
    private static Material unlit;

    private delegate float Pixel(float u, float v);   // u, v: -1 ~ 1

    public static Sprite SoftDot => softDot != null ? softDot : softDot = Make("BossFx_SoftDot", 128, 128, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
        return a * a;
    });

    public static Sprite Ring => ring != null ? ring : ring = Make("BossFx_Ring", 256, 256, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        float d = Mathf.Sqrt(u * u + v * v);
        float k = (d - 0.88f) / 0.07f;
        return Mathf.Exp(-k * k);
    });

    public static Sprite Rays => rays != null ? rays : rays = Make("BossFx_Rays", 256, 256, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        float d = Mathf.Sqrt(u * u + v * v);
        if (d > 1f) return 0f;
        float ang = Mathf.Atan2(v, u);
        // 굵기가 조금씩 다른 빛살 18개
        float spoke = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 9f)), 18f) + 0.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 9f + 0.9f)), 30f);
        float fall = Mathf.Clamp01(1f - d);
        return Mathf.Clamp01(spoke * fall * 1.4f + Mathf.Pow(Mathf.Clamp01(1f - d * 3f), 2f));
    });

    public static Sprite Beam => beam != null ? beam : beam = Make("BossFx_Beam", 64, 256, new Vector2(0.5f, 1f), (u, v) =>
    {
        float across = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u)), 1.6f);
        float down = Mathf.Clamp01((v + 1f) * 0.5f);            // 위 1 → 아래 0
        return across * Mathf.Pow(down, 0.8f);
    });

    public static Sprite UpFade => upFade != null ? upFade : upFade = Make("BossFx_UpFade", 16, 128, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        float up = Mathf.Clamp01((v + 1f) * 0.5f);              // 아래 0 → 위 1
        return Mathf.Pow(1f - up, 1.4f);
    });

    public static Sprite FadeLine => fadeLine != null ? fadeLine : fadeLine = Make("BossFx_FadeLine", 256, 4, new Vector2(0.5f, 0.5f), (u, v) =>
        Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u)), 1.2f));

    public static Sprite BandV => bandV != null ? bandV : bandV = Make("BossFx_BandV", 4, 128, new Vector2(0.5f, 0.5f), (u, v) =>
        Mathf.Clamp01((1f - Mathf.Abs(v)) * 2.2f));

    public static Sprite SoftRect => softRect != null ? softRect : softRect = Make("BossFx_SoftRect", 64, 32, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        float x = Mathf.Clamp01((1f - Mathf.Abs(u)) * 4f);
        float y = Mathf.Clamp01(1f - Mathf.Abs(v));
        return x * y * y;
    });

    public static Sprite Diamond => diamond != null ? diamond : diamond = Make("BossFx_Diamond", 64, 64, new Vector2(0.5f, 0.5f), (u, v) =>
        Mathf.Clamp01((0.92f - (Mathf.Abs(u) + Mathf.Abs(v))) * 24f));

    public static Sprite Ink => ink != null ? ink : ink = Make("BossFx_Ink", 256, 32, new Vector2(0.5f, 0.5f), (u, v) =>
    {
        // 왼쪽이 굵고 오른쪽으로 가늘어지는 붓줄, 가장자리는 살짝 거칠게
        float x01 = (u + 1f) * 0.5f;
        float thick = Mathf.Lerp(0.9f, 0.25f, x01) * (0.9f + 0.1f * Mathf.Sin(u * 40f));
        float edge = Mathf.Clamp01((thick - Mathf.Abs(v)) * 6f);
        float tail = Mathf.Clamp01((1f - x01) * 8f) * Mathf.Clamp01(x01 * 30f);
        return edge * tail * (0.75f + 0.25f * Mathf.PerlinNoise(u * 12f, v * 3f));
    });

    public static Sprite White => white != null ? white : white = Make("BossFx_White", 4, 4, new Vector2(0.5f, 0.5f), (u, v) => 1f);

    /// <summary>빛을 안 받는 스프라이트 머티리얼. 이펙트가 2D 조명에 어두워지지 않게 한다.</summary>
    public static Material Unlit
    {
        get
        {
            if (unlit != null) return unlit;
            unlit = Resources.Load<Material>(UnlitMaterialPath);
            if (unlit != null) return unlit;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            unlit = new Material(shader) { name = "BossFx_Unlit", hideFlags = HideFlags.DontSave };
            return unlit;
        }
    }

    private static Sprite Make(string name, int w, int h, Vector2 pivot, Pixel pixel)
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
        // 가로 1유닛 크기. 쓰는 쪽에서 localScale로 크기를 정한다.
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, w, 0, SpriteMeshType.FullRect);
        s.name = name;
        s.hideFlags = HideFlags.DontSave;
        return s;
    }
}
