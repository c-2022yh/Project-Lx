using UnityEngine;

/// <summary>
/// 덤불·나무에 붙이면 플레이어가 지나갈 때 잎이 몇 장 떨어진다.
/// 트리거 콜라이더(Collider2D, Is Trigger)가 같이 있어야 한다.
/// 흔들리는 모양 자체는 FoliageSway 머티리얼이 맡는다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FoliageRustle : MonoBehaviour
{
    [SerializeField, Min(0)] private int leavesPerRustle = 3;
    [Tooltip("이 속도보다 빠르게 지나가야 잎이 떨어진다.")]
    [SerializeField, Min(0f)] private float minSpeed = 1.5f;
    [SerializeField, Min(0f)] private float cooldown = 0.8f;
    [Tooltip("비우면 코드로 만든 임시 잎 그림을 쓴다.")]
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private Color leafColor = new Color(0.45f, 0.62f, 0.6f);

    private float nextTime;
    private ParticleSystem ps;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < nextTime) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && rb.linearVelocity.magnitude < minSpeed) return;

        nextTime = Time.time + cooldown;
        Emit(other.bounds.center);
    }

    private void Emit(Vector3 around)
    {
        if (leavesPerRustle <= 0) return;
        if (ps == null) ps = Build();

        Bounds b = GetComponent<Collider2D>().bounds;
        for (int i = 0; i < leavesPerRustle; i++)
        {
            Color color = leafColor * Random.Range(0.85f, 1.1f);
            color.a = 1f;

            ParticleSystem.EmitParams e = new ParticleSystem.EmitParams
            {
                position = new Vector3(Mathf.Clamp(around.x + Random.Range(-0.4f, 0.4f), b.min.x, b.max.x),
                                       Random.Range(b.center.y, b.max.y), 0f),
                velocity = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 0.8f), 0f),
                startSize = Random.Range(0.12f, 0.2f),
                startLifetime = Random.Range(1.6f, 2.4f),
                rotation = Random.Range(0f, 360f),
                angularVelocity = Random.Range(-200f, 200f),
                startColor = color
            };
            ps.Emit(e, 1);
        }
    }

    private ParticleSystem Build()
    {
        GameObject go = new GameObject("RustleLeaves");
        go.transform.SetParent(transform, false);

        ParticleSystem p = go.AddComponent<ParticleSystem>();
        p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = p.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.gravityModifier = 0.06f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = p.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = p.shape;
        shape.enabled = false;

        ParticleSystem.NoiseModule noise = p.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.6f;

        ParticleSystem.ColorOverLifetimeModule col = p.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        Texture tex = VfxResources.TextureOr(leafSprite, VfxResources.Leaf);
        r.sharedMaterial = VfxResources.MaterialFor(tex);
        VfxResources.ApplySheet(p, tex);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            r.sortingLayerID = sr.sortingLayerID;
            r.sortingOrder = sr.sortingOrder + 1;
        }

        p.Play();
        return p;
    }
}
