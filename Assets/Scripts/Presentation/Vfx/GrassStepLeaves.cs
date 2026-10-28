using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 풀 타일 위를 걷거나 착지하면 발밑에서 잎사귀가 튀어 오른다 (할로우 나이트 풀숲 느낌).
///
/// - 걷기: 일정 거리마다 한두 장이 뒤로 차여 오른다. 빠를수록(대시) 더 자주, 더 높이.
/// - 착지: 높은 데서 떨어지면 양옆으로 한 움큼 튄다.
/// - 잎은 팔랑이며 떨어지다가 바닥(Ground)에 닿으면 잠깐 머물렀다 사라진다.
///
/// 이름에 "Grass"가 들어간 타일맵(예: Tilemap_Grass)이나 타일을 풀로 본다. 씬·프리팹은 건드리지 않는다.
/// 잎 그림은 VfxResources의 선화 잎(남색 선 + 두 단계 음영)을 풀 타일 색으로 물들여 쓴다.
/// </summary>
public class GrassStepLeaves : MonoBehaviour
{
    private const string GrassKeyword = "Grass";

    // 걷기
    private const float StepDistance = 0.55f;      // 이만큼 걸을 때마다 잎이 튐
    private const float DashStepDistance = 0.3f;
    private const float MinWalkSpeed = 1.2f;
    private const float DashSpeed = 9f;            // 이보다 빠르면 대시로 본다

    // 착지
    private const float MinLandingSpeed = 5f;      // 이보다 빠르게 떨어져 착지하면 한 움큼

    // 풀 타일(Panyong 풀 타일맵) 색에서 뽑은 잎 색. 셀 음영이 곱해져서 그림자 쪽은 더 푸르게 보인다.
    private static readonly Color[] LeafColors =
    {
        new Color(0.12f, 0.44f, 0.26f),
        new Color(0.18f, 0.52f, 0.27f),
        new Color(0.06f, 0.34f, 0.24f),
        new Color(0.04f, 0.28f, 0.22f),
    };

    private static GrassStepLeaves instance;

    private readonly List<Tilemap> tilemaps = new();
    private ParticleSystem front;
    private ParticleSystem back;

    private Player player;
    private Rigidbody2D rb;
    private Collider2D body;
    private float nextSearchTime;

    private bool wasGrounded;
    private float fallSpeed;
    private float walked;
    private float lastX;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[GrassStepLeaves]");
        instance = go.AddComponent<GrassStepLeaves>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        Texture tex = VfxResources.Leaf;
        front = Build("GrassLeaves_Front", tex, "Effects", -5);
        back = Build("GrassLeaves_Back", tex, "GroundCover", 20);
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindTilemaps();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindTilemaps();
        front.Clear();
        back.Clear();
        wasGrounded = false;
    }

    private void FindTilemaps()
    {
        tilemaps.Clear();
        tilemaps.AddRange(FindObjectsByType<Tilemap>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (!FindPlayer()) return;

        bool grounded = player.isGrounded;
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        float x = player.transform.position.x;

        if (!grounded)
        {
            fallSpeed = Mathf.Max(fallSpeed, -velocity.y);
            walked = 0f;
        }
        else
        {
            if (!wasGrounded && fallSpeed >= MinLandingSpeed && OnGrass(out Vector3 feet))
                Burst(feet, Mathf.InverseLerp(MinLandingSpeed, 20f, fallSpeed));
            fallSpeed = 0f;

            float speed = Mathf.Abs(velocity.x);
            if (speed >= MinWalkSpeed)
            {
                walked += Mathf.Abs(x - lastX);
                bool dashing = speed >= DashSpeed;
                if (walked >= (dashing ? DashStepDistance : StepDistance))
                {
                    walked = 0f;
                    if (OnGrass(out Vector3 stepFeet)) Kick(stepFeet, Mathf.Sign(velocity.x), dashing);
                }
            }
        }

        wasGrounded = grounded;
        lastX = x;
    }

    private bool FindPlayer()
    {
        if (player != null) return true;
        if (Time.unscaledTime < nextSearchTime) return false;
        nextSearchTime = Time.unscaledTime + 0.5f;

        player = FindFirstObjectByType<Player>();
        if (player == null) return false;

        rb = player.GetComponent<Rigidbody2D>();
        body = null;
        foreach (Collider2D c in player.GetComponents<Collider2D>())
        {
            if (c.isTrigger) continue;
            body = c;
            break;
        }
        lastX = player.transform.position.x;
        wasGrounded = player.isGrounded;
        return true;
    }

    // 발밑 칸에 풀 타일이 있는지
    private bool OnGrass(out Vector3 feet)
    {
        feet = body != null
            ? new Vector3(body.bounds.center.x, body.bounds.min.y, 0f)
            : player.transform.position;

        Vector3 probe = feet + Vector3.down * 0.1f;
        for (int i = tilemaps.Count - 1; i >= 0; i--)
        {
            Tilemap tm = tilemaps[i];
            if (tm == null)
            {
                tilemaps.RemoveAt(i);
                continue;
            }
            if (!tm.isActiveAndEnabled) continue;

            Vector3Int cell = tm.WorldToCell(probe);
            bool namedGrass = tm.name.Contains(GrassKeyword);
            if (IsGrassTile(tm, cell, namedGrass) || IsGrassTile(tm, cell + Vector3Int.up, namedGrass)) return true;
        }
        return false;
    }

    private static bool IsGrassTile(Tilemap tm, Vector3Int cell, bool namedGrass)
    {
        TileBase tile = tm.GetTile(cell);
        if (tile == null) return false;
        return namedGrass || tile.name.Contains(GrassKeyword);
    }

    // ───────────── 잎 뿌리기 ─────────────

    private void Kick(Vector3 feet, float moveDir, bool dashing)
    {
        int count = dashing ? Random.Range(2, 4) : Random.Range(1, 3);
        float lift = dashing ? 1.3f : 1f;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = feet + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.02f, 0.15f), 0f);
            // 걸어가는 반대쪽(뒤)으로 차인다
            Vector3 vel = new Vector3(-moveDir * Random.Range(0.4f, 1.6f) + Random.Range(-0.3f, 0.3f),
                                      Random.Range(1.4f, 2.6f) * lift, 0f);
            Emit(pos, vel, Random.Range(0.2f, 0.3f));
        }
    }

    private void Burst(Vector3 feet, float strength)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(5f, 10f, strength));
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = feet + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.02f, 0.12f), 0f);
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 vel = new Vector3(side * Random.Range(0.6f, 2.2f), Random.Range(1.6f, 3.4f) * Mathf.Lerp(1f, 1.3f, strength), 0f);
            Emit(pos, vel, Random.Range(0.22f, 0.34f));
        }
    }

    private void Emit(Vector3 position, Vector3 velocity, float size)
    {
        ParticleSystem.EmitParams e = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize = size,
            startLifetime = Random.Range(1.6f, 2.6f),
            rotation = Random.Range(0f, 360f),
            angularVelocity = Random.Range(-420f, 420f),
            startColor = LeafColors[Random.Range(0, LeafColors.Length)] * Random.Range(0.9f, 1.12f)
        };
        Color c = e.startColor;
        c.a = 1f;
        e.startColor = c;

        (Random.value < 0.6f ? front : back).Emit(e, 1);
    }

    private ParticleSystem Build(string name, Texture tex, string sortingLayer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.gravityModifier = 0.32f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        // 공기 저항: 처음엔 튀어 오르고, 금방 느려져서 팔랑팔랑 떨어진다
        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.9f;
        limit.dampen = 0.05f;
        limit.drag = 0.6f;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.7f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        // 뒤집히는 것처럼 가로 폭이 줄었다 늘었다
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = true;
        AnimationCurve flip = new AnimationCurve();
        for (int i = 0; i <= 8; i++) flip.AddKey(i / 8f, i % 2 == 0 ? 1f : 0.3f);
        size.x = new ParticleSystem.MinMaxCurve(1f, flip);
        size.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
        size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        // 바닥에 닿으면 멈춰서 잠깐 머문다
        ParticleSystem.CollisionModule c = ps.collision;
        c.enabled = true;
        c.type = ParticleSystemCollisionType.World;
        c.mode = ParticleSystemCollisionMode.Collision2D;
        int ground = LayerMask.GetMask("Ground");
        c.collidesWith = ground != 0 ? ground : Physics2D.DefaultRaycastLayers;
        c.bounce = 0.05f;
        c.dampen = 0.9f;
        c.lifetimeLoss = 0f;
        c.radiusScale = 0.3f;
        c.quality = ParticleSystemCollisionQuality.Medium;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = VfxResources.MaterialFor(tex);
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;

        VfxResources.ApplySheet(ps, tex);
        ps.Play();
        return ps;
    }
}
