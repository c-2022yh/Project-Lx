using UnityEngine;
using UnityEngine.Rendering.Universal;

// Flashes with the attack effect, then quickly fades its local light.
[DisallowMultipleComponent]
[RequireComponent(typeof(Light2D))]
public sealed class AttackLightFlash : MonoBehaviour
{
    [SerializeField, Range(0.05f, 0.15f)] private float duration = 0.1f;
    [SerializeField, Min(0f)] private float peakIntensity = 1.5f;

    private Light2D flashLight;
    private float elapsed;

    // Reset on every spawn or pool activation.
    private void OnEnable()
    {
        flashLight = GetComponent<Light2D>();
        elapsed = 0f;
        flashLight.intensity = peakIntensity;
        flashLight.enabled = true;
    }

    // A squared falloff makes the initial flash sharp and its tail soft.
    private void Update()
    {
        elapsed += Time.deltaTime;
        float remaining = 1f - Mathf.Clamp01(elapsed / Mathf.Clamp(duration, 0.05f, 0.15f));
        flashLight.intensity = peakIntensity * remaining * remaining;
        if (remaining <= 0f) flashLight.enabled = false;
    }

    // Prevent a lingering light when the effect is interrupted or disabled.
    private void OnDisable()
    {
        if (flashLight == null) return;
        flashLight.intensity = 0f;
        flashLight.enabled = false;
    }
}
