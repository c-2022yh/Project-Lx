using UnityEngine;

/// <summary>
/// 보스전 월드 이펙트 (달빛 방향). 그림 없이 코드 도형으로 만든다.
///
///   BossFx.Impact(center, size)          공격이 바닥을 칠 때: 고리 + 흙먼지 + 돌 조각 + 흔들림 (넓으면 충격파)
///   BossFx.CeilingDust(roomBounds, 2f)   보스방에 들어설 때 천장에서 떨어지는 돌가루
///   BossFx.PhaseTwoBurst(center, groundY) 2페이즈: 큰 충격파 고리 + 바닥 물결 + 포효 빛살
///   BossFx.DefeatBurst(center)           격파: 빛살 + 흩어지는 빛 조각 (슬로모션과 상관없이 흐름)
///   BossFx.MarkerSpark / SummonPop       소환 표시에서 오르는 불티 / 소환될 때 흙먼지
/// </summary>
public static class BossFx
{
    public const string Layer = "Effects";

    public static readonly Color Moon = new Color(0.86f, 0.92f, 1f, 1f);
    public static readonly Color Warm = new Color(1f, 0.62f, 0.40f, 1f);
    public static readonly Color DustColor = new Color(0.70f, 0.68f, 0.64f, 1f);
    public static readonly Color StoneColor = new Color(0.55f, 0.57f, 0.60f, 1f);

    // ───────────── 공격 ─────────────

    public static void Impact(Vector2 center, Vector2 size)
    {
        float groundY = center.y - size.y * 0.5f;
        Vector3 ground = new Vector3(center.x, groundY, 0f);
        bool wide = size.x >= 6f;
        bool column = size.y > size.x * 1.5f;   // 낙석
        float w = Mathf.Max(1f, size.x);

        // 바닥을 따라 퍼지는 납작한 고리
        BossFxSprite ring = BossFxSprite.Create("Boss Impact Ring", BossFxArt.Ring, ground, Layer, 31);
        ring.color = new Color(Moon.r, Moon.g, Moon.b, 0.85f);
        ring.scaleFrom = new Vector3(w * 0.3f, w * 0.06f, 1f);
        ring.scaleTo = new Vector3(w * 1.25f, w * 0.2f, 1f);
        ring.life = 0.55f;

        // 바닥 번쩍
        BossFxSprite flash = BossFxSprite.Create("Boss Impact Flash", BossFxArt.SoftDot, ground, Layer, 30);
        flash.color = new Color(Warm.r, Warm.g, Warm.b, 0.7f);
        flash.scaleFrom = new Vector3(w * 1.1f, 0.9f, 1f);
        flash.scaleTo = new Vector3(w * 1.3f, 0.3f, 1f);
        flash.life = 0.35f;

        // 흙먼지
        int puffs = wide ? 8 : 4;
        for (int i = 0; i < puffs; i++)
        {
            float x = center.x + Random.Range(-0.5f, 0.5f) * size.x;
            BossFxSprite d = BossFxSprite.Create("Boss Impact Dust", BossFxArt.SoftDot, new Vector3(x, groundY + 0.25f, 0f), Layer, 32);
            d.color = new Color(DustColor.r, DustColor.g, DustColor.b, 0.55f);
            float s = Random.Range(0.9f, 1.4f) * (wide ? 1.3f : 1f);
            d.scaleFrom = new Vector3(s, s * 0.6f, 1f);
            d.scaleTo = new Vector3(s * 2.6f, s * 1.6f, 1f);
            d.velocity = new Vector2(Mathf.Sign(x - center.x) * Random.Range(0.4f, 1.4f), Random.Range(0.4f, 1.1f));
            d.drag = 1.5f;
            d.life = Random.Range(0.8f, 1.1f);
        }

        // 양옆으로 달려 나가는 충격파 (짓누르기)
        if (wide)
        {
            for (int dir = -1; dir <= 1; dir += 2)
            {
                BossFxSprite wave = BossFxSprite.Create("Boss Shock Wave", BossFxArt.SoftDot, ground + Vector3.up * 0.2f, Layer, 33);
                wave.color = new Color(Moon.r, Moon.g, Moon.b, 0.8f);
                wave.scaleFrom = new Vector3(2.4f, 0.6f, 1f);
                wave.scaleTo = new Vector3(3.6f, 0.8f, 1f);
                wave.velocity = new Vector2(dir * size.x * 0.5f / 0.6f, 0f);
                wave.life = 0.6f;
            }
        }

        // 돌 조각
        DebrisBurst.Options o = DebrisBurst.Options.Default(StoneColor);
        o.sortingLayerId = SortingLayer.NameToID(Layer);
        o.sortingOrder = 29;
        o.strength = wide ? 1.1f : 0.8f;
        o.shake = false;
        DebrisBurst.Spawn(new Bounds(ground + Vector3.up * 0.4f, new Vector3(Mathf.Min(size.x, 4f), 0.8f, 0.1f)), o);

        CameraShake.Shake(wide ? CameraShake.Heavy : column ? CameraShake.Light : CameraShake.Medium);
    }

    public static void MarkerSpark(Vector2 groundCenter, float width, Color color)
    {
        Vector3 p = new Vector3(groundCenter.x + Random.Range(-0.5f, 0.5f) * width, groundCenter.y + 0.05f, 0f);
        BossFxSprite s = BossFxSprite.Create("Boss Marker Spark", BossFxArt.SoftDot, p, Layer, 34);
        s.color = color;
        float k = Random.Range(0.08f, 0.14f);
        s.scaleFrom = s.scaleTo = new Vector3(k, k, 1f);
        s.velocity = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(1.2f, 2.2f));
        s.life = Random.Range(0.6f, 1f);
        s.fadeIn = 0.15f;
    }

    public static void SummonPop(Vector2 groundCenter, float width)
    {
        Vector3 g = new Vector3(groundCenter.x, groundCenter.y, 0f);
        BossFxSprite ring = BossFxSprite.Create("Boss Summon Ring", BossFxArt.Ring, g, Layer, 31);
        ring.color = new Color(Warm.r, Warm.g, Warm.b, 0.8f);
        ring.scaleFrom = new Vector3(width * 0.5f, width * 0.12f, 1f);
        ring.scaleTo = new Vector3(width * 2f, width * 0.4f, 1f);
        ring.life = 0.5f;
        for (int i = 0; i < 3; i++)
        {
            BossFxSprite d = BossFxSprite.Create("Boss Summon Dust", BossFxArt.SoftDot, g + new Vector3(Random.Range(-0.4f, 0.4f) * width, 0.3f, 0f), Layer, 32);
            d.color = new Color(DustColor.r, DustColor.g, DustColor.b, 0.5f);
            d.scaleFrom = new Vector3(1f, 0.6f, 1f);
            d.scaleTo = new Vector3(2.4f, 1.8f, 1f);
            d.velocity = new Vector2(0f, 0.8f);
            d.drag = 1.5f;
            d.life = 0.8f;
        }
    }

    // ───────────── 등장 ─────────────

    public static void CeilingDust(Bounds room, float seconds)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(room.size.x * 2.5f), 12, 60);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = new Vector3(Random.Range(room.min.x, room.max.x), room.max.y + Random.Range(0f, 0.5f), 0f);
            BossFxSprite d = BossFxSprite.Create("Boss Ceiling Dust", BossFxArt.SoftDot, p, Layer, 28);
            d.color = new Color(0.82f, 0.82f, 0.80f, 0.8f);
            float k = Random.Range(0.06f, 0.13f);
            d.scaleFrom = d.scaleTo = new Vector3(k, k, 1f);
            d.velocity = new Vector2(Random.Range(-0.15f, 0.15f), -Random.Range(2.5f, 4.5f));
            d.delay = Random.Range(0f, seconds);
            d.life = Mathf.Max(0.8f, room.size.y / 3.5f);
            d.alphaFrom = 1f;
            d.alphaTo = 0f;
            d.fadeIn = 0.2f;
        }
        // 굵은 돌가루 덩이 몇 개
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = new Vector3(Random.Range(room.min.x, room.max.x), room.max.y, 0f);
            BossFxSprite d = BossFxSprite.Create("Boss Ceiling Puff", BossFxArt.SoftDot, p, Layer, 27);
            d.color = new Color(DustColor.r, DustColor.g, DustColor.b, 0.35f);
            d.scaleFrom = new Vector3(0.8f, 0.5f, 1f);
            d.scaleTo = new Vector3(1.8f, 2.6f, 1f);
            d.velocity = new Vector2(0f, -1.6f);
            d.delay = Random.Range(0f, seconds * 0.6f);
            d.life = 1.4f;
        }
    }

    // ───────────── 2페이즈 ─────────────

    public static void PhaseTwoBurst(Vector3 center, float groundY)
    {
        // 붉은 불꽃 고리 그림(Resources/Presentation/Boss/BossPhaseRing)이 있으면: 고리가 확 커지며 퍼지고, 안쪽에 하나가 일렁이며 남았다 사라짐
        Sprite ringArt = Resources.Load<Sprite>("Presentation/Boss/BossPhaseRing");
        if (ringArt != null)
        {
            float u = 1f / ringArt.bounds.size.x;   // 폭 1칸 기준
            BossFxSprite burst = BossFxSprite.Create("Boss Phase Fire Ring", ringArt, center, Layer, 36);
            burst.scaleFrom = new Vector3(u * 2f, u * 2f, 1f);
            burst.scaleTo = new Vector3(u * 16f, u * 16f, 1f);
            burst.spin = 30f;
            burst.life = 1.1f;
            burst.alphaFrom = 1f;
            burst.alphaTo = 0f;

            BossFxSprite hold = BossFxSprite.Create("Boss Phase Fire Core", ringArt, center, Layer, 37);
            hold.scaleFrom = new Vector3(u * 1.5f, u * 1.5f, 1f);
            hold.scaleTo = new Vector3(u * 6.5f, u * 6.5f, 1f);
            hold.spin = -40f;
            hold.fadeIn = 0.12f;
            hold.life = 1.6f;
            hold.alphaFrom = 1f;
            hold.alphaTo = 0f;

            BossFxSprite late = BossFxSprite.Create("Boss Phase Fire Ring 2", ringArt, center, Layer, 35);
            late.color = new Color(1f, 0.6f, 0.45f, 0.8f);
            late.delay = 0.2f;
            late.scaleFrom = new Vector3(u * 3f, u * 3f, 1f);
            late.scaleTo = new Vector3(u * 20f, u * 20f, 1f);
            late.spin = 15f;
            late.life = 1.2f;
            late.alphaFrom = 0.8f;
            late.alphaTo = 0f;
        }
        else
        for (int i = 0; i < 2; i++)
        {
            BossFxSprite ring = BossFxSprite.Create("Boss Phase Ring", BossFxArt.Ring, center, Layer, 35);
            ring.color = new Color(Warm.r, Warm.g, Warm.b, 0.9f);
            ring.scaleFrom = new Vector3(1.5f, 1.5f, 1f);
            ring.scaleTo = new Vector3(18f, 18f, 1f);
            ring.delay = i * 0.18f;
            ring.life = 0.9f;
        }

        // 바닥을 따라 퍼지는 물결 (여울)
        Vector3 g = new Vector3(center.x, groundY, 0f);
        for (int i = 0; i < 2; i++)
        {
            BossFxSprite ripple = BossFxSprite.Create("Boss Phase Ripple", BossFxArt.Ring, g, Layer, 30);
            ripple.color = new Color(1f, 0.78f, 0.62f, 0.75f);
            ripple.scaleFrom = new Vector3(2f, 0.35f, 1f);
            ripple.scaleTo = new Vector3(22f, 3f, 1f);
            ripple.delay = 0.1f + i * 0.3f;
            ripple.life = 1.2f;
        }

        // 포효 빛살
        BossFxSprite roar = BossFxSprite.Create("Boss Roar Rays", BossFxArt.Rays, center, Layer, 26);
        roar.color = new Color(1f, 0.85f, 0.70f, 0.55f);
        roar.scaleFrom = new Vector3(3f, 3f, 1f);
        roar.scaleTo = new Vector3(13f, 13f, 1f);
        roar.spin = 20f;
        roar.life = 1f;

        CameraShake.Shake(CameraShake.Heavy);
    }

    // ───────────── 격파 ─────────────

    public static void DefeatBurst(Vector3 center)
    {
        BossFxSprite rays = BossFxSprite.Create("Boss Defeat Rays", BossFxArt.Rays, center, Layer, 40);
        rays.unscaled = true;
        rays.color = new Color(1f, 0.97f, 0.90f, 1f);
        rays.scaleFrom = new Vector3(2f, 2f, 1f);
        rays.scaleTo = new Vector3(15f, 15f, 1f);
        rays.spin = 10f;
        rays.fadeIn = 0.12f;
        rays.life = 1.8f;

        BossFxSprite core = BossFxSprite.Create("Boss Defeat Core", BossFxArt.SoftDot, center, Layer, 41);
        core.unscaled = true;
        core.color = new Color(1f, 1f, 1f, 1f);
        core.scaleFrom = new Vector3(1.5f, 1.5f, 1f);
        core.scaleTo = new Vector3(7f, 7f, 1f);
        core.life = 0.7f;

        // 흩어지는 빛 조각
        for (int i = 0; i < 36; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            BossFxSprite m = BossFxSprite.Create("Boss Light Mote", BossFxArt.SoftDot, center + (Vector3)(dir * Random.Range(0.2f, 1.2f)), Layer, 42);
            m.unscaled = true;
            m.color = Moon;
            float k = Random.Range(0.12f, 0.24f);
            m.scaleFrom = new Vector3(k, k, 1f);
            m.scaleTo = new Vector3(k * 0.4f, k * 0.4f, 1f);
            m.velocity = dir * Random.Range(2f, 6.5f) + Vector2.up * 0.6f;
            m.drag = 1.6f;
            m.delay = Random.Range(0f, 0.25f);
            m.life = Random.Range(1.3f, 2.2f);
        }
    }
}
