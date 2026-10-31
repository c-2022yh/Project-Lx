using UnityEngine;

/// <summary>
/// 벽이 부서질 때 파편을 뿌린다. 세 겹으로 나온다.
///   1. 앞 파편: 튀어 올랐다가 바닥(Ground 레이어)에 통통 튀며 떨어짐
///   2. 뒤 파편: 벽 뒤로 작은 조각이 우수수 떨어짐 (바닥 충돌 없음)
///   3. 먼지: 뒤쪽에서 뿌옇게 피어올랐다 사라짐
///
///   DebrisBurst.Spawn(bounds, color, sortingLayerId, sortingOrder);
///
/// 파티클 오브젝트는 다 끝나면 스스로 사라진다.
/// </summary>
public static class DebrisBurst
{
    public struct Options
    {
        public Color color;
        public int sortingLayerId;
        public int sortingOrder;
        public float strength;     // 1이 기본. 크면 더 멀리, 더 많이
        public Sprite chunkSprite; // 비우면 임시 그림
        public bool shake;

        public static Options Default(Color color) => new Options
        {
            color = color,
            sortingLayerId = 0,
            sortingOrder = 0,
            strength = 1f,
            chunkSprite = null,
            shake = true
        };
    }

    private const string GroundLayerName = "Ground";

    public static void Spawn(Bounds area, Color color, int sortingLayerId, int sortingOrder)
    {
        Options o = Options.Default(color);
        o.sortingLayerId = sortingLayerId;
        o.sortingOrder = sortingOrder;
        Spawn(area, o);
    }

    /// <summary>부서지는 오브젝트의 그림 크기·정렬 순서를 그대로 따라 파편을 뿌린다.</summary>
    public static void SpawnFrom(GameObject source, Color color, float strength = 1f)
    {
        if (source == null) return;

        SpriteRenderer sr = source.GetComponentInChildren<SpriteRenderer>();
        Collider2D col = source.GetComponentInChildren<Collider2D>();
        Bounds area = sr != null ? sr.bounds
                    : col != null ? col.bounds
                    : new Bounds(source.transform.position, Vector3.one);

        Color tint = sr != null ? color * sr.color : color;
        tint.a = 1f;

        Options o = Options.Default(tint);
        o.strength = strength;
        if (sr != null)
        {
            o.sortingLayerId = sr.sortingLayerID;
            o.sortingOrder = sr.sortingOrder;
        }
        Spawn(area, o);
    }

    public static void Spawn(Bounds area, Options o)
    {
        float strength = Mathf.Max(0.2f, o.strength);
        float areaSize = Mathf.Max(0.5f, area.size.x * area.size.y);
        Texture chunkTex = VfxResources.TextureOr(o.chunkSprite, VfxResources.Chunk);

        // 1. 앞 파편 (튀고 구름)
        ParticleSystem front = Create("Debris_Front", area.center, chunkTex, o.sortingLayerId, o.sortingOrder + 1, 2.4f, true);
        int frontCount = Mathf.Clamp(Mathf.RoundToInt(areaSize * 6f * strength), 8, 40);
        for (int i = 0; i < frontCount; i++)
        {
            ParticleSystem.EmitParams e = new ParticleSystem.EmitParams
            {
                position = RandomPoint(area),
                velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(1.5f, 5f), 0f) * strength,
                startSize = Random.Range(0.08f, 0.24f),
                startLifetime = Random.Range(1.4f, 2.2f),
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-540f, 540f),
                startColor = Shade(o.color, Random.Range(0.7f, 1.05f))
            };
            front.Emit(e, 1);
        }

        // 2. 뒤 파편 (벽 뒤로 떨어짐)
        ParticleSystem back = Create("Debris_Back", area.center, chunkTex, o.sortingLayerId, o.sortingOrder - 1, 1.8f, false);
        int backCount = Mathf.Clamp(Mathf.RoundToInt(areaSize * 5f * strength), 6, 30);
        for (int i = 0; i < backCount; i++)
        {
            ParticleSystem.EmitParams e = new ParticleSystem.EmitParams
            {
                position = RandomPoint(area),
                velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 1.5f), 0f) * strength,
                startSize = Random.Range(0.05f, 0.14f),
                startLifetime = Random.Range(0.8f, 1.4f),
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-360f, 360f),
                startColor = Shade(o.color, Random.Range(0.45f, 0.7f))
            };
            back.Emit(e, 1);
        }

        // 3. 먼지
        ParticleSystem dust = Create("Debris_Dust", area.center, VfxResources.Dust, o.sortingLayerId, o.sortingOrder - 2, -0.05f, false);
        ParticleSystem.SizeOverLifetimeModule size = dust.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.6f));
        int dustCount = Mathf.Clamp(Mathf.RoundToInt(areaSize * 3f), 5, 18);
        Color dustColor = Color.Lerp(o.color, Color.white, 0.35f);
        for (int i = 0; i < dustCount; i++)
        {
            Vector3 p = RandomPoint(area);
            ParticleSystem.EmitParams e = new ParticleSystem.EmitParams
            {
                position = p,
                velocity = new Vector3((p.x - area.center.x) * 0.8f, Random.Range(0.1f, 0.6f), 0f),
                startSize = Random.Range(0.5f, 1.1f),
                startLifetime = Random.Range(0.7f, 1.2f),
                rotation = Random.Range(0f, 360f),
                startColor = new Color(dustColor.r, dustColor.g, dustColor.b, Random.Range(0.35f, 0.55f))
            };
            dust.Emit(e, 1);
        }

        if (o.shake) CameraShake.Shake(CameraShake.Light);
    }

    private static ParticleSystem Create(string name, Vector3 position, Texture texture,
                                         int sortingLayerId, int sortingOrder, float gravity, bool collide)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        // 끝무렵에 서서히 사라짐
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;

        if (collide)
        {
            ParticleSystem.CollisionModule c = ps.collision;
            c.enabled = true;
            c.type = ParticleSystemCollisionType.World;
            c.mode = ParticleSystemCollisionMode.Collision2D;
            int ground = LayerMask.GetMask(GroundLayerName);
            c.collidesWith = ground != 0 ? ground : Physics2D.DefaultRaycastLayers;
            c.bounce = 0.35f;
            c.dampen = 0.45f;
            c.lifetimeLoss = 0f;
            c.radiusScale = 0.5f;
            c.quality = ParticleSystemCollisionQuality.Medium;
        }

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = VfxResources.MaterialFor(texture);
        r.sortingLayerID = sortingLayerId;
        r.sortingOrder = sortingOrder;

        VfxResources.ApplySheet(ps, texture);

        ps.Play();
        return ps;
    }

    private static Vector3 RandomPoint(Bounds b)
        => new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);

    private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
}
