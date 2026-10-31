using UnityEngine;

/// <summary>Red core and short world-space embers. ScorchedEarthProjectile owns all gameplay.</summary>
[DisallowMultipleComponent]
public sealed class ScorchedEarthProjectileVisual : MonoBehaviour
{
    [Header("Red Core")]
    [SerializeField] private Material glowMaterial;
    [SerializeField, ColorUsage(true, true)] private Color coreColor = new Color(3.6f, 0.035f, 0.07f, 1f);
    [SerializeField, ColorUsage(true, true)] private Color haloColor = new Color(1.8f, 0.015f, 0.04f, 1f);
    [SerializeField, Min(0.01f)] private float coreSize = 0.32f;
    [SerializeField, Min(0.01f)] private float haloSize = 0.78f;
    [SerializeField, Range(0f, 1f)] private float haloStrength = 0.4f;
    [SerializeField, Range(0.5f, 3f)] private float coreSoftness = 1.6f;
    [SerializeField, Range(0.1f, 1f)] private float preparationScale = 0.8f;
    [SerializeField, Range(0f, 0.3f)] private float pulseAmount = 0.06f;
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 4;

    [Header("Soft Particle Tail")]
    [SerializeField, Range(0f, 100f)] private float embersPerSecond = 55f;
    [SerializeField, Min(0.01f)] private float emberSize = 0.14f;
    [SerializeField, Min(0.01f)] private float emberLifetime = 0.16f;
    [SerializeField, Range(0f, 60f)] private float sparklesPerSecond = 4f;
    [SerializeField, Min(0.01f)] private float sparkleSize = 0.08f;

    [Header("Launch And Confirmed Hit")]
    [SerializeField, Min(0.01f)] private float launchFlashDuration = 0.055f;
    [SerializeField, Min(1f)] private float launchFlashScale = 1.25f;
    [SerializeField, Min(0.01f)] private float impactSize = 0.6f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.075f;
    [SerializeField, Range(0, 20)] private int impactEmbers = 5;

    public bool IsReady => rig != null;
    private GameObject rig;
    private SpriteRenderer original;
    private bool originalEnabled;
    private MeshRenderer core;
    private MeshRenderer impact;
    private ParticleSystem embers;
    private ParticleSystem sparkles;
    private ParticleSystem hitEmbers;
    private ScorchedEarthVisualResidue residue;
    private MaterialPropertyBlock properties;
    private float age;
    private float launchAge;
    private float pulsePhase;
    private bool flying;

    private void OnEnable()
    {
        if (glowMaterial == null) return;
        age = 0f;
        flying = false;
        pulsePhase = Random.value * Mathf.PI * 2f;
        properties = new MaterialPropertyBlock();
        original = GetComponent<SpriteRenderer>();
        originalEnabled = original != null && original.enabled;
        rig = new GameObject("Scorched Earth Red Glow (Runtime)");
        rig.layer = gameObject.layer;
        rig.transform.position = transform.position;
        Mesh quad = new Mesh { name = "Red Glow Quad (Runtime)" };
        quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quad.RecalculateBounds();
        core = CreateGlow("Scarlet Core And Crimson Halo", quad, haloSize);
        impact = CreateGlow("Small Red Impact", quad, impactSize);
        impact.enabled = false;
        embers = CreateParticles("Soft Red Light Motes", 2, emberSize, emberLifetime, embersPerSecond);
        sparkles = CreateParticles("Red Twinkles", 1, sparkleSize, emberLifetime, sparklesPerSecond);
        hitEmbers = CreateParticles("Impact Embers", 1, emberSize * 1.2f, emberLifetime, 0f);
        residue = rig.AddComponent<ScorchedEarthVisualResidue>();
        residue.Initialize(quad, impact);
        if (original != null) original.enabled = false;
        UpdateCore();
    }

    public void BeginFlight()
    {
        if (rig == null || flying) return;
        flying = true;
        launchAge = age;
        rig.transform.position = transform.position;
        embers.Play();
        sparkles.Play();
        UpdateCore();
    }

    public void PlayImpact()
    {
        Release(true);
    }

    private MeshRenderer CreateGlow(string objectName, Mesh mesh, float size)
    {
        var child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(rig.transform, false);
        child.transform.localScale = Vector3.one * size;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = child.AddComponent<MeshRenderer>();
        Setup(renderer, 0, sortingOrder);
        return renderer;
    }

    private ParticleSystem CreateParticles(string objectName, int shapeMode, float size, float life, float rate)
    {
        var child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(rig.transform, false);
        var ps = child.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.65f, life);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size * 1.25f);
        main.startSpeed = 0.3f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.45f, 0.45f, 0.65f), Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 32;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = coreSize * 0.2f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var scale = ps.sizeOverLifetime;
        scale.enabled = true;
        scale.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.7f, 0.65f), new Keyframe(1f, 0.3f)));
        Setup(ps.GetComponent<ParticleSystemRenderer>(), shapeMode, sortingOrder - 1);
        return ps;
    }

    private void Setup(Renderer renderer, int shape, int order)
    {
        renderer.sharedMaterial = glowMaterial;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        properties.Clear();
        properties.SetColor("_CoreColor", coreColor);
        properties.SetColor("_HaloColor", haloColor);
        properties.SetFloat("_HaloStrength", haloStrength);
        properties.SetFloat("_CoreSoftness", coreSoftness);
        properties.SetFloat("_CoreRatio", Mathf.Clamp(coreSize / Mathf.Max(coreSize, haloSize), 0.05f, 1f));
        properties.SetFloat("_Shape", shape);
        properties.SetFloat("_Opacity", 1f);
        renderer.SetPropertyBlock(properties);
    }

    private void LateUpdate()
    {
        if (rig == null) return;
        age += Time.deltaTime;
        rig.transform.position = transform.position;
        UpdateCore();
    }

    private void UpdateCore()
    {
        float pulse = 1f + Mathf.Sin(age * (flying ? 20f : 8f) + pulsePhase) * pulseAmount;
        float launch = flying ? Mathf.Clamp01(1f - (age - launchAge) / launchFlashDuration) : 0f;
        float scale = (flying ? 1f : preparationScale) * pulse * Mathf.Lerp(1f, launchFlashScale, launch);
        core.transform.localScale = Vector3.one * Mathf.Max(coreSize, haloSize) * scale;
        core.GetPropertyBlock(properties);
        properties.SetFloat("_Opacity", flying ? 1f : 0.8f);
        core.SetPropertyBlock(properties);
    }

    private void Release(bool confirmedHit)
    {
        if (rig == null) return;
        rig.transform.position = transform.position;
        core.enabled = false;
        embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        sparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (confirmedHit)
        {
            hitEmbers.Play();
            for (int i = 0; i < impactEmbers; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, impactEmbers);
                hitEmbers.Emit(new ParticleSystem.EmitParams { velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.5f }, 1);
            }
            hitEmbers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        residue.Finish(Mathf.Max(emberLifetime, confirmedHit ? impactDuration : 0f) + 0.04f,
            confirmedHit ? impactDuration : 0f);
        rig = null;
    }

    private void OnDisable()
    {
        Release(false);
        if (original != null) original.enabled = originalEnabled;
    }
}
