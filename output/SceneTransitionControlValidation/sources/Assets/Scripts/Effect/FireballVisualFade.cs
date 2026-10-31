using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Keeps the emitted tail alive after the projectile is destroyed.</summary>
public sealed class FireballVisualFade : MonoBehaviour
{
    private Light2D nearbyLight;
    private float baseIntensity;
    private Volume bloomVolume;
    private VolumeProfile bloomProfile;
    private bool ending;
    private float elapsed;
    private float lifetime;
    private float flashDuration;
    private float flashMultiplier;

    public void Initialize(Light2D lightSource, bool bloomEnabled, float intensity, float threshold)
    {
        nearbyLight = lightSource;
        baseIntensity = lightSource.intensity;
        if (!bloomEnabled) return;
        // Matches the existing ScreenEffectsCamera post-processing path.
        gameObject.layer = 0;
        bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = bloomProfile.Add<Bloom>();
        bloom.intensity.Override(intensity);
        bloom.threshold.Override(threshold);
        bloom.scatter.Override(0.65f);
        bloomVolume = gameObject.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = 10f; // Awakening's priority 101 remains authoritative.
        bloomVolume.sharedProfile = bloomProfile;
        bloomVolume.enabled = false;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    public void Finish(float duration, float impactDuration, float impactMultiplier)
    {
        if (ending) return;
        ending = true;
        lifetime = duration;
        flashDuration = impactDuration;
        flashMultiplier = impactMultiplier;
        nearbyLight.intensity = flashDuration > 0f ? baseIntensity * flashMultiplier : 0f;
    }

    private void Update()
    {
        if (!ending) return;
        elapsed += Time.deltaTime;
        if (nearbyLight != null)
            nearbyLight.intensity = flashDuration > 0f
                ? baseIntensity * flashMultiplier * Mathf.Clamp01(1f - elapsed / flashDuration) : 0f;
        if (bloomVolume != null) bloomVolume.weight = Mathf.Clamp01(1f - elapsed / lifetime);
        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume == null) return;
        var screenEffects = camera.GetComponent<ScreenEffectsCamera>();
        bloomVolume.enabled = camera.cameraType == CameraType.Game && screenEffects != null && screenEffects.isActiveAndEnabled;
    }

    private void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (bloomVolume != null) bloomVolume.enabled = false;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        if (bloomProfile != null)
        {
            foreach (var component in bloomProfile.components) Destroy(component);
            Destroy(bloomProfile);
        }
    }
}
