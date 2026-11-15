using UnityEngine;

/// <summary>Lets already emitted red embers fade after a projectile hits or expires.</summary>
public sealed class ScorchedEarthVisualResidue : MonoBehaviour
{
    private Mesh quad;
    private MeshRenderer flash;
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private float age;
    private float duration;
    private float flashDuration;
    private bool ending;

    public void Initialize(Mesh mesh, MeshRenderer hitFlash)
    {
        properties = new MaterialPropertyBlock();
        quad = mesh;
        flash = hitFlash;
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Finish(float fadeDuration, float hitFlashDuration)
    {
        if (ending) return;
        ending = true;
        duration = Mathf.Max(0.01f, fadeDuration);
        flashDuration = hitFlashDuration;
        flash.enabled = hitFlashDuration > 0f;
    }

    private void Update()
    {
        if (!ending) return;
        age += Time.deltaTime;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            float fade = renderer == flash
                ? (flashDuration > 0f ? Mathf.Clamp01(1f - age / flashDuration) : 0f)
                : Mathf.Clamp01(1f - age / duration);
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_Opacity", fade);
            renderer.SetPropertyBlock(properties);
        }
        if (age >= duration) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (quad != null) Destroy(quad);
    }
}
