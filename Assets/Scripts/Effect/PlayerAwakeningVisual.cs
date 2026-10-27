using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Adds glow and a flowing aura without replacing the animated sprite's material or tint.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAwakening), typeof(PlayerHealth))]
public sealed class PlayerAwakeningVisual : MonoBehaviour
{
    [Header("Orb Relic Colors")]
    [Tooltip("Use the equipped orb relic's Awakening Colors when enabled on its SO. Otherwise use the colors below.")]
    [SerializeField] private bool useEquippedOrbColors = true;

    [Header("Sprite Glow")]
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private Material glowMaterial;
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField, Min(0f)] private float glowIntensity = 4f;
    [SerializeField, Range(0.5f, 8f)] private float outlinePixels = 2f;
    [SerializeField, Range(0f, 0.3f)] private float bodyGlow = 0.025f;

    [Header("Flowing Aura")]
    [SerializeField] private bool enableAura = true;
    [SerializeField] private Material auraMaterial;
    [SerializeField, ColorUsage(false, true)] private Color auraColor = new Color(0.15f, 0.45f, 1f);
    [SerializeField, Min(0f)] private float auraIntensity = 2.5f;
    [SerializeField, Range(0f, 1f)] private float auraOpacity = 0.4f;
    [Tooltip("Canvas width and aura reach in Player-root units. Use Fan Angle for spread; allow enough canvas width for the fan.")]
    [SerializeField] private Vector2 auraSize = new Vector2(5.4f, 4.2f);
    [Tooltip("Fan origin near the feet, relative to Player root. Follows the player in the air too.")]
    [SerializeField] private Vector2 auraOffset = new Vector2(-0.15f, -1.65f);
    [SerializeField, Range(20f, 140f)] private float auraFanAngle = 80f;
    [SerializeField, Range(3f, 16f)] private float auraStreamCount = 7f;
    [SerializeField, Min(0f)] private float auraRiseSpeed = 0.65f;
    [SerializeField, Range(1f, 12f)] private float auraNoiseScale = 5f;
    [SerializeField, Range(0f, 1f)] private float auraTurbulence = 0.65f;

#if UNITY_EDITOR
    [Header("Preview (Play Mode Only)")]
    [Tooltip("Preview visuals without energy cost or combat bonuses. Editor Play Mode only.")]
    [SerializeField] private bool previewInPlayMode;
#endif

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float fadeIn = 0.3f;
    [SerializeField, Min(0.01f)] private float fadeOut = 0.3f;
    [SerializeField, Min(1f)] private float entryPeak = 1.8f;
    [SerializeField, Min(0.01f)] private float entrySettle = 0.35f;
    [SerializeField, Range(0f, 0.4f)] private float pulseAmount = 0.1f;
    [SerializeField, Min(0f)] private float pulseFrequency = 0.8f;

    [Header("Nearby Light")]
    [SerializeField] private Color lightColor = new Color(0.3f, 0.55f, 1f);
    [SerializeField, Min(0f)] private float lightIntensity = 0.8f;
    [SerializeField, Min(0.1f)] private float lightRadius = 2.5f;
    [SerializeField] private Vector2 lightOffset = new Vector2(0f, -0.3f);
    [Tooltip("Sorting Layer names, not physics layers. Only lit materials receive this light.")]
    [SerializeField] private string[] lightSortingLayers = { "Ground" };

    [Header("Bloom (Awakening Only)")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField, Min(0f)] private float bloomIntensity = 0.6f;
    [SerializeField, Min(0f)] private float bloomThreshold = 1.1f;
    [SerializeField, Range(0f, 1f)] private float bloomScatter = 0.65f;

    private PlayerAwakening awakening;
    private PlayerHealth health;
    private PlayerRelicManager relicManager;
    private Color resolvedGlowColor, resolvedAuraColor, resolvedLightColor;
    private SpriteRenderer overlay;
    private MeshRenderer aura;
    private Mesh auraMesh;
    private Light2D nearbyLight;
    private Volume bloomVolume;
    private VolumeProfile bloomProfile;
    private Bloom bloom;
    private MaterialPropertyBlock properties;
    private MaterialPropertyBlock auraProperties;
    private Sprite previousSprite;
    private Vector4 uvRect;
    private float weight, peakRemaining, clock;
    private bool wasAwakened;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int OutlinePixelsId = Shader.PropertyToID("_OutlinePixels");
    private static readonly int BodyGlowId = Shader.PropertyToID("_BodyGlow");
    private static readonly int UvRectId = Shader.PropertyToID("_SpriteUVRect");
    private static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
    private static readonly int AuraIntensityId = Shader.PropertyToID("_AuraIntensity");
    private static readonly int AuraOpacityId = Shader.PropertyToID("_AuraOpacity");
    private static readonly int AuraTimeId = Shader.PropertyToID("_AuraTime");
    private static readonly int RiseSpeedId = Shader.PropertyToID("_RiseSpeed");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int TurbulenceId = Shader.PropertyToID("_Turbulence");
    private static readonly int FanAngleId = Shader.PropertyToID("_FanAngle");
    private static readonly int StreamCountId = Shader.PropertyToID("_StreamCount");
    private static readonly int AuraAspectId = Shader.PropertyToID("_AuraAspect");

    private void Awake()
    {
        awakening = GetComponent<PlayerAwakening>();
        health = GetComponent<PlayerHealth>();
        relicManager = GetComponent<PlayerRelicManager>();
        if (source == null) source = GetComponentInChildren<SpriteRenderer>();
        if (source == null || glowMaterial == null)
        {
            Debug.LogWarning("PlayerAwakeningVisual needs a source sprite and glow material.", this);
            enabled = false;
            return;
        }

        var glowObject = new GameObject("Awakening Glow (Runtime)");
        glowObject.layer = source.gameObject.layer;
        glowObject.transform.SetParent(source.transform, false);
        overlay = glowObject.AddComponent<SpriteRenderer>();
        overlay.sharedMaterial = glowMaterial;
        overlay.enabled = false;
        properties = new MaterialPropertyBlock();
        CreateAura();

        var lightObject = new GameObject("Awakening Light (Runtime)");
        lightObject.transform.SetParent(transform, false);
        nearbyLight = lightObject.AddComponent<Light2D>();
        nearbyLight.lightType = Light2D.LightType.Point;
        nearbyLight.blendStyleIndex = 0;
        nearbyLight.shadowsEnabled = true;
        nearbyLight.shadowIntensity = 1f;
        nearbyLight.falloffIntensity = 0.5f;
        ConfigureLightLayers();
        nearbyLight.enabled = false;

        var volumeObject = new GameObject("Awakening Bloom (Runtime)");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.layer = 0; // ScreenEffectsCamera includes Default in its Volume mask.
        bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        bloom = bloomProfile.Add<Bloom>();
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        bloomVolume = volumeObject.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = 101f;
        bloomVolume.sharedProfile = bloomProfile;
        bloomVolume.enabled = false;
    }

    private void OnEnable()
    {
        if (overlay == null) return;
        health.OnDied += ResetVisuals;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= ResetVisuals;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        ResetVisuals();
    }

    private void LateUpdate()
    {
        if (overlay == null) return;
        if (health.IsDead) { ResetVisuals(); return; }

        // IsAwakening covers the intro before OnAwakeningStarted is raised.
        bool active = awakening.isActiveAndEnabled && (awakening.IsAwakening || awakening.IsAwakened);
        bool awakened = awakening.isActiveAndEnabled && awakening.IsAwakened;
#if UNITY_EDITOR
        active |= previewInPlayMode;
        awakened |= previewInPlayMode;
#endif
        if (awakened && !wasAwakened) peakRemaining = entrySettle;
        wasAwakened = awakened;
        float dt = Time.deltaTime;
        weight = Mathf.MoveTowards(weight, active ? 1f : 0f,
            dt / Mathf.Max(0.01f, active ? fadeIn : fadeOut));
        clock = active || weight > 0f ? clock + dt : 0f;
        peakRemaining = Mathf.Max(0f, peakRemaining - dt);
        float peak = Mathf.Lerp(1f, entryPeak, peakRemaining / Mathf.Max(0.01f, entrySettle));
        float pulse = 1f + Mathf.Sin(clock * pulseFrequency * Mathf.PI * 2f) * pulseAmount;
        float strength = weight * peak * pulse;

        ResolveRelicColors();
        UpdateOverlay(strength);
        UpdateAura(strength);
        nearbyLight.transform.position = source.bounds.center + (Vector3)lightOffset;
        nearbyLight.color = resolvedLightColor;
        nearbyLight.intensity = lightIntensity * strength;
        nearbyLight.pointLightOuterRadius = Mathf.Max(0.1f, lightRadius);
        nearbyLight.pointLightInnerRadius = nearbyLight.pointLightOuterRadius * 0.15f;
        nearbyLight.enabled = strength > 0.001f;
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.value = bloomThreshold;
        bloom.scatter.value = bloomScatter;
        bloomVolume.weight = weight;
    }

    private void ResolveRelicColors()
    {
        // Do not overwrite the Inspector defaults. Unequip restores them immediately,
        // and reading the SO each frame also supports color tuning during Play Mode.
        resolvedGlowColor = glowColor;
        resolvedAuraColor = auraColor;
        resolvedLightColor = lightColor;
        if (!useEquippedOrbColors || relicManager == null) return;
        RelicData relic = relicManager.EquippedOrbRelic;
        if (relic == null || !relic.HasAwakeningColors) return;
        resolvedGlowColor = relic.AwakeningGlowColor;
        resolvedAuraColor = relic.AwakeningAuraColor;
        resolvedLightColor = relic.AwakeningLightColor;
    }

    private void CreateAura()
    {
        // A quad with a procedural shader needs no sprite, texture, or collider.
        var holder = new GameObject("Awakening Aura (Runtime)");
        holder.layer = source.gameObject.layer;
        holder.transform.SetParent(transform, false);
        auraMesh = new Mesh { name = "Awakening Aura Quad (Runtime)" };
        auraMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        auraMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        auraMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        auraMesh.RecalculateBounds();
        holder.AddComponent<MeshFilter>().sharedMesh = auraMesh;
        aura = holder.AddComponent<MeshRenderer>();
        aura.shadowCastingMode = ShadowCastingMode.Off;
        aura.receiveShadows = false;
        aura.lightProbeUsage = LightProbeUsage.Off;
        aura.reflectionProbeUsage = ReflectionProbeUsage.Off;
        aura.enabled = false;
        auraProperties = new MaterialPropertyBlock();
    }

    private void UpdateAura(float strength)
    {
        aura.enabled = enableAura && auraMaterial != null && strength > 0.001f
            && source.enabled && source.gameObject.activeInHierarchy
            && source.sprite != null && source.color.a > 0f;
        if (!aura.enabled) return;
        aura.sharedMaterial = auraMaterial;
        aura.sortingLayerID = source.sortingLayerID;
        aura.sortingOrder = source.sortingOrder - 1;
        // The quad's bottom-center is the fan origin, independent of pose and flipX.
        float width = Mathf.Max(0.1f, auraSize.x);
        float height = Mathf.Max(0.1f, auraSize.y);
        aura.transform.localPosition = new Vector3(auraOffset.x, auraOffset.y + height * 0.5f, 0f);
        aura.transform.localScale = new Vector3(width, height, 1f);
        auraProperties.SetColor(AuraColorId, resolvedAuraColor);
        auraProperties.SetFloat(AuraIntensityId, auraIntensity * strength);
        auraProperties.SetFloat(AuraOpacityId, auraOpacity * source.color.a);
        auraProperties.SetFloat(AuraTimeId, clock);
        auraProperties.SetFloat(RiseSpeedId, auraRiseSpeed);
        auraProperties.SetFloat(NoiseScaleId, auraNoiseScale);
        auraProperties.SetFloat(TurbulenceId, auraTurbulence);
        auraProperties.SetFloat(FanAngleId, Mathf.Clamp(auraFanAngle, 20f, 140f));
        auraProperties.SetFloat(StreamCountId, Mathf.Clamp(auraStreamCount, 3f, 16f));
        auraProperties.SetFloat(AuraAspectId, width / height);
        aura.SetPropertyBlock(auraProperties);
    }

    private void UpdateOverlay(float strength)
    {
        overlay.enabled = strength > 0.001f && source.enabled
            && source.gameObject.activeInHierarchy && source.sprite != null && source.color.a > 0f;
        if (!overlay.enabled) return;
        overlay.sprite = source.sprite;
        overlay.flipX = source.flipX;
        overlay.flipY = source.flipY;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder + 1;
        overlay.maskInteraction = source.maskInteraction;
        overlay.spriteSortPoint = source.spriteSortPoint;
        overlay.drawMode = source.drawMode;
        overlay.size = source.size;
        overlay.color = new Color(1f, 1f, 1f, source.color.a);
        if (previousSprite != source.sprite)
        {
            previousSprite = source.sprite;
            // Sprite UVs also work for tight meshes and atlas-packed sprites.
            Vector2 min = Vector2.one, max = Vector2.zero;
            foreach (Vector2 uv in previousSprite.uv)
            {
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }
            uvRect = new Vector4(min.x, min.y, max.x, max.y);
        }
        properties.SetColor(GlowColorId, resolvedGlowColor);
        properties.SetFloat(GlowIntensityId, glowIntensity * strength);
        properties.SetFloat(OutlinePixelsId, outlinePixels);
        properties.SetFloat(BodyGlowId, bodyGlow);
        properties.SetVector(UvRectId, uvRect);
        overlay.SetPropertyBlock(properties);
    }

    private void ConfigureLightLayers()
    {
        var ids = new List<int>();
        if (lightSortingLayers != null)
            foreach (string layerName in lightSortingLayers)
                foreach (SortingLayer layer in SortingLayer.layers)
                    if (layer.name == layerName && !ids.Contains(layer.id)) ids.Add(layer.id);
        nearbyLight.targetSortingLayers = ids.ToArray();
    }

    private void OnValidate()
    {
        if (nearbyLight != null) ConfigureLightLayers();
    }

    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume == null) return;
        var marker = camera.GetComponent<ScreenEffectsCamera>();
        bloomVolume.enabled = enableBloom && weight > 0.001f
            && camera.cameraType == CameraType.Game && marker != null && marker.isActiveAndEnabled;
    }

    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume != null) bloomVolume.enabled = false;
    }

    private void ResetVisuals()
    {
        weight = peakRemaining = clock = 0f;
        wasAwakened = false;
        if (overlay != null) overlay.enabled = false;
        if (aura != null) aura.enabled = false;
        if (nearbyLight != null) nearbyLight.enabled = false;
        if (bloomVolume != null) { bloomVolume.weight = 0f; bloomVolume.enabled = false; }
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay.gameObject);
        if (aura != null) Destroy(aura.gameObject);
        if (auraMesh != null) Destroy(auraMesh);
        if (nearbyLight != null) Destroy(nearbyLight.gameObject);
        if (bloomVolume != null) Destroy(bloomVolume.gameObject);
        if (bloom != null) Destroy(bloom);
        if (bloomProfile != null) Destroy(bloomProfile);
    }
}
