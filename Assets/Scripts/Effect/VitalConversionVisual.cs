using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Healing presentation only. VitalConversionSkillData owns the heal and its timing.</summary>
[DisallowMultipleComponent]
public sealed class VitalConversionVisual : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Material effectMaterial;
    [SerializeField, ColorUsage(true, true)] private Color energyColor = new Color(0.3f, 2f, 0.65f, 1f);
    [SerializeField, ColorUsage(true, true)] private Color healColor = new Color(1.5f, 2.5f, 1.7f, 1f);
    [SerializeField] private string sortingLayer = "Effects";
    [SerializeField] private int sortingOrder = 3;
    [SerializeField] private Vector2 positionOffset = new Vector2(0f, 0.05f);

    [Header("Rising Energy Ribbons")]
    [SerializeField, Min(0.1f)] private float height = 1.8f;
    [SerializeField, Min(0.05f)] private float radius = 0.55f;
    [SerializeField, Range(1, 4)] private int ribbonCount = 2;
    [SerializeField, Min(0.005f)] private float ribbonWidth = 0.055f;
    [SerializeField, Min(0f)] private float swirlSpeed = 0.9f;
    [SerializeField, Min(0.01f)] private float footRingWidth = 0.035f;

    [Header("Rising Particles")]
    [SerializeField, Range(0f, 80f)] private float lightsPerSecond = 22f;
    [SerializeField, Min(0.01f)] private float lightSize = 0.09f;
    [SerializeField, Range(0f, 30f)] private float leavesPerSecond = 7f;
    [SerializeField, Min(0.01f)] private float leafSize = 0.16f;
    [SerializeField, Min(0f)] private float riseSpeed = 1.1f;
    [SerializeField, Min(0.05f)] private float particleLifetime = 0.75f;

    [Header("Healing Completion")]
    [SerializeField] private bool showPlus = true;
    [SerializeField, Min(0.01f)] private float plusSize = 0.4f;
    [SerializeField, Min(0f)] private float plusRiseSpeed = 0.9f;
    [SerializeField, Min(0.05f)] private float finishDuration = 0.75f;
    [SerializeField, Min(0.05f)] private float cancelFadeDuration = 0.25f;
    [SerializeField, Min(0.05f)] private float pulseRadius = 1.05f;
    [SerializeField, Min(0.01f)] private float pulseWidth = 0.045f;

    [Header("Bloom")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField, Range(0f, 3f)] private float bloomIntensity = 0.35f;
    [SerializeField, Min(0f)] private float bloomThreshold = 1.1f;

    public VitalConversionSkillData Skill { get; private set; }
    public bool IsFinishing { get; private set; }
    private Player owner;
    private PlayerHealth health;
    private LineRenderer[] ribbons;
    private LineRenderer footRing;
    private LineRenderer healPulse;
    private ParticleSystem lights;
    private ParticleSystem leaves;
    private ParticleSystem plus;
    private ParticleSystem burst;
    private readonly List<Renderer> renderers = new();
    private MaterialPropertyBlock properties;
    private Volume volume;
    private VolumeProfile profile;
    private float age;
    private float endAge;
    private bool completed;
    private float opacity = 1f;
    private const int Segments = 48;

    private void Awake()
    {
        properties = new MaterialPropertyBlock();
    }

    public void Begin(Player player, VitalConversionSkillData skill)
    {
        owner = player;
        Skill = skill;
        if (owner == null || effectMaterial == null)
        {
            Debug.LogWarning("VitalConversionVisual needs a player and effect material.", this);
            Destroy(gameObject);
            return;
        }
        // Preserve world size; the player owns cleanup, but its sprite never changes.
        transform.SetParent(owner.transform, true);
        FollowOwner();
        health = owner.GetComponent<PlayerHealth>();
        if (health != null) health.OnDied += Cancel;
        ribbons = new LineRenderer[Mathf.Clamp(ribbonCount, 1, 4)];
        for (int i = 0; i < ribbons.Length; i++) ribbons[i] = CreateLine("Rising Ribbon " + i, ribbonWidth);
        footRing = CreateLine("Foot Ring", footRingWidth);
        healPulse = CreateLine("Healing Pulse", pulseWidth);
        healPulse.enabled = false;
        lights = CreateParticles("Rising Lights", 1, lightSize, lightsPerSecond, riseSpeed, energyColor);
        leaves = CreateParticles("Rising Leaves", 2, leafSize, leavesPerSecond, riseSpeed * 0.8f, energyColor);
        plus = CreateParticles("Healing Plus", 3, plusSize, 0f, plusRiseSpeed, healColor);
        burst = CreateParticles("Healing Sparkles", 1, lightSize * 1.4f, 0f, riseSpeed * 1.4f, healColor);
        plus.transform.localPosition = Vector3.up * height * 0.65f;
        burst.transform.localPosition = Vector3.up * height * 0.45f;
        lights.Play();
        leaves.Play();
        CreateBloom();
        DrawEnergy();
    }

    public void Complete()
    {
        if (IsFinishing || ribbons == null) return;
        completed = true;
        StartFinish();
        if (showPlus) { plus.Play(); plus.Emit(1); }
        burst.Play();
        burst.Emit(14);
        healPulse.enabled = true;
        DrawEnergy();
    }

    public void Cancel()
    {
        // A committed heal keeps its completion effect, including during recovery cancellation.
        if (IsFinishing || ribbons == null) return;
        StartFinish();
    }

    private void StartFinish()
    {
        IsFinishing = true;
        endAge = 0f;
        lights.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        leaves.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private LineRenderer CreateLine(string objectName, float width)
    {
        var child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        var line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = Segments + 1;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        ConfigureRenderer(line, 0, energyColor);
        return line;
    }

    private ParticleSystem CreateParticles(string objectName, int shapeMode, float size, float rate, float speed, Color color)
    {
        var child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = shapeMode == 3 ? finishDuration : particleLifetime;
        main.startRotation = shapeMode == 2 ? new ParticleSystem.MinMaxCurve(-0.7f, 0.7f) : 0f;
        main.startColor = Color.white;
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;
        var shape = particles.shape;
        shape.enabled = rate > 0f;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius * 2f, 0.08f, 0.01f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // All three axes must use the same curve mode in Unity's velocity module.
        velocity.x = new ParticleSystem.MinMaxCurve(shapeMode == 3 ? 0f : -0.15f, shapeMode == 3 ? 0f : 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(speed, speed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var colorOverLife = particles.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;
        ConfigureRenderer(particles.GetComponent<ParticleSystemRenderer>(), shapeMode, color);
        return particles;
    }

    private void ConfigureRenderer(Renderer renderer, int mode, Color color)
    {
        renderer.sharedMaterial = effectMaterial;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        properties.Clear();
        properties.SetFloat("_Shape", mode);
        properties.SetColor("_Tint", color);
        properties.SetFloat("_Opacity", 1f);
        renderer.SetPropertyBlock(properties);
        renderers.Add(renderer);
    }

    private void Update()
    {
        if (ribbons == null) return;
        if (owner == null) { Destroy(gameObject); return; }
        age += Time.deltaTime;
        FollowOwner();
        if (IsFinishing)
        {
            endAge += Time.deltaTime;
            float duration = Mathf.Max(0.01f, completed ? finishDuration : cancelFadeDuration);
            opacity = Mathf.Clamp01(1f - endAge / duration);
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_Opacity", opacity);
                renderer.SetPropertyBlock(properties);
            }
            if (volume != null) volume.weight = opacity;
            if (endAge >= duration) { Destroy(gameObject); return; }
        }
        DrawEnergy();
    }

    private void FollowOwner()
    {
        Vector3 position = owner.transform.position;
        if (owner.sr != null && owner.sr.sprite != null) position.y = owner.sr.bounds.min.y;
        transform.position = position + (Vector3)positionOffset;
        transform.rotation = Quaternion.identity;
    }

    private void DrawEnergy()
    {
        float phase = age * swirlSpeed * Mathf.PI * 2f;
        float ribbonFade = IsFinishing ? Mathf.Clamp01(1f - endAge / 0.25f) : 1f;
        for (int r = 0; r < ribbons.Length; r++)
        {
            ribbons[r].widthMultiplier = ribbonWidth * ribbonFade;
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float angle = t * Mathf.PI * 2f - phase + r * Mathf.PI * 2f / ribbons.Length;
                ribbons[r].SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, t * height, 0f));
            }
        }
        DrawRing(footRing, radius, radius * 0.2f, 0.02f);
        if (completed)
        {
            float t = Mathf.Clamp01(endAge / Mathf.Max(0.01f, finishDuration));
            float size = Mathf.Lerp(radius * 0.5f, pulseRadius, t);
            DrawRing(healPulse, size, size, height * 0.5f);
        }
    }

    private static void DrawRing(LineRenderer line, float width, float verticalRadius, float y)
    {
        for (int i = 0; i <= Segments; i++)
        {
            float angle = (float)i / Segments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * width, y + Mathf.Sin(angle) * verticalRadius, 0f));
        }
    }

    private void CreateBloom()
    {
        if (!enableBloom) return;
        var child = new GameObject("Healing Bloom (Runtime)");
        child.transform.SetParent(transform, false);
        child.layer = 0;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(0.65f);
        volume = child.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 11f;
        volume.sharedProfile = profile;
        volume.enabled = false;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (volume == null || !isActiveAndEnabled) return;
        var marker = camera.GetComponent<ScreenEffectsCamera>();
        volume.enabled = camera.cameraType == CameraType.Game && marker != null && marker.isActiveAndEnabled;
    }

    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (volume != null) volume.enabled = false;
    }

    private void OnDisable()
    {
        if (volume != null) volume.enabled = false;
        if (Application.isPlaying) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= Cancel;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        if (profile == null) return;
        foreach (var component in profile.components) Destroy(component);
        Destroy(profile);
    }
}
