using UnityEngine;

/// <summary>
/// 보스방 돌 연출 (그림: Resources/Presentation/Boss/BossRock_1~10, BossCrack).
///
///   Debris   돌 파편이 튀어 올랐다가 떨어진다 (문이 쿵 떨어질 때, 2페이즈, 격파)
///   RockFall 천장에서 작은 돌이 우수수 떨어진다 (보스방 입장)
///   Crack    바닥에 푸른 빛이 새는 균열이 양옆으로 쫙 퍼졌다가 서서히 사라진다 (2페이즈, 격파)
///
/// 그림이 없으면 아무것도 하지 않는다.
/// </summary>
public static class BossStageFx
{
    private const string Folder = "Presentation/Boss/";
    private static Sprite[] rocks;
    private static Sprite[] fallRocks;   // 천장에서 떨어지는 이끼 낀 돌 (BossFallRock_1~9)
    private static Sprite crack;
    private static bool loaded;

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        System.Collections.Generic.List<Sprite> list = new System.Collections.Generic.List<Sprite>();
        for (int i = 1; i <= 10; i++)
        {
            Sprite s = Resources.Load<Sprite>(Folder + "BossRock_" + i);
            if (s != null) list.Add(s);
        }
        rocks = list.ToArray();
        System.Collections.Generic.List<Sprite> fall = new System.Collections.Generic.List<Sprite>();
        for (int i = 1; i <= 9; i++)
        {
            Sprite s = Resources.Load<Sprite>(Folder + "BossFallRock_" + i);
            if (s != null) fall.Add(s);
        }
        fallRocks = fall.Count > 0 ? fall.ToArray() : rocks;
        crack = Resources.Load<Sprite>(Folder + "BossCrack");
    }

    /// <summary>이끼 낀 돌 그림 하나 (떨어지는 돌)</summary>
    public static Sprite RandomFallRock()
    {
        Load();
        return fallRocks.Length > 0 ? fallRocks[Random.Range(0, fallRocks.Length)] : null;
    }

    private static BossFxSprite Rock(Vector3 pos, float size, string layer, int order, Sprite[] from = null)
    {
        Sprite[] set = from != null && from.Length > 0 ? from : rocks;
        Sprite s = set[Random.Range(0, set.Length)];
        BossFxSprite fx = BossFxSprite.Create("BossRock", s, pos, layer, order);
        float k = size / Mathf.Max(s.bounds.size.x, s.bounds.size.y);
        fx.scaleFrom = fx.scaleTo = new Vector3(Random.value < 0.5f ? -k : k, k, 1f);
        fx.spin = Random.Range(-360f, 360f);
        fx.alphaFrom = 1f;
        fx.alphaTo = 0f;
        fx.color = new Color(0.85f, 0.88f, 0.95f, 1f);   // 방 조명에 맞게 살짝 어둡게
        return fx;
    }

    /// <summary>origin에서 돌 파편 count개가 위·옆으로 튀었다가 떨어진다.</summary>
    public static void Debris(Vector3 origin, int count, float spreadX = 5f, float up = 6f, bool unscaled = false)
    {
        Load();
        if (rocks.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            BossFxSprite fx = Rock(origin + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.4f), 0f),
                Random.Range(0.18f, 0.55f), BossFx.Layer, 22);
            fx.velocity = new Vector2(Random.Range(-spreadX, spreadX), Random.Range(up * 0.5f, up));
            fx.gravity = 22f;
            fx.life = Random.Range(0.8f, 1.2f);
            fx.unscaled = unscaled;
        }
    }

    /// <summary>방 천장에서 작은 돌이 seconds 동안 우수수 떨어진다.</summary>
    public static void RockFall(Bounds room, int count, float seconds)
    {
        Load();
        if (rocks.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = new Vector3(Random.Range(room.min.x + 1f, room.max.x - 1f), room.max.y + 1f, 0f);
            BossFxSprite fx = Rock(p, Random.Range(0.2f, 0.6f), BossFx.Layer, 21, fallRocks);
            fx.spin = Random.Range(-200f, 200f);
            fx.delay = Random.Range(0f, seconds);
            fx.velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-2f, 0f));
            fx.gravity = 18f;
            float fall = room.size.y + 1f;
            fx.life = Mathf.Sqrt(2f * fall / 18f) + 0.1f;   // 바닥쯤에서 사라짐
            fx.alphaFrom = 1f;
            fx.alphaTo = 0.6f;
            fx.unscaled = true;
        }
    }

    /// <summary>2페이즈에 골렘이 둘로 갈라지는 순간: 몸 가운데로 세로 빛줄기가 쫙 갈라지고, 빛살과 파편이 양옆으로 터진다.</summary>
    public static void SplitBurst(Vector3 center, float groundY, float height)
    {
        Load();
        Color crackBlue = new Color(0.6f, 0.85f, 1f, 1f);
        // 세로로 갈라지는 빛 (가운데가 진하고 위아래가 흐린 선을 세로로 세움)
        BossFxSprite line = BossFxSprite.Create("BossSplitLine", BossFxArt.FadeLine, center, BossFx.Layer, 45);
        line.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        float k = Mathf.Max(3f, height) / BossFxArt.FadeLine.bounds.size.x;
        line.scaleFrom = new Vector3(k * 0.3f, 8f, 1f);
        line.scaleTo = new Vector3(k * 1.15f, 40f, 1f);
        line.color = crackBlue;
        line.life = 0.7f;
        line.alphaFrom = 1f;
        line.alphaTo = 0f;
        line.unscaled = true;

        // 갈라진 틈에서 퍼지는 빛살
        BossFxSprite rays = BossFxSprite.Create("BossSplitRays", BossFxArt.Rays, center, BossFx.Layer, 44);
        rays.color = new Color(crackBlue.r, crackBlue.g, crackBlue.b, 0.8f);
        rays.scaleFrom = new Vector3(2f, 2f, 1f);
        rays.scaleTo = new Vector3(11f, 11f, 1f);
        rays.spin = 25f;
        rays.life = 0.9f;
        rays.unscaled = true;

        // 양옆으로 터지는 파편 + 바닥 균열
        if (rocks.Length > 0)
        {
            Debris(center + Vector3.left * 0.5f, 8, 7f, 7f, true);
            Debris(center + Vector3.right * 0.5f, 8, 7f, 7f, true);
        }
        Crack(center, groundY, 10f, 2.5f, true);
        CameraShake.Shake(CameraShake.Heavy);
    }

    /// <summary>바닥(groundY)에 폭 width의 푸른 균열이 퍼졌다가 life초 동안 사라진다.</summary>
    // 균열은 땅 타일 위, 보스 아래에 그린다 (보스는 Ground 레이어 100번대로 옮겨져 있음 — BossRoom)
    private const string CrackLayer = "Ground";
    private const int CrackOrder = 60;

    public static void Crack(Vector3 center, float groundY, float width, float life = 3f, bool unscaled = false)
    {
        Load();
        if (crack == null) return;
        float k = width / crack.bounds.size.x;
        // 그림 윗부분이 바닥 윗면에 오도록 (균열은 바닥 안쪽으로 뻗음)
        Vector3 pos = new Vector3(center.x, groundY - crack.bounds.size.y * k * 0.3f, 0f);

        // 0.35초 동안 양옆으로 쫙 퍼지고
        BossFxSprite spread = BossFxSprite.Create("BossCrack", crack, pos, CrackLayer, CrackOrder);
        spread.scaleFrom = new Vector3(k * 0.15f, k * 0.8f, 1f);
        spread.scaleTo = new Vector3(k, k, 1f);
        spread.life = 0.35f;
        spread.alphaFrom = 1f;
        spread.alphaTo = 1f;
        spread.unscaled = unscaled;

        // 그대로 남았다가 서서히 사라진다
        BossFxSprite hold = BossFxSprite.Create("BossCrackHold", crack, pos, CrackLayer, CrackOrder);
        hold.scaleFrom = hold.scaleTo = new Vector3(k, k, 1f);
        hold.delay = 0.33f;
        hold.life = life;
        hold.alphaFrom = 1f;
        hold.alphaTo = 0f;
        hold.unscaled = unscaled;

        // 처음 순간 더 밝게 번쩍
        BossFxSprite flash = BossFxSprite.Create("BossCrackFlash", crack, pos, CrackLayer, CrackOrder + 1);
        flash.color = new Color(0.7f, 0.85f, 1f, 1f);
        flash.scaleFrom = new Vector3(k * 0.4f, k, 1f);
        flash.scaleTo = new Vector3(k * 1.05f, k * 1.1f, 1f);
        flash.life = 0.5f;
        flash.alphaFrom = 0.9f;
        flash.alphaTo = 0f;
        flash.unscaled = unscaled;
    }
}
