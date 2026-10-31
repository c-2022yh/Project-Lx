using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Runtime visuals only. ProjectileHitbox still owns motion and damage.</summary>
[DisallowMultipleComponent]
public sealed class FireballVisual : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Material particleMaterial;
    [SerializeField, ColorUsage(true, true)] private Color coreColor = new Color(4f, 2.2f, 0.6f, 1f);
    [SerializeField, ColorUsage(true, true)] private Color emberColor = new Color(2.4f, 0.45f, 0.06f, 1f);
    [SerializeField, Min(0.01f)] private float coreSize = 0.7f;
    [SerializeField, Min(0.01f)] private float emberSize = 0.18f;
    [SerializeField] private string sortingLayer = "";
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("Short Ember Tail")]
    [SerializeField, Range(0.02f, 1f)] private float tailLifetime = 0.14f;
    [SerializeField, Range(1f, 150f)] private float embersPerSecond = 65f;
    [SerializeField, Min(0f)] private float emberDrift = 0.35f;

    [Header("Nearby Light")]
    [SerializeField] private Color lightColor = new Color(1f, 0.42f, 0.08f);
    [SerializeField, Min(0f)] private float lightIntensity = 1.2f;
    [SerializeField, Min(0.01f)] private float lightRadius = 1.8f;
    [Tooltip("Empty means all Sorting Layers. Only lit materials receive this light.")]
    [SerializeField] private string[] lightSortingLayers = new string[0];

    [Header("Impact Flash")]
    [SerializeField, Min(0.01f)] private float impactSize = 1.1f;
    [SerializeField, Range(0.02f, 0.5f)] private float impactDuration = 0.12f;
    [SerializeField, Min(0f)] private float impactLightMultiplier = 1.6f;

    [Header("Bloom")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField, Range(0f, 3f)] private float bloomIntensity = 0.6f;
    [SerializeField, Min(0f)] private float bloomThreshold = 1.1f;

    private SpriteRenderer originalSprite;
    private bool spriteWasEnabled;
    private GameObject rig;
    private ParticleSystem core;
    private ParticleSystem tail;
    private ParticleSystem flash;
    private FireballVisualFade fade;

    private void OnEnable()
    {
        if (particleMaterial == null)
        {
            Debug.LogWarning("FireballVisual needs a particle material.", this);
            return;
        }

        originalSprite = GetComponent<SpriteRenderer>();
        spriteWasEnabled = originalSprite != null && originalSprite.enabled;
        int layerId = originalSprite != null ? originalSprite.sortingLayerID : 0;
        if (!string.IsNullOrEmpty(sortingLayer)) layerId = SortingLayer.NameToID(sortingLayer);
        int order = (originalSprite != null ? originalSprite.sortingOrder : 0) + sortingOrderOffset;

        // A separate world-space rig ignores the projectile's spin and prefab scale.
        rig = new GameObject("Fireball Particles (Runtime)");
        rig.transform.position = transform.position;
        core = CreateParticles("Luminous Core", coreSize, 0.07f, coreColor, 0f, layerId, order + 1);
        var coreMain = core.main;
        coreMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        var coreEmission = core.emission;
        coreEmission.rateOverTime = 60f;
        core.Play();

        tail = CreateParticles("World Space Embers", emberSize, tailLifetime, emberColor, emberDrift, layerId, order);
        var tailEmission = tail.emission;
        tailEmission.rateOverTime = embersPerSecond;
        var tailShape = tail.shape;
        tailShape.enabled = true;
        tailShape.shapeType = ParticleSystemShapeType.Circle;
        tailShape.radius = coreSize * 0.18f;
        tail.Play();

        flash = CreateParticles("Impact Flash", impactSize, impactDuration, coreColor, 0f, layerId, order + 2);

        var lightObject = new GameObject("Nearby Light");
        lightObject.transform.SetParent(rig.transform, false);
        var nearbyLight = lightObject.AddComponent<Light2D>();
        nearbyLight.lightType = Light2D.LightType.Point;
        nearbyLight.blendStyleIndex = 0;
        nearbyLight.color = lightColor;
        nearbyLight.intensity = lightIntensity;
        nearbyLight.pointLightInnerRadius = lightRadius * 0.15f;
        nearbyLight.pointLightOuterRadius = lightRadius;
        nearbyLight.shadowsEnabled = false;
        var ids = new List<int>();
        foreach (var layer in SortingLayer.layers)
            if (lightSortingLayers == null || lightSortingLayers.Length == 0 ||
                System.Array.IndexOf(lightSortingLayers, layer.name) >= 0)
                ids.Add(layer.id);
        nearbyLight.targetSortingLayers = ids.ToArray();

        fade = rig.AddComponent<FireballVisualFade>();
        fade.Initialize(nearbyLight, enableBloom, bloomIntensity, bloomThreshold);
        if (originalSprite != null) originalSprite.enabled = false;
    }

    private ParticleSystem CreateParticles(string effectName, float size, float lifetime,
        Color color, float speed, int layerId, int order)
    {
        var child = new GameObject(effectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(rig.transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        // Particle vertex colors are packed; carry HDR brightness in the shader instead.
        float brightness = Mathf.Max(1f, Mathf.Max(color.r, Mathf.Max(color.g, color.b)));
        main.startColor = new Color(color.r / brightness, color.g / brightness, color.b / brightness, color.a);
        main.maxParticles = 128;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        var shape = particles.shape;
        shape.enabled = false;
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var colorOverLife = particles.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetFloat("_Brightness", brightness);
        renderer.SetPropertyBlock(properties);
        renderer.sortingLayerID = layerId;
        renderer.sortingOrder = order;
        return particles;
    }

    private void LateUpdate()
    {
        if (rig != null) rig.transform.position = transform.position;
    }

    /// <summary>Call only for a confirmed damage/wall hit, before destroying the projectile.</summary>
    public void PlayImpact()
    {
        Release(true);
    }

    private void OnDisable()
    {
        Release(false);
        if (originalSprite != null) originalSprite.enabled = spriteWasEnabled;
    }

    private void Release(bool impact)
    {
        if (rig == null) return;
        rig.transform.position = transform.position;
        core.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        tail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (impact)
        {
            flash.Play();
            flash.Emit(1);
        }
        fade.Finish(Mathf.Max(tailLifetime, impact ? impactDuration : 0f) + 0.05f,
            impact ? impactDuration : 0f, impactLightMultiplier);
        rig = null;
    }
}
