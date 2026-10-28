using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이펙트용 임시 그림(돌 조각, 먼지, 나뭇잎)을 코드로 만들어 두고 같이 쓴다.
/// 나중에 진짜 그림이 오면 각 이펙트 컴포넌트의 스프라이트 칸에 넣으면 이 그림 대신 쓴다.
///
/// 파티클 머티리얼은 Resources/Presentation/M_VfxSprite (URP Sprite-Unlit-Default)을 복사해서 쓴다.
/// </summary>
public static class VfxResources
{
    private const string BaseMaterialPath = "Presentation/M_VfxSprite";

    private static Texture2D chunk;
    private static Texture2D dust;
    private static Texture2D leaf;
    private static Material baseMaterial;
    private static readonly Dictionary<Texture, Material> materials = new();

    public static Texture2D Chunk => chunk != null ? chunk : chunk = MakeChunk(24);
    public static Texture2D Dust => dust != null ? dust : dust = MakeDust(32);
    public static Texture2D Leaf => leaf != null ? leaf : leaf = MakeLeaf(32);

    /// <summary>이 그림을 쓰는 파티클 머티리얼. 그림마다 하나씩만 만든다.</summary>
    public static Material MaterialFor(Texture texture)
    {
        if (texture == null) texture = Texture2D.whiteTexture;
        if (materials.TryGetValue(texture, out Material m) && m != null) return m;

        if (baseMaterial == null) baseMaterial = Resources.Load<Material>(BaseMaterialPath);

        if (baseMaterial != null)
        {
            m = new Material(baseMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            m = new Material(shader);
        }

        m.name = "Vfx_" + texture.name;
        m.hideFlags = HideFlags.DontSave;
        m.mainTexture = texture;
        materials[texture] = m;
        return m;
    }

    /// <summary>스프라이트가 지정돼 있으면 그 텍스처를, 없으면 기본 그림을 쓴다.</summary>
    public static Texture TextureOr(Sprite sprite, Texture2D fallback)
        => sprite != null ? sprite.texture : fallback;

    // ───────────── 임시 그림 ─────────────

    // 모서리가 각진 돌 조각
    private static Texture2D MakeChunk(int size)
    {
        Texture2D t = NewTexture(size, "VfxChunk");
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        const int corners = 6;
        float[] radius = { 0.46f, 0.32f, 0.44f, 0.38f, 0.47f, 0.30f };

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
            float angle = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * corners;
            if (angle < 0f) angle += corners;
            int i = Mathf.FloorToInt(angle) % corners;
            float k = angle - Mathf.Floor(angle);
            float edge = Mathf.Lerp(radius[i], radius[(i + 1) % corners], k) * size;
            float a = Mathf.Clamp01(edge - p.magnitude + 0.5f);

            // 위쪽은 밝게, 아래쪽은 어둡게 해서 입체감을 준다.
            float shade = Mathf.Lerp(0.72f, 1f, (y / (float)size));
            t.SetPixel(x, y, new Color(shade, shade, shade, a));
        }

        t.Apply();
        return t;
    }

    // 가장자리가 부드러운 먼지 덩어리
    private static Texture2D MakeDust(int size)
    {
        Texture2D t = NewTexture(size, "VfxDust");
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude / (size * 0.5f);
            float a = Mathf.Clamp01(1f - d);
            a = a * a * (3f - 2f * a);
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }

        t.Apply();
        return t;
    }

    // 끝이 뾰족한 잎사귀 + 가운데 잎맥
    private static Texture2D MakeLeaf(int size)
    {
        Texture2D t = NewTexture(size, "VfxLeaf");
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float v = (y + 0.5f) / size;                 // 0(꼭지) ~ 1(끝)
            float u = (x + 0.5f - half) / half;          // -1 ~ 1
            float width = Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI) * 0.62f * (1f - 0.35f * v);
            float a = Mathf.Clamp01((width - Mathf.Abs(u)) * half + 0.5f);

            float vein = Mathf.Abs(u) < 0.07f && v > 0.08f && v < 0.9f ? 0.78f : 1f;
            float shade = Mathf.Lerp(0.85f, 1f, 1f - Mathf.Abs(u)) * vein;
            t.SetPixel(x, y, new Color(shade, shade, shade, a));
        }

        t.Apply();
        return t;
    }

    private static Texture2D NewTexture(int size, string name)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        return t;
    }
}
