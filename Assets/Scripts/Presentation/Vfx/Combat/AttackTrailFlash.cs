using System.Collections.Generic;
using UnityEngine;

// Adds a short unlit glow around the animated sword trail's silhouette.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AttackTrailFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField, Range(0.03f, 0.3f)] private float duration = 0.2f;
    [SerializeField, Min(0f)] private float holdTime = 0.06f;
    [SerializeField, Min(0f)] private float brightness = 2f;
    [SerializeField] private Color tint = Color.white;
    [SerializeField, Range(0.5f, 8f)] private float edgeWidth = 3f;
    [Tooltip("Keep the edge glow visible for the effect's entire lifetime to check rendering.")]
    [SerializeField] private bool keepVisibleForTesting;

    private SpriteRenderer source;
    private SpriteRenderer overlay;
    private float elapsed;
    private float brightnessMultiplier = 1f;
    private MaterialPropertyBlock glowProperties;
    private readonly Dictionary<Sprite, Sprite> glowSprites = new Dictionary<Sprite, Sprite>();
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int GlowTexelSizeId = Shader.PropertyToID("_GlowTexelSize");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

    // Use a renderer without any collider, animator or damage component.
    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        GameObject child = new GameObject("AttackTrailFlashRenderer");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        overlay = child.AddComponent<SpriteRenderer>();
        overlay.sharedMaterial = flashMaterial;
        glowProperties = new MaterialPropertyBlock();
    }

    // Per-instance adjustment for relic-generated trails; the shared material stays untouched.
    public void SetBrightnessMultiplier(float multiplier)
    {
        brightnessMultiplier = Mathf.Max(0f, multiplier);
        RefreshOverlay();
    }

    // Restart the flash when spawned or reactivated.
    private void OnEnable()
    {
        elapsed = 0f;
        RefreshOverlay();
    }

    // Copy after animation and facing updates so both trails stay aligned.
    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        RefreshOverlay();
    }

    // Fade only the added flash, leaving the original animation intact.
    private void RefreshOverlay()
    {
        if (overlay == null) return;
        float lifetime = Mathf.Max(0.001f, duration);
        float hold = Mathf.Clamp(holdTime, 0f, lifetime - 0.001f);
        float remaining = keepVisibleForTesting ? 1f :
            1f - Mathf.Clamp01((elapsed - hold) / Mathf.Max(0.001f, lifetime - hold));
        overlay.enabled = source.enabled && source.sprite != null && flashMaterial != null && remaining > 0f;
        if (!overlay.enabled) return;

        overlay.sprite = GetGlowSprite(source.sprite);
        overlay.flipX = source.flipX;
        overlay.flipY = source.flipY;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder + 1;
        overlay.maskInteraction = source.maskInteraction;
        overlay.spriteSortPoint = source.spriteSortPoint;
        overlay.drawMode = source.drawMode;
        overlay.size = source.size;
        // Supply texture data explicitly instead of relying on renderer-injected values.
        Texture2D texture = source.sprite.texture;
        overlay.GetPropertyBlock(glowProperties);
        glowProperties.SetTexture(MainTexId, texture);
        glowProperties.SetVector(GlowTexelSizeId,
            new Vector4(1f / texture.width, 1f / texture.height, texture.width, texture.height));
        glowProperties.SetFloat(GlowIntensityId, Mathf.Max(0f, brightness) * brightnessMultiplier);
        glowProperties.SetFloat(GlowWidthId, Mathf.Clamp(edgeWidth, 0.5f, 8f));
        overlay.SetPropertyBlock(glowProperties);

        Color color = source.color * tint;
        color.a *= remaining;
        overlay.color = color;
    }

    // Full-rect padded geometry leaves room for glow outside a tight sprite mesh.
    private Sprite GetGlowSprite(Sprite sprite)
    {
        if (sprite == null || sprite.packed) return sprite;
        if (glowSprites.TryGetValue(sprite, out Sprite cached)) return cached;

        Rect rect = sprite.rect;
        const float padding = 10f;
        float left = Mathf.Max(0f, rect.xMin - padding);
        float bottom = Mathf.Max(0f, rect.yMin - padding);
        Rect padded = Rect.MinMaxRect(left, bottom,
            Mathf.Min(sprite.texture.width, rect.xMax + padding),
            Mathf.Min(sprite.texture.height, rect.yMax + padding));
        Vector2 pivot = new Vector2(
            (sprite.pivot.x + rect.xMin - left) / padded.width,
            (sprite.pivot.y + rect.yMin - bottom) / padded.height);
        Sprite glowSprite = Sprite.Create(sprite.texture, padded, pivot,
            sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        glowSprite.name = sprite.name + "_EdgeGlow";
        glowSprites.Add(sprite, glowSprite);
        return glowSprite;
    }

    // Release the per-effect geometry without touching imported sprite assets.
    private void OnDestroy()
    {
        foreach (Sprite sprite in glowSprites.Values) Destroy(sprite);
        glowSprites.Clear();
    }

    // Clear the overlay when interrupted or returned to a pool.
    private void OnDisable()
    {
        if (overlay != null) overlay.enabled = false;
    }
}
