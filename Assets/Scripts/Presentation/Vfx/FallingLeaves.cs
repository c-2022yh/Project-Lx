using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 화면 위에서 낙엽이 천천히 흩날리며 떨어진다. 카메라를 따라다닌다.
///
/// 씬에 빈 오브젝트를 만들고 붙이면 된다. 아래 AutoScene 규칙에 맞는 씬(숲 씬)은
/// 씬에 없어도 자동으로 하나 생긴다. 씬 파일을 안 건드리려고 이렇게 했다.
///
/// 뒤쪽(배경 앞, 지형 뒤)에 작은 잎이 많이, 앞쪽(플레이어 앞)에 큰 잎이 조금 나온다.
/// </summary>
public class FallingLeaves : MonoBehaviour
{
    [Header("Amount")]
    [SerializeField, Min(0f)] private float backLeavesPerSecond = 2.2f;
    [SerializeField, Min(0f)] private float frontLeavesPerSecond = 0.35f;

    [Header("Motion")]
    [Tooltip("옆으로 흘러가는 바람. 음수면 왼쪽으로.")]
    [SerializeField] private float wind = -0.35f;
    [SerializeField, Min(0.05f)] private float fallSpeed = 0.7f;

    [Header("Look")]
    [Tooltip("비우면 코드로 만든 임시 잎 그림을 쓴다.")]
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private Color[] colors =
    {
        new Color(0.80f, 0.52f, 0.22f),
        new Color(0.70f, 0.32f, 0.18f),
        new Color(0.86f, 0.70f, 0.32f),
        new Color(0.55f, 0.58f, 0.28f),
    };

    [Header("Sorting")]
    [SerializeField] private string backSortingLayer = "VFX";
    [SerializeField] private string frontSortingLayer = "Effects";

    private ParticleSystem backLeaves;
    private ParticleSystem frontLeaves;
    private Camera cam;

    // ───────────── 자동 생성 ─────────────

    /// <summary>이 이름이 들어간 씬은 낙엽을 자동으로 깐다.</summary>
    private static readonly string[] AutoSceneKeywords = { "Forest", "Test_Alpha", "Tutorial", "F_0" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAutoSpawn(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAutoSpawn(scene);

    private static void TryAutoSpawn(Scene scene)
    {
        if (!IsAutoScene(scene.name)) return;
        if (FindAnyObjectByType<FallingLeaves>() != null) return;

        GameObject go = new GameObject("[FallingLeaves]");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<FallingLeaves>();
    }

    private static bool IsAutoScene(string sceneName)
    {
        foreach (string key in AutoSceneKeywords)
            if (sceneName.Contains(key)) return true;
        return false;
    }

    // ───────────── 본체 ─────────────

    private void Awake()
    {
        Texture tex = VfxResources.TextureOr(leafSprite, VfxResources.Leaf);
        backLeaves = Build("Leaves_Back", tex, backSortingLayer, -10, backLeavesPerSecond, 0.10f, 0.20f, 0.8f);
        frontLeaves = Build("Leaves_Front", tex, frontSortingLayer, 10, frontLeavesPerSecond, 0.22f, 0.34f, 1.15f);

        // 미리 채워 두기(prewarm)가 카메라 위치에서 일어나도록 자리부터 잡고 재생한다.
        LateUpdate();
        backLeaves.Play();
        frontLeaves.Play();
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float halfHeight = cam.orthographic ? cam.orthographicSize : 5f;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 top = cam.transform.position + new Vector3(-wind * 2f, halfHeight + 0.6f, 0f);
        top.z = 0f;

        Place(backLeaves, top, halfWidth);
        Place(frontLeaves, top, halfWidth);
    }

    private static void Place(ParticleSystem ps, Vector3 position, float halfWidth)
    {
        if (ps == null) return;
        ps.transform.position = position;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.scale = new Vector3(halfWidth * 2.6f, 0.2f, 1f);
    }

    private ParticleSystem Build(string name, Texture tex, string sortingLayer, int order,
                                 float rate, float minSize, float maxSize, float speedScale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float fall = fallSpeed * speedScale;
        float life = 16f / Mathf.Max(0.2f, fall);   // 화면 위에서 아래까지 넉넉히

        ParticleSystem.MainModule main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.8f, life);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = RandomColors();
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 0.2f, 1f);
        shape.randomDirectionAmount = 0f;

        // 아래로 떨어지면서 바람 쪽으로 흐름
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(wind * 0.6f, wind * 1.4f);
        vel.y = new ParticleSystem.MinMaxCurve(-fall * 1.25f, -fall * 0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // 팔랑팔랑 흔들림
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.55f * speedScale;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.25f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // 빙글빙글
        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);   // 라디안/초

        // 뒤집히는 것처럼 가로 폭이 줄었다 늘었다
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = true;
        size.x = new ParticleSystem.MinMaxCurve(1f, FlipCurve());
        size.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
        size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));

        // 생길 때와 사라질 때 살짝 페이드
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.05f), new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = VfxResources.MaterialFor(tex);
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;

        return ps;
    }

    private ParticleSystem.MinMaxGradient RandomColors()
    {
        if (colors == null || colors.Length == 0) return new ParticleSystem.MinMaxGradient(Color.white);

        // 팔레트에서 하나를 고르도록 계단식 그라디언트를 만든다.
        int n = Mathf.Min(colors.Length, 8);
        GradientColorKey[] keys = new GradientColorKey[n];
        for (int i = 0; i < n; i++) keys[i] = new GradientColorKey(colors[i], n == 1 ? 0f : i / (float)(n - 1));

        Gradient g = new Gradient { mode = GradientMode.Fixed };
        g.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

        return new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
    }

    private static AnimationCurve FlipCurve()
    {
        AnimationCurve c = new AnimationCurve();
        const int flips = 7;
        for (int i = 0; i <= flips * 2; i++)
            c.AddKey(i / (float)(flips * 2), i % 2 == 0 ? 1f : 0.25f);
        return c;
    }
}
