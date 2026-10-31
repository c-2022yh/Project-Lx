using UnityEngine;

/// <summary>
/// 튜토리얼 동굴 배경.
///
///   긴 그림 한 장(동굴 안 → 출구)을 쓴다. 동굴 안 그림(insideSprite)을 넣으면
///   [동굴 안] [동굴 안(좌우 뒤집음)] [출구] 로 이어 붙인다 (지금은 안 씀).
///   카메라 x에 맞춰 보이는 부분을 정한다.
///   카메라 x에 맞춰 보이는 부분을 정한다. 시작에서는 맨 왼쪽, 끝(전투 구역)에서는 출구 구멍이 화면 가운데에 온다.
///   두 번째 동굴 그림은 좌우를 뒤집어서 이음새가 자연스럽게 맞는다.
///   출구 그림은 왼쪽 끝이 흐려지게 만들어 둬서 동굴 그림 위에 겹쳐 이어진다.
///
/// 보이는 위치를 카메라 위치만으로 정하므로, 죽고 제단에서 다시 살아나도 배경이 어긋나지 않는다.
/// 빛을 안 받는 머티리얼을 써서 동굴 밝기 조명(CaveLightRamp)에 어두워지지 않는다. 밝고 어두움은 그림 자체로 보인다.
/// 튜토리얼 맵 빌더가 Background_Cave 오브젝트에 붙이고 그림을 채운다.
///
/// 빛 먼지 (11.01): 그림에서 밝은(흰) 부분에서만 반짝이는 먼지가 천천히 떠오른다.
///   파티클 모양을 그림 자체로 정하고, 그림 밝기가 motesThreshold보다 밝은 곳에서만 나오게 한다.
///   그래서 그림 파일은 Read/Write가 켜져 있어야 한다 (BG_Cave_Panorama는 켜 둠).
///   먼지는 그림에 붙어서 같이 움직인다 (배경이 카메라를 따라 밀려도 밝은 곳에 그대로 있다).
/// </summary>
[DefaultExecutionOrder(900)]   // 카메라가 움직인 다음에 자리를 정한다 (ParallaxLayer와 같은 순서)
public class CavePanoramaBackground : MonoBehaviour
{
    [Header("그림")]
    [SerializeField] private Sprite insideSprite;
    [SerializeField] private Sprite exitSprite;

    [Header("크기·위치")]
    [Tooltip("배경 기본 높이 (월드 단위). 화면이 더 크면(카메라 크기 7이면 높이 14) 화면에 맞춰 자동으로 키운다")]
    [SerializeField] private float height = 11f;
    [Tooltip("출구 그림이 앞 그림과 겹치는 비율 (그림 왼쪽 흐린 부분 폭)")]
    [SerializeField, Range(0f, 0.5f)] private float exitOverlap = 0.15f;
    [Tooltip("출구 그림에서 구멍 가운데 (0 왼쪽 ~ 1 오른쪽). 끝에서 이 지점이 화면 가운데에 온다")]
    [SerializeField, Range(0f, 1f)] private float exitFocus = 0.58f;
    [Tooltip("배경을 카메라보다 위아래로 옮김")]
    [SerializeField] private float yOffset = 0f;

    [Header("카메라가 보여주는 맵 범위 (월드 x, CameraConfiner와 같게)")]
    [SerializeField] private float viewLeftX = 0f;
    [SerializeField] private float viewRightX = 140f;

    [Header("빛 먼지 (그림의 밝은 곳에서만)")]
    [SerializeField] private bool glowMotes = true;
    [Tooltip("그림 밝기(0~1)가 이보다 밝은 곳에서만 먼지가 나온다")]
    [SerializeField, Range(0f, 1f)] private float motesThreshold = 0.62f;
    [SerializeField] private float motesPerSecond = 22f;
    [SerializeField] private Color motesColor = new Color(0.82f, 0.93f, 1f, 0.9f);

    [Header("그리기 순서")]
    [SerializeField] private string sortingLayer = "BackGround";
    [SerializeField] private int sortingOrder = -100;

    private Camera cam;
    private Transform strip;
    private float stripWidth;
    private float exitCenter;

    private void Start()
    {
        strip = new GameObject("Strip").transform;
        strip.SetParent(transform, false);

        float wIn = Width(insideSprite);
        float wEx = Width(exitSprite);
        float exitLeft = 0f;
        if (insideSprite != null)
        {
            Piece(insideSprite, 0f, false, 0);
            Piece(insideSprite, wIn, true, 0);
            exitLeft = 2f * wIn - wEx * exitOverlap;
        }
        Piece(exitSprite, exitLeft, false, 1);

        stripWidth = exitLeft + wEx;
        exitCenter = exitLeft + wEx * exitFocus;
    }

    private float Width(Sprite s) => s == null ? 0f : height * s.rect.width / s.rect.height;

    private void Piece(Sprite s, float left, bool flip, int orderAdd)
    {
        if (s == null) return;

        GameObject go = new GameObject(s.name);
        go.transform.SetParent(strip, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.flipX = flip;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder + orderAdd;

        // 피벗과 상관없이 왼쪽 끝을 left에, 세로 가운데를 0에
        Bounds b = s.bounds;
        float scale = height / b.size.y;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        float minX = flip ? -b.max.x : b.min.x;
        go.transform.localPosition = new Vector3(left - minX * scale, -b.center.y * scale, 0f);

        if (glowMotes) AddMotes(sr);
    }

    private void AddMotes(SpriteRenderer sr)
    {
        Texture2D tex = sr.sprite.texture;
        if (tex == null || !tex.isReadable)
        {
            Debug.LogWarning("[CavePanoramaBackground] 빛 먼지: 그림의 Read/Write가 꺼져 있어서 밝은 곳을 못 찾아요 — " + (tex != null ? tex.name : "없음"));
            return;
        }

        GameObject go = new GameObject("Glow Motes");
        go.transform.SetParent(sr.transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = motesColor;
        main.maxParticles = 160;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;   // 그림에 붙어서 같이 움직인다
        main.scalingMode = ParticleSystemScalingMode.Shape;           // 크기는 그림 배율과 상관없이 월드 크기
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = motesPerSecond;

        // 그림 모양에서 나오되, 그림이 밝은 곳에서만
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.SpriteRenderer;
        shape.spriteRenderer = sr;
        shape.texture = tex;
        shape.textureClipChannel = ParticleSystemShapeTextureChannel.Red;
        shape.textureClipThreshold = motesThreshold;
        shape.textureColorAffectsParticles = false;
        shape.textureAlphaAffectsParticles = false;

        // 천천히 위로 떠오르며 살랑살랑
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vel.y = new ParticleSystem.MinMaxCurve(0.04f, 0.22f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;
        noise.quality = ParticleSystemNoiseQuality.Low;

        // 서서히 켜졌다 꺼지고, 반짝이듯 크기가 출렁인다
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve twinkle = new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(0.4f, 0.7f),
            new Keyframe(0.6f, 1.05f), new Keyframe(0.8f, 0.75f), new Keyframe(1f, 0.4f));
        size.size = new ParticleSystem.MinMaxCurve(1f, twinkle);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = VfxResources.MaterialFor(BossFxArt.SoftDot.texture);
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = sortingOrder + 5;

        ps.Play();
    }

    private void LateUpdate()
    {
        if (strip == null) return;
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // 화면 크기에 맞춰 그림을 키운다 (위아래가 잘려 보이지 않게)
        float viewH = cam.orthographic ? cam.orthographicSize * 2f : 14f;
        float halfW = viewH * 0.5f * cam.aspect;
        float s = Mathf.Max(1f, (viewH + 1f) / height);
        strip.localScale = new Vector3(s, s, 1f);
        float width = stripWidth * s;
        float focus = exitCenter * s;

        float from = halfW + 0.2f;                                         // 시작: 그림 왼쪽 끝이 화면 왼쪽
        float to = Mathf.Max(from, Mathf.Min(focus, width - halfW - 0.2f)); // 끝: 출구 구멍이 가운데 (그림 밖은 안 보이게)
        // 카메라가 실제로 움직일 수 있는 범위 (맵 범위에서 화면 반 폭씩 안쪽)
        float k = Mathf.InverseLerp(viewLeftX + halfW, viewRightX - halfW, cam.transform.position.x);
        float local = Mathf.Lerp(from, to, k);

        Vector3 p = cam.transform.position;
        transform.position = new Vector3(p.x - local, p.y + yOffset, 0f);
    }
}
