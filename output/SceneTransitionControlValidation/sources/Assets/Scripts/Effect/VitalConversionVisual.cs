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

    [Header("Gathering Focus")]
    [SerializeField, Range(0.2f, 0.9f)] private float focusHeight = 0.58f;
    [SerializeField, Min(0.01f)] private float coreSize = 0.38f;
    [SerializeField, Range(0f, 1f)] private float coreOpacity = 0.65f;
    [SerializeField, Min(0.01f)] private float preparationFadeIn = 0.16f;
    [SerializeField, Min(0.01f)] private float completionFlashDuration = 0.18f;
    [SerializeField, Min(1f)] private float completionFlashScale = 2.3f;
    [SerializeField, Range(1, 6)] private int completionSparkleCount = 3;

    [Header("Hand Gather And Release")]
    [SerializeField] private bool enableHandParticles = true;
    [Tooltip("Hand location inside the sprite rectangle: (0,0) bottom-left, (1,1) top-right. Mirrors with SpriteRenderer flip.")]
    [SerializeField] private Vector2 handSpritePosition = new Vector2(0.86f, 0.42f);
    [Tooltip("Fraction of preparation elapsed before hand particles appear. Release is synchronized with the actual heal.")]
    [SerializeField, Range(0f, 0.95f)] private float handGatherStart = 0.6f;
    [SerializeField, Range(1, 64)] private int handParticleCount = 18;
    [SerializeField, Min(0.01f)] private float handGatherRadius = 0.5f;
    [SerializeField, Min(0.01f)] private float handParticleSize = 0.085f;
    [SerializeField, Min(0f)] private float handReleaseSpeed = 2f;
    [SerializeField, Min(0.05f)] private float handReleaseLifetime = 0.38f;

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
    private ParticleSystem handParticles;
    private bool handParticlesEmitted;
    private MeshRenderer core;
    private Mesh coreMesh;
    private readonly ParticleSystem.Particle[] gatheringParticles = new ParticleSystem.Particle[128];
    private readonly List<Renderer> renderers = new();
    private MaterialPropertyBlock properties;
    private Volume volume;
    private VolumeProfile profile;
    private float age;
    private float endAge;
    private bool completed;
    private float opacity = 1f;
    private float footOffset;
    private float preparationDuration;
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
        // Cache the foot anchor so different animation-frame bounds do not shake the effect.
        footOffset = owner.sr != null && owner.sr.sprite != null
            ? owner.sr.bounds.min.y - owner.transform.position.y : 0f;
        preparationDuration = Mathf.Max(0.01f, skill.windupTime + skill.activeTime);
        FollowOwner();
        health = owner.GetComponent<PlayerHealth>();
        if (health != null) health.OnDied += Cancel;
        ribbons = new LineRenderer[Mathf.Clamp(ribbonCount, 1, 4)];
        for (int i = 0; i < ribbons.Length; i++) ribbons[i] = CreateLine("Rising Ribbon " + i, ribbonWidth);
        foreach (var ribbon in ribbons)
            ribbon.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));
        footRing = CreateLine("Foot Ring", footRingWidth);
        healPulse = CreateLine("Healing Pulse", pulseWidth);
        healPulse.enabled = false;
        lights = CreateParticles("Rising Lights", 1, lightSize, lightsPerSecond, riseSpeed, energyColor);
        leaves = CreateParticles("Rising Leaves", 2, leafSize, leavesPerSecond, riseSpeed * 0.8f, energyColor);
        plus = CreateParticles("Completion Cross Sparkles", 5, plusSize, 0f, plusRiseSpeed, healColor);
        burst = CreateParticles("Healing Sparkles", 1, lightSize, 0f, riseSpeed * 0.5f, healColor);
        plus.transform.localPosition = Vector3.up * height * focusHeight;
        burst.transform.localPosition = Vector3.up * height * focusHeight;
        if (enableHandParticles)
        {
            handParticles = CreateParticles("Hand Gather And Release", 1, handParticleSize, 0f, 0f, healColor);
            var velocity = handParticles.velocityOverLifetime;
            velocity.enabled = false;
            var colorOverLife = handParticles.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;
            var sizeOverLife = handParticles.sizeOverLifetime;
            sizeOverLife.enabled = false;
            handParticles.Play();
        }
        core = CreateCore();
        lights.Play();
        leaves.Play();
        CreateBloom();
        DrawEnergy();
    }

    public void Complete()
    {
        if (IsFinishing || ribbons == null) return;
        completed = true;
        ReleaseHandParticles();
        StartFinish();
        if (showPlus)
        {
            plus.Play();
            for (int i = 0; i < completionSparkleCount; i++)
            {
                float angle = i * Mathf.PI * 2f / completionSparkleCount + 0.4f;
                var emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(Mathf.Cos(angle) * radius * 0.65f, Mathf.Sin(angle) * radius * 0.45f, 0f),
                    startLifetime = finishDuration * Random.Range(0.65f, 1f),
                    startSize = plusSize * Random.Range(0.8f, 1.2f)
                };
                plus.Emit(emit, 1);
            }
        }
        burst.Play();
        burst.Emit(6);
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

    private MeshRenderer CreateCore()
    {
        var child = new GameObject("Healing Focus Glow");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        coreMesh = new Mesh { name = "Healing Focus Quad (Runtime)" };
        coreMesh.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
        coreMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        coreMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        coreMesh.RecalculateBounds();
        child.AddComponent<MeshFilter>().sharedMesh = coreMesh;
        var renderer = child.AddComponent<MeshRenderer>();
        ConfigureRenderer(renderer, 4, healColor);
        return renderer;
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
        shape.scale = new Vector3(radius * 2f, height * 0.4f, 0.01f);
        shape.position = Vector3.up * height * 0.2f;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = rate <= 0f;
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
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
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
        if (!IsFinishing)
        {
            GatherParticles(lights);
            GatherParticles(leaves);
            UpdateHandGather();
        }
    }

    private Vector3 GetHandPosition()
    {
        if (owner.sr == null || owner.sr.sprite == null)
            return new Vector3(radius, height * focusHeight, 0f);
        Sprite sprite = owner.sr.sprite;
        Vector2 point = (Vector2.Scale(handSpritePosition, sprite.rect.size) - sprite.pivot) / sprite.pixelsPerUnit;
        if (owner.sr.flipX) point.x = -point.x;
        if (owner.sr.flipY) point.y = -point.y;
        return transform.InverseTransformPoint(owner.sr.transform.TransformPoint(point));
    }

    private void EmitHandParticles()
    {
        if (handParticles == null || handParticlesEmitted) return;
        handParticlesEmitted = true;
        Vector3 target = GetHandPosition();
        for (int i = 0; i < handParticleCount; i++)
        {
            float angle = (i + Random.value * 0.6f) * Mathf.PI * 2f / handParticleCount;
            float distance = handGatherRadius * Random.Range(0.65f, 1f);
            float lifetime = Mathf.Max(0.05f, preparationDuration - age) + Mathf.Min(handReleaseLifetime, finishDuration);
            var emit = new ParticleSystem.EmitParams
            {
                position = target + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance,
                velocity = Vector3.zero,
                startLifetime = lifetime,
                startSize = handParticleSize * Random.Range(0.7f, 1.2f)
            };
            handParticles.Emit(emit, 1);
        }
    }

    private void UpdateHandGather()
    {
        if (handParticles == null || age < preparationDuration * handGatherStart) return;
        EmitHandParticles();
        Vector3 target = GetHandPosition();
        int count = handParticles.GetParticles(gatheringParticles);
        float remaining = Mathf.Max(0.025f, preparationDuration - age);
        float fadeIn = Mathf.Clamp01((age - preparationDuration * handGatherStart) / 0.06f);
        for (int i = 0; i < count; i++)
        {
            var particle = gatheringParticles[i];
            particle.velocity = (target - particle.position) / remaining;
            particle.startColor = new Color(1f, 1f, 1f, fadeIn);
            gatheringParticles[i] = particle;
        }
        handParticles.SetParticles(gatheringParticles, count);
    }

    private void ReleaseHandParticles()
    {
        if (handParticles == null) return;
        EmitHandParticles();
        Vector3 target = GetHandPosition();
        int count = handParticles.GetParticles(gatheringParticles);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + Random.value * 0.4f) * Mathf.PI * 2f / Mathf.Max(1, count);
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var particle = gatheringParticles[i];
            particle.position = target + direction * handParticleSize * 0.4f;
            particle.velocity = direction * handReleaseSpeed * Random.Range(0.65f, 1f);
            particle.startLifetime = particle.remainingLifetime = Mathf.Min(handReleaseLifetime, finishDuration);
            particle.startColor = Color.white;
            gatheringParticles[i] = particle;
        }
        handParticles.SetParticles(gatheringParticles, count);
    }

    private void GatherParticles(ParticleSystem particles)
    {
        int count = particles.GetParticles(gatheringParticles);
        Vector3 target = Vector3.up * height * focusHeight;
        for (int i = 0; i < count; i++)
        {
            var particle = gatheringParticles[i];
            particle.velocity = (target - particle.position) / Mathf.Max(0.06f, particle.remainingLifetime);
            gatheringParticles[i] = particle;
        }
        particles.SetParticles(gatheringParticles, count);
    }

    private void FollowOwner()
    {
        Vector3 position = owner.transform.position;
        position.y += footOffset;
        transform.position = position + (Vector3)positionOffset;
        transform.rotation = Quaternion.identity;
    }

    private void DrawEnergy()
    {
        float phase = age * swirlSpeed * Mathf.PI * 2f;
        float charge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / preparationDuration));
        float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / preparationFadeIn));
        float ribbonFade = IsFinishing ? Mathf.Clamp01(1f - endAge / 0.16f) : fadeIn;
        for (int r = 0; r < ribbons.Length; r++)
        {
            ribbons[r].widthMultiplier = ribbonWidth * ribbonFade;
            for (int i = 0; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float angle = t * Mathf.PI * 1.15f - phase + r * Mathf.PI * 2f / ribbons.Length;
                float spread = radius * (1f - t) * Mathf.Lerp(1f, 0.55f, charge);
                ribbons[r].SetPosition(i, new Vector3(Mathf.Sin(angle) * spread, Mathf.Lerp(height * 0.04f, height * focusHeight, t), 0f));
            }
        }
        DrawRing(footRing, radius, radius * 0.2f, 0.02f);
        footRing.widthMultiplier = footRingWidth * ribbonFade * 0.6f;
        float flash = completed ? Mathf.Clamp01(1f - endAge / completionFlashDuration) : 0f;
        float coreScale = IsFinishing
            ? (completed ? Mathf.Lerp(completionFlashScale, 1f, 1f - flash) : 1f)
            : Mathf.Lerp(0.55f, 1f, charge) * (1f + Mathf.Sin(age * 8f) * 0.035f);
        core.transform.localPosition = Vector3.up * height * focusHeight;
        core.transform.localScale = Vector3.one * coreSize * coreScale;
        core.GetPropertyBlock(properties);
        properties.SetFloat("_Opacity", opacity * coreOpacity * (IsFinishing ? (completed ? flash : 1f) : fadeIn));
        core.SetPropertyBlock(properties);
        if (completed)
        {
            float t = Mathf.Clamp01(endAge / Mathf.Max(0.01f, completionFlashDuration * 1.8f));
            float size = Mathf.Lerp(radius * 0.5f, pulseRadius, t);
            healPulse.widthMultiplier = pulseWidth * (1f - t);
            DrawRing(healPulse, size, size * 0.75f, height * focusHeight);
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
        if (coreMesh != null) Destroy(coreMesh);
        if (profile == null) return;
        foreach (var component in profile.components) Destroy(component);
        Destroy(profile);
    }
}
