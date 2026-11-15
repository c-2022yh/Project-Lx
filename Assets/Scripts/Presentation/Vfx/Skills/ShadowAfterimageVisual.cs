using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>A frozen player sprite; its root remains the skill's teleport anchor.</summary>
[DisallowMultipleComponent]
public sealed class ShadowAfterimageVisual : MonoBehaviour
{
    [SerializeField] private Material silhouetteMaterial;
    [Header("Black Silhouette")]
    [SerializeField, Range(0f, 1f)] private float bodyOpacity = 0.92f;
    [Header("Glowing Rim")]
    [SerializeField, ColorUsage(true, true)] private Color rimColor = new Color(0.38f, 0.22f, 1f, 1f);
    [SerializeField, Range(0.5f, 5f)] private float rimPixels = 1.25f;
    [SerializeField, Min(0f)] private float rimIntensity = 3f;
    [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.18f;
    [SerializeField, Min(0f)] private float pulseFrequency = 0.65f;
    [Header("Teleport Fade")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
    [SerializeField, Min(1f)] private float consumeBrightness = 2f;
    [Header("Bloom")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField, Range(0f, 3f)] private float bloomIntensity = 0.35f;
    [SerializeField, Min(0f)] private float bloomThreshold = 1.1f;

    private SpriteRenderer silhouette;
    private MaterialPropertyBlock properties;
    private Vector4 uvRect;
    private Volume bloomVolume;
    private VolumeProfile bloomProfile;
    private float age;
    private bool consuming;
    private float fadeElapsed;

    public void Capture(SpriteRenderer source)
    {
        if (silhouette != null) return;
        var template = GetComponent<SpriteRenderer>();
        if (source == null) source = template;
        if (source == null || source.sprite == null || silhouetteMaterial == null)
        {
            Debug.LogWarning("ShadowAfterimageVisual needs a sprite source and silhouette material.", this);
            return;
        }

        var visual = new GameObject("Frozen Player Silhouette");
        visual.layer = gameObject.layer;
        visual.transform.position = source.transform.position;
        visual.transform.rotation = source.transform.rotation;
        visual.transform.localScale = source.transform.lossyScale;
        visual.transform.SetParent(transform, true);
        silhouette = visual.AddComponent<SpriteRenderer>();
        silhouette.sprite = source.sprite;
        silhouette.flipX = source.flipX;
        silhouette.flipY = source.flipY;
        silhouette.drawMode = source.drawMode;
        silhouette.size = source.size;
        silhouette.spriteSortPoint = source.spriteSortPoint;
        silhouette.maskInteraction = source.maskInteraction;
        silhouette.sortingLayerID = template != null ? template.sortingLayerID : source.sortingLayerID;
        silhouette.sortingOrder = template != null ? template.sortingOrder : source.sortingOrder;
        silhouette.color = Color.white;
        silhouette.sharedMaterial = silhouetteMaterial;
        Vector2 min = Vector2.one, max = Vector2.zero;
        foreach (Vector2 uv in source.sprite.uv)
        {
            min = Vector2.Min(min, uv);
            max = Vector2.Max(max, uv);
        }
        uvRect = new Vector4(min.x, min.y, max.x, max.y);
        properties = new MaterialPropertyBlock();
        if (template != null) template.enabled = false;
        CreateBloom();
        ApplyAppearance(1f, rimIntensity);
    }

    /// <summary>Logical use completes immediately; only the visual lingers.</summary>
    public void Consume()
    {
        if (consuming) return;
        consuming = true;
        fadeElapsed = 0f;
        if (silhouette == null) Destroy(gameObject);
        else ApplyAppearance(1f, rimIntensity * consumeBrightness);
    }

    private void Update()
    {
        if (silhouette == null) return;
        age += Time.deltaTime;
        if (consuming)
        {
            fadeElapsed += Time.deltaTime;
            float remaining = Mathf.Clamp01(1f - fadeElapsed / fadeDuration);
            ApplyAppearance(remaining, rimIntensity * Mathf.Lerp(1f, consumeBrightness, remaining));
            if (bloomVolume != null) bloomVolume.weight = remaining;
            if (remaining <= 0f) Destroy(gameObject);
            return;
        }
        float pulse = 1f + Mathf.Sin(age * pulseFrequency * Mathf.PI * 2f) * pulseAmount;
        ApplyAppearance(1f, rimIntensity * pulse);
    }

    private void ApplyAppearance(float opacity, float intensity)
    {
        properties.SetColor("_RimColor", rimColor);
        properties.SetFloat("_RimIntensity", intensity);
        properties.SetFloat("_RimPixels", rimPixels);
        properties.SetFloat("_BodyOpacity", bodyOpacity);
        properties.SetFloat("_Fade", opacity);
        properties.SetVector("_SpriteUVRect", uvRect);
        silhouette.SetPropertyBlock(properties);
    }

    private void CreateBloom()
    {
        if (!enableBloom) return;
        var volumeObject = new GameObject("Shadow Bloom (Runtime)");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.layer = 0;
        bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = bloomProfile.Add<Bloom>();
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(0.65f);
        bloomVolume = volumeObject.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = 9f;
        bloomVolume.sharedProfile = bloomProfile;
        bloomVolume.enabled = false;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume == null || !isActiveAndEnabled) return;
        var marker = camera.GetComponent<ScreenEffectsCamera>();
        bloomVolume.enabled = camera.cameraType == CameraType.Game && marker != null && marker.isActiveAndEnabled;
    }

    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume != null) bloomVolume.enabled = false;
    }

    private void OnDisable()
    {
        if (bloomVolume != null) bloomVolume.enabled = false;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        if (bloomProfile == null) return;
        foreach (var component in bloomProfile.components) Destroy(component);
        Destroy(bloomProfile);
    }
}
