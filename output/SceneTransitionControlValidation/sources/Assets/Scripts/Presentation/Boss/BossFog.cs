using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스방 안개 (구름 그림 4장: Resources/Presentation/Boss/BossFog_1~4).
///
///   Create   보스방 바닥에 낮게 깔려 천천히 흐르는 안개. 앞(캐릭터 앞)과 뒤(땅 뒤) 두 겹.
///   Gather   보스가 나오기 전, 보스 자리로 안개가 모여 짙어진다.
///   Burst    보스가 나오는 순간, 모인 안개가 사방으로 확 흩어진다.
///   DoorDust 돌문이 쿵 떨어질 때 문 밑에서 먼지구름이 퍼진다.
///   Clear    보스를 잡으면 안개가 걷힌다.
///
/// 모두 실제 시간으로 움직인다 (히트스탑·슬로모션 중에도).
/// </summary>
public class BossFog : MonoBehaviour
{
    private const string Folder = "Presentation/Boss/BossFog_";
    private static Sprite[] clouds;

    public static readonly Color FogColor = new Color(0.78f, 0.85f, 1f, 1f);

    private class Cloud
    {
        public SpriteRenderer sr;
        public float speed, baseAlpha, phase, minX, maxX;
    }

    private readonly List<Cloud> layer = new List<Cloud>();
    private float density, densityFrom, densityTo, densityTime, densityDuration;
    private bool destroyWhenClear;
    private Color tint = FogColor;

    // ── 그림 ───────────────────────────────

    public static Sprite[] Clouds
    {
        get
        {
            if (clouds != null) return clouds;
            List<Sprite> list = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                Sprite s = Resources.Load<Sprite>(Folder + i);
                if (s != null) list.Add(s);
            }
            clouds = list.ToArray();
            return clouds;
        }
    }

    private static Sprite Pick(int i)
    {
        Sprite[] c = Clouds;
        return c.Length == 0 ? BossFxArt.SoftDot : c[Mathf.Abs(i) % c.Length];
    }

    // ── 바닥 안개 ───────────────────────────

    /// <summary>room 바닥에 깔리는 안개를 만든다 (처음엔 투명, SetDensity로 짙게).</summary>
    public static BossFog Create(Bounds room)
    {
        GameObject go = new GameObject("BossFog");
        BossFog fog = go.AddComponent<BossFog>();
        fog.Build(room);
        return fog;
    }

    private void Build(Bounds room)
    {
        float floor = room.min.y;
        float margin = 6f;
        float minX = room.min.x - margin, maxX = room.max.x + margin;
        int backCount = Mathf.Clamp(Mathf.RoundToInt(room.size.x / 3.5f), 5, 14);
        int frontCount = Mathf.Clamp(Mathf.RoundToInt(room.size.x / 6f), 3, 8);

        for (int i = 0; i < backCount; i++)   // 땅 뒤: 넓고 진하게
            Add(i, Mathf.Lerp(minX, maxX, (i + Random.value * 0.6f) / backCount), floor + Random.Range(0.3f, 1.6f),
                Random.Range(6f, 10f), Random.Range(0.35f, 0.55f), "Default", 40, minX, maxX);
        for (int i = 0; i < frontCount; i++)  // 캐릭터 앞: 옅게, 발목 높이
            Add(i + 7, Mathf.Lerp(minX, maxX, (i + Random.value * 0.6f) / frontCount), floor + Random.Range(-0.6f, 0.3f),
                Random.Range(7f, 11f), Random.Range(0.18f, 0.28f), "GroundCover", 6, minX, maxX);
    }

    private void Add(int index, float x, float y, float width, float alpha, string sortingLayer, int order, float minX, float maxX)
    {
        Sprite s = Pick(index);
        GameObject go = new GameObject("Cloud");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, y, 0f);
        float k = width / s.bounds.size.x;
        go.transform.localScale = new Vector3(Random.value < 0.5f ? -k : k, k * Random.Range(0.55f, 0.75f), 1f);   // 납작하게 깔림
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.color = new Color(tint.r, tint.g, tint.b, 0f);
        layer.Add(new Cloud
        {
            sr = sr,
            speed = Random.Range(0.25f, 0.6f) * (Random.value < 0.5f ? -1f : 1f),
            baseAlpha = alpha,
            phase = Random.value * 10f,
            minX = minX,
            maxX = maxX
        });
    }

    /// <summary>안개 짙기 (0 = 없음, 1 = 기본)를 seconds 동안 바꾼다.</summary>
    public void SetDensity(float target, float seconds)
    {
        densityFrom = density;
        densityTo = Mathf.Max(0f, target);
        densityTime = 0f;
        densityDuration = Mathf.Max(0.01f, seconds);
    }

    /// <summary>안개 색 (2페이즈에 붉게 등).</summary>
    public void SetTint(Color c) => tint = c;

    /// <summary>안개를 걷고 다 걷히면 지운다.</summary>
    public void Clear(float seconds)
    {
        SetDensity(0f, seconds);
        destroyWhenClear = true;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        densityTime += dt;
        density = Mathf.Lerp(densityFrom, densityTo, Mathf.SmoothStep(0f, 1f, densityTime / densityDuration));
        if (destroyWhenClear && densityTime >= densityDuration && density <= 0.001f)
        {
            Destroy(gameObject);
            return;
        }

        float time = Time.unscaledTime;
        foreach (Cloud c in layer)
        {
            Transform t = c.sr.transform;
            Vector3 p = t.position;
            p.x += c.speed * dt;
            if (p.x > c.maxX) p.x = c.minX;
            else if (p.x < c.minX) p.x = c.maxX;
            t.position = p;
            float breathe = 0.8f + 0.2f * Mathf.Sin(time * 0.6f + c.phase);   // 천천히 짙어졌다 옅어짐
            c.sr.color = new Color(tint.r, tint.g, tint.b, c.baseAlpha * breathe * density);
        }
    }

    // ── 한 번 터지는 안개 ───────────────────

    /// <summary>보스 자리로 안개가 모여 짙어진다 (seconds 동안). 다 모일 즈음 Burst로 흩어지게 부른다.</summary>
    public static void Gather(Vector3 center, float width, float seconds)
    {
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.45f);
            float dist = width * Random.Range(0.6f, 0.9f);
            Vector3 from = center + (Vector3)(dir * dist);
            BossFxSprite fx = Puff(i, from, width * Random.Range(0.45f, 0.65f), i % 3 == 0 ? "GroundCover" : "Default", 45);
            fx.velocity = -dir * dist / seconds;
            fx.life = seconds + 0.2f;   // 다 모이면 바로 Burst가 이어받는다
            fx.fadeIn = seconds * 0.6f;
            fx.alphaFrom = 0.85f;
            fx.alphaTo = 0.7f;
            fx.scaleTo = fx.scaleFrom * 0.85f;
        }
    }

    /// <summary>안개가 사방으로 확 흩어지며 사라진다 (보스 등장).</summary>
    public static void Burst(Vector3 center, float width)
    {
        for (int i = 0; i < 10; i++)
        {
            float ang = i / 10f * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.5f);
            BossFxSprite fx = Puff(i, center + (Vector3)(dir * width * 0.15f), width * Random.Range(0.5f, 0.75f),
                i % 3 == 0 ? "GroundCover" : "Default", 46);
            fx.velocity = dir * width * Random.Range(1.4f, 2.2f);
            fx.drag = 2.5f;
            fx.life = Random.Range(1.1f, 1.6f);
            fx.alphaFrom = 0.85f;
            fx.alphaTo = 0f;
            fx.scaleTo = fx.scaleFrom * 1.8f;
        }
    }

    /// <summary>돌문이 떨어진 자리에서 먼지구름이 양옆으로 퍼진다.</summary>
    public static void DoorDust(Vector3 ground, float width)
    {
        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            BossFxSprite fx = Puff(i, ground + new Vector3(side * width * 0.2f, Random.Range(0f, 0.4f), 0f),
                width * Random.Range(0.6f, 0.9f), "GroundCover", 7);
            fx.color = new Color(0.72f, 0.74f, 0.80f, 1f);   // 돌가루
            fx.velocity = new Vector2(side * Random.Range(2f, 4f), Random.Range(0.3f, 1f));
            fx.drag = 3f;
            fx.life = Random.Range(0.8f, 1.3f);
            fx.alphaFrom = 0.7f;
            fx.alphaTo = 0f;
            fx.scaleFrom = new Vector3(fx.scaleFrom.x, fx.scaleFrom.y * 0.5f, 1f);
            fx.scaleTo = fx.scaleFrom * 1.7f;
        }
    }

    /// <summary>보스가 맞은 자리에서 작은 돌가루가 맞은 반대쪽으로 퍼진다 (side: 맞은 쪽 방향).</summary>
    public static void HitDust(Vector3 at, float side)
    {
        for (int i = 0; i < 2; i++)
        {
            BossFxSprite fx = Puff(i, at, Random.Range(1.2f, 1.8f), "Effects", 18);
            fx.color = new Color(0.75f, 0.77f, 0.82f, 1f);
            fx.velocity = new Vector2(side * Random.Range(1f, 2.5f), Random.Range(0.2f, 1.2f));
            fx.drag = 3f;
            fx.life = Random.Range(0.45f, 0.7f);
            fx.alphaFrom = 0.55f;
            fx.alphaTo = 0f;
            fx.scaleTo = fx.scaleFrom * 1.6f;
        }
    }

    /// <summary>보스 공격이 땅에 닿은 자리: 폭 width를 따라 먼지구름이 낮게 퍼진다.</summary>
    public static void LandingDust(Vector3 ground, float width)
    {
        int n = Mathf.Clamp(Mathf.RoundToInt(width / 1.5f), 2, 8);
        for (int i = 0; i < n; i++)
        {
            float u = n == 1 ? 0.5f : (float)i / (n - 1);
            float side = u < 0.5f ? -1f : 1f;
            BossFxSprite fx = Puff(i, ground + new Vector3((u - 0.5f) * width, Random.Range(0f, 0.3f), 0f),
                Random.Range(2f, 3f), "GroundCover", 7);
            fx.color = new Color(0.7f, 0.72f, 0.78f, 1f);
            fx.velocity = new Vector2(side * Random.Range(1.5f, 3.5f), Random.Range(0.2f, 0.8f));
            fx.drag = 3f;
            fx.life = Random.Range(0.7f, 1.1f);
            fx.alphaFrom = 0.65f;
            fx.alphaTo = 0f;
            fx.scaleFrom = new Vector3(fx.scaleFrom.x, fx.scaleFrom.y * 0.5f, 1f);
            fx.scaleTo = fx.scaleFrom * 1.6f;
        }
    }

    private static BossFxSprite Puff(int i, Vector3 pos, float width, string sortingLayer, int order)
    {
        Sprite s = Pick(i + Random.Range(0, 4));
        BossFxSprite fx = BossFxSprite.Create("FogPuff", s, pos, sortingLayer, order);
        float k = width / s.bounds.size.x;
        fx.scaleFrom = new Vector3(Random.value < 0.5f ? -k : k, k * 0.7f, 1f);
        fx.scaleTo = fx.scaleFrom;
        fx.color = FogColor;
        fx.unscaled = true;
        return fx;
    }
}
