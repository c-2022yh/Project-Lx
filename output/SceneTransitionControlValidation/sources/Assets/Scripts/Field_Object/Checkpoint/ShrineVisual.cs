using UnityEngine;

/// <summary>Bounded lantern glow; coordinates are relative to the shrine sprite bounds.</summary>
[DisallowMultipleComponent]
public sealed class ShrineVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer shrineRenderer;
    [SerializeField] private Material glowMaterial;
    [Header("Opening (relative to sprite width / height)")]
    [SerializeField] private Vector2 center = new Vector2(0.01f, 0.095f);
    [SerializeField] private Vector2 size = new Vector2(0.44f, 0.34f);
    [Header("Light")]
    [ColorUsage(false, true)]
    [SerializeField] private Color lightColor = new Color(1.8f, 1.15f, 0.35f, 1f);
    [Min(0f)] [SerializeField] private float brightness = 0.85f;
    [Range(0f, 0.5f)] [SerializeField] private float breatheAmount = 0.12f;
    [Min(0.1f)] [SerializeField] private float breathePeriod = 3.6f;
    [Min(0.05f)] [SerializeField] private float fadeDuration = 0.45f;
    [Min(0.05f)] [SerializeField] private float ignitionDuration = 0.9f;
    [Min(0f)] [SerializeField] private float ignitionBoost = 1.1f;
    [Header("Sparkles")]
    [Range(0f, 3f)] [SerializeField] private float sparkleBrightness = 1f;
    [Range(0.3f, 3f)] [SerializeField] private float sparkleSize = 1f;
    [Range(0.1f, 3f)] [SerializeField] private float sparkleSpeed = 0.65f;

    private Mesh mesh;
    private Material material;
    private MeshRenderer glowRenderer;
    private Transform glowTransform;
    private bool lit;
    private float visibility;
    private float ignitionAge = 100f;
    private float phase;

    private void Awake()
    {
        phase = Mathf.Repeat(transform.position.x * 0.73f + transform.position.y * 1.37f, 6.28f);
    }

    private void OnEnable()
    {
        Shrine shrine = GetComponent<Shrine>();
        SetLit(shrine != null && shrine.IsActive, false);
    }

    public void SetLit(bool value, bool ignite)
    {
        lit = value;
        ignitionAge = value && ignite ? 0f : 100f;
        if (value && !ignite) visibility = 1f;
    }

    private bool EnsureRenderer()
    {
        if (glowRenderer != null) return true;
        if (shrineRenderer == null || shrineRenderer.sprite == null || glowMaterial == null) return false;
        var child = new GameObject("Shrine Inner Light");
        child.layer = gameObject.layer;
        glowTransform = child.transform;
        glowTransform.SetParent(shrineRenderer.transform, false);
        mesh = new Mesh { name = "Shrine Light Quad" };
        mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        glowRenderer = child.AddComponent<MeshRenderer>();
        glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;
        material = new Material(glowMaterial) { name = "Shrine Light (Runtime)" };
        glowRenderer.sharedMaterial = material;
        return true;
    }

    private void LateUpdate()
    {
        visibility = Mathf.MoveTowards(visibility, lit ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, fadeDuration));
        if (lit) ignitionAge += Time.deltaTime;
        if (!EnsureRenderer()) return;
        glowRenderer.enabled = visibility > 0f && shrineRenderer.enabled && shrineRenderer.sprite != null;
        if (!glowRenderer.enabled) return;

        Bounds bounds = shrineRenderer.sprite.bounds;
        Vector3 offset = new Vector3(center.x * bounds.size.x, center.y * bounds.size.y, 0f);
        Vector3 position = bounds.center + offset;
        if (shrineRenderer.flipX) position.x = -position.x;
        if (shrineRenderer.flipY) position.y = -position.y;
        glowTransform.localPosition = position;
        glowTransform.localScale = new Vector3(Mathf.Max(0.001f, size.x * bounds.size.x),
            Mathf.Max(0.001f, size.y * bounds.size.y), 1f);
        glowRenderer.sortingLayerID = shrineRenderer.sortingLayerID;
        glowRenderer.sortingOrder = shrineRenderer.sortingOrder + 1;
        float time = Time.time + phase;
        float breathe = 1f + Mathf.Sin(time * Mathf.PI * 2f / Mathf.Max(0.1f, breathePeriod)) * breatheAmount;
        float flash = Mathf.Sin(Mathf.Clamp01(ignitionAge / Mathf.Max(0.05f, ignitionDuration)) * Mathf.PI);
        material.SetColor("_GlowColor", lightColor);
        material.SetFloat("_Brightness", brightness * breathe + flash * ignitionBoost);
        material.SetFloat("_Visibility", visibility * shrineRenderer.color.a);
        material.SetVector("_Sparkles", new Vector4(sparkleBrightness, sparkleSize, sparkleSpeed, time));
    }

    private void OnDisable()
    {
        if (glowRenderer != null) glowRenderer.enabled = false;
        visibility = 0f;
    }

    private void OnDestroy()
    {
        if (glowTransform != null) Destroy(glowTransform.gameObject);
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }

    private void OnDrawGizmosSelected()
    {
        if (shrineRenderer == null || shrineRenderer.sprite == null) return;
        Bounds bounds = shrineRenderer.sprite.bounds;
        Vector3 position = bounds.center + new Vector3(center.x * bounds.size.x, center.y * bounds.size.y, 0f);
        if (shrineRenderer.flipX) position.x = -position.x;
        if (shrineRenderer.flipY) position.y = -position.y;
        Gizmos.matrix = shrineRenderer.transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.75f, 0.2f);
        Gizmos.DrawWireCube(position, new Vector3(size.x * bounds.size.x, size.y * bounds.size.y, 0f));
    }
}
