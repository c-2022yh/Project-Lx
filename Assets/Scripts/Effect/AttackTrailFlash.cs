using UnityEngine;

// Adds a short unlit flash over the animated sword trail.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AttackTrailFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField, Range(0.03f, 0.15f)] private float duration = 0.07f;
    [SerializeField, Min(0f)] private float brightness = 2f;
    [SerializeField] private Color tint = Color.white;

    private SpriteRenderer source;
    private SpriteRenderer overlay;
    private float elapsed;

    // Use a renderer without any collider, animator or damage component.
    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        GameObject child = new GameObject("AttackTrailFlashRenderer");
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        overlay = child.AddComponent<SpriteRenderer>();
        overlay.sharedMaterial = flashMaterial;
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
        float remaining = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
        overlay.enabled = source.enabled && flashMaterial != null && remaining > 0f;
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

        Color color = source.color * tint;
        color.r *= brightness;
        color.g *= brightness;
        color.b *= brightness;
        color.a *= remaining * remaining;
        overlay.color = color;
    }

    // Clear the overlay when interrupted or returned to a pool.
    private void OnDisable()
    {
        if (overlay != null) overlay.enabled = false;
    }
}
