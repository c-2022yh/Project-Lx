using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이펙트용 임시 그림(돌 조각, 먼지, 나뭇잎)을 코드로 그려 두고 같이 쓴다.
///
/// 캐릭터 일러스트에 맞춰 남색 가는 선 + 두 단계 셀 음영(밝은 면 / 푸른 그림자)으로 그린다.
/// 그림 자체는 거의 흰색이고, 실제 색(쿨톤)은 파티클 색으로 곱해서 입힌다.
/// 그림마다 모양이 4가지(2x2 시트)라서 파티클마다 하나씩 무작위로 고른다(ApplySheet).
///
/// 나중에 진짜 그림이 오면 각 이펙트 컴포넌트의 스프라이트 칸에 넣으면 이 그림 대신 쓴다.
/// 파티클 머티리얼은 Resources/Presentation/M_VfxSprite (URP Sprite-Unlit-Default)을 복사해서 쓴다.
/// </summary>
public static class VfxResources
{
    private const string BaseMaterialPath = "Presentation/M_VfxSprite";

    /// <summary>임시 그림 시트의 가로·세로 칸 수.</summary>
    public const int SheetTiles = 2;

    private const int TileSize = 64;
    private const int Supersample = 3;

    // 선과 음영 색 (캐릭터 선화의 남색 계열)
    private static readonly Color Ink = new Color(0.16f, 0.19f, 0.29f, 1f);
    private static readonly Color LightFace = new Color(1f, 1f, 1f, 1f);
    private static readonly Color ShadowFace = new Color(0.74f, 0.78f, 0.9f, 1f);
    private static readonly Color Crease = new Color(0.46f, 0.52f, 0.66f, 1f);
    private static readonly Color LeafShadow = new Color(0.8f, 0.86f, 0.96f, 1f);
    private static readonly Color Vein = new Color(0.68f, 0.75f, 0.88f, 1f);

    private const float LineWidth = 0.035f;    // 칸 크기 대비 선 두께 (64px 기준 약 2px)
    private const float CreaseWidth = 0.018f;

    private static Texture2D chunk;
    private static Texture2D dust;
    private static Texture2D leaf;
    private static Material baseMaterial;
    private static readonly Dictionary<Texture, Material> materials = new();

    public static Texture2D Chunk => chunk != null ? chunk : chunk = BuildSheet("VfxChunk", ShardPixel);
    public static Texture2D Dust => dust != null ? dust : dust = BuildSheet("VfxDust", DustPixel);
    public static Texture2D Leaf => leaf != null ? leaf : leaf = BuildSheet("VfxLeaf", LeafPixel);

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

    /// <summary>
    /// 임시 그림(2x2 시트)이면 파티클마다 4가지 모양 중 하나를 무작위로 쓰게 한다.
    /// 직접 넣은 스프라이트면 아무것도 안 한다.
    /// </summary>
    public static void ApplySheet(ParticleSystem ps, Texture texture)
    {
        if (ps == null) return;
        bool isSheet = texture != null && (texture == chunk || texture == dust || texture == leaf);

        ParticleSystem.TextureSheetAnimationModule sheet = ps.textureSheetAnimation;
        sheet.enabled = isSheet;
        if (!isSheet) return;

        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = SheetTiles;
        sheet.numTilesY = SheetTiles;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        // 두 상수 사이 무작위 = 파티클마다 고정된 칸 하나
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        sheet.cycleCount = 1;
    }

    // ───────────── 시트 그리기 ─────────────

    private delegate Color TilePixel(int variant, float x, float y);

    // 칸마다 3x3 표본을 평균내서 선이 매끄럽게 나오게 한다. 밉맵도 만들어서 작게 보여도 깨지지 않게.
    private static Texture2D BuildSheet(string name, TilePixel pixel)
    {
        int size = TileSize * SheetTiles;
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            hideFlags = HideFlags.DontSave
        };

        Color[] pixels = new Color[size * size];
        const int n = Supersample * Supersample;

        for (int ty = 0; ty < SheetTiles; ty++)
        for (int tx = 0; tx < SheetTiles; tx++)
        {
            int variant = ty * SheetTiles + tx;
            for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                float pr = 0f, pg = 0f, pb = 0f, sa = 0f, r = 0f, g = 0f, b = 0f;
                for (int j = 0; j < Supersample; j++)
                for (int i = 0; i < Supersample; i++)
                {
                    float u = (x + (i + 0.5f) / Supersample) / TileSize;
                    float v = (y + (j + 0.5f) / Supersample) / TileSize;
                    Color c = pixel(variant, u, v);
                    pr += c.r * c.a; pg += c.g * c.a; pb += c.b * c.a; sa += c.a;
                    r += c.r; g += c.g; b += c.b;
                }

                Color o = sa > 0.0001f
                    ? new Color(pr / sa, pg / sa, pb / sa, sa / n)
                    : new Color(r / n, g / n, b / n, 0f);
                pixels[(ty * TileSize + y) * size + tx * TileSize + x] = o;
            }
        }

        t.SetPixels(pixels);
        t.Apply(true);
        return t;
    }

    private static Color Transparent(Color rgb) => new Color(rgb.r, rgb.g, rgb.b, 0f);

    // ───────────── 돌 조각: 각진 파편, 대각선 주름으로 밝은 면/그림자 면 ─────────────

    private static readonly Vector2[][] Shards =
    {
        new[] { new Vector2(0.18f, 0.30f), new Vector2(0.42f, 0.88f), new Vector2(0.80f, 0.70f), new Vector2(0.86f, 0.28f), new Vector2(0.50f, 0.12f) },
        new[] { new Vector2(0.12f, 0.55f), new Vector2(0.36f, 0.86f), new Vector2(0.74f, 0.80f), new Vector2(0.90f, 0.42f), new Vector2(0.62f, 0.14f), new Vector2(0.26f, 0.20f) },
        new[] { new Vector2(0.22f, 0.18f), new Vector2(0.14f, 0.62f), new Vector2(0.52f, 0.90f), new Vector2(0.70f, 0.62f), new Vector2(0.88f, 0.22f) },
        new[] { new Vector2(0.30f, 0.12f), new Vector2(0.12f, 0.40f), new Vector2(0.40f, 0.84f), new Vector2(0.84f, 0.66f), new Vector2(0.74f, 0.22f) },
    };

    // 주름(면이 꺾이는 선): 각 조각의 꼭짓점 번호 두 개
    private static readonly Vector2Int[] ShardCreases = { new(1, 4), new(1, 4), new(2, 4), new(2, 4) };

    private static Color ShardPixel(int variant, float x, float y)
    {
        Vector2[] poly = Shards[variant];
        Vector2 p = new Vector2(x, y);
        bool inside = InsidePolygon(poly, p);
        float edge = DistanceToEdges(poly, p);

        if (!inside && edge > LineWidth * 0.5f) return Transparent(Ink);
        if (edge < LineWidth * 0.5f) return Ink;

        Vector2 a = poly[ShardCreases[variant].x];
        Vector2 b = poly[ShardCreases[variant].y];
        float lightSide = Cross(b - a, new Vector2(0f, 1f) - a);   // 왼쪽 위를 밝은 면으로
        bool light = Cross(b - a, p - a) * lightSide > 0f;

        Color c = light ? LightFace : ShadowFace;
        c *= Mathf.Lerp(0.9f, 1.04f, y);                           // 위쪽이 조금 더 밝게
        if (light && edge < LineWidth * 1.6f) c = Color.Lerp(c, Color.white, 0.6f);   // 밝은 면 테두리 하이라이트

        if (DistanceToSegment(p, a, b) < CreaseWidth * 0.5f) c = Color.Lerp(c, Crease, 0.85f);

        c.a = 1f;
        return c;
    }

    // ───────────── 나뭇잎: 4가지 잎 모양, 가운데 잎맥 + 곁맥, 반쪽 그림자 ─────────────

    private const float LeafBase = 0.08f;
    private const float LeafTip = 0.94f;

    private static float LeafMid(int variant, float v)
    {
        float bend = variant == 2 ? 0.07f : variant == 3 ? -0.04f : 0f;   // 살짝 휜 잎
        return 0.5f + bend * Mathf.Sin(Mathf.PI * v);
    }

    private static float LeafHalfWidth(int variant, float v)
    {
        if (v <= 0f || v >= 1f) return 0f;
        float s = Mathf.Sin(Mathf.PI * v);
        switch (variant)
        {
            case 0: return 0.13f * Mathf.Pow(s, 0.85f) * (1f - 0.25f * v);                      // 버들잎
            case 1: return 0.27f * Mathf.Pow(s, 0.7f) * (1f - 0.3f * v);                        // 둥근 잎
            case 2: return 0.2f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(v, 0.85f)), 0.8f);   // 휜 창 모양
            default: return 0.3f * Mathf.Pow(s, 0.6f) * (1f - 0.4f * v);                        // 꽃잎 같은 잎
        }
    }

    private static Color LeafPixel(int variant, float x, float y)
    {
        float len = LeafTip - LeafBase;
        float v = (y - LeafBase) / len;

        // 꼭지
        if (v < 0f)
        {
            bool stem = Mathf.Abs(x - 0.5f) < LineWidth * 0.45f && y > 0.02f;
            return stem ? Ink : Transparent(Ink);
        }
        if (v > 1f) return Transparent(Ink);

        float mid = LeafMid(variant, v);
        float w = LeafHalfWidth(variant, v);
        float du = x - mid;

        // 가로 거리를 테두리에 수직인 거리로 바꿔서 끝부분 선이 굵어지지 않게
        const float e = 0.01f;
        float slope = (LeafHalfWidth(variant, Mathf.Min(1f, v + e)) - LeafHalfWidth(variant, Mathf.Max(0f, v - e))) / (2f * e * len);
        float d = (w - Mathf.Abs(du)) / Mathf.Sqrt(1f + slope * slope);

        if (d < -LineWidth * 0.5f) return Transparent(Ink);
        if (d < LineWidth * 0.5f) return Ink;

        Color c = du < 0f ? LightFace : LeafShadow;
        c *= Mathf.Lerp(0.94f, 1.04f, v);

        // 가운데 잎맥
        if (Mathf.Abs(du) < 0.012f * (1f - 0.6f * v) && v < 0.93f) c *= Vein;

        // 곁맥 (위로 비스듬히)
        float a = Mathf.Abs(du);
        if (a > 0.015f && a < w * 0.8f)
        {
            for (int k = 0; k < 3; k++)
            {
                float start = 0.22f + 0.2f * k;
                if (Mathf.Abs((v - start) * len - 0.75f * a) < 0.008f)
                {
                    c *= Color.Lerp(Vein, Color.white, 0.4f);
                    break;
                }
            }
        }

        c.a = 1f;
        return c;
    }

    // ───────────── 먼지: 몽글몽글한 먹 번짐 구름 ─────────────

    private static readonly Vector3[][] DustBlobs =
    {
        new[] { new Vector3(0.50f, 0.50f, 0.34f), new Vector3(0.36f, 0.42f, 0.22f), new Vector3(0.64f, 0.58f, 0.20f), new Vector3(0.50f, 0.66f, 0.18f) },
        new[] { new Vector3(0.45f, 0.48f, 0.30f), new Vector3(0.62f, 0.40f, 0.24f), new Vector3(0.32f, 0.60f, 0.20f), new Vector3(0.58f, 0.66f, 0.16f), new Vector3(0.70f, 0.55f, 0.14f) },
        new[] { new Vector3(0.50f, 0.45f, 0.28f), new Vector3(0.35f, 0.55f, 0.25f), new Vector3(0.66f, 0.55f, 0.22f) },
        new[] { new Vector3(0.48f, 0.52f, 0.36f), new Vector3(0.30f, 0.36f, 0.16f), new Vector3(0.70f, 0.68f, 0.16f) },
    };

    private static readonly Color DustTint = new Color(0.95f, 0.97f, 1f, 1f);

    private static Color DustPixel(int variant, float x, float y)
    {
        float keep = 1f;
        foreach (Vector3 blob in DustBlobs[variant])
        {
            Vector2 d = new Vector2(x - blob.x, y - blob.y);
            float angle = Mathf.Atan2(d.y, d.x);
            float wobble = 1f + 0.08f * Mathf.Sin(6f * angle + blob.x * 20f);   // 가장자리 일렁임
            float k = Mathf.Clamp01(1f - d.magnitude * wobble / blob.z);
            k = k * k * (3f - 2f * k);
            keep *= 1f - Mathf.Pow(k, 1.4f) * 0.75f;
        }

        Color c = DustTint;
        c.a = Mathf.Clamp01(1f - keep);
        return c;
    }

    // ───────────── 도형 계산 ─────────────

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static bool InsidePolygon(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    private static float DistanceToEdges(Vector2[] poly, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            best = Mathf.Min(best, DistanceToSegment(p, poly[j], poly[i]));
        return best;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return (p - (a + ab * t)).magnitude;
    }
}
