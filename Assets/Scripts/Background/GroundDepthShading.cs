using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Builds a cell-space distance field; only this Tilemap contributes solid cells.</summary>
[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
[AddComponentMenu("Rendering/Ground Depth Shading")]
public sealed class GroundDepthShading : MonoBehaviour
{
    [Header("Depth (in tiles)")]
    [Min(0f), Tooltip("Distance from empty space that keeps the original tile brightness.")]
    public float surfaceWidth = 1f;
    [Min(0.1f), Tooltip("Additional distance over which the ground fades into darkness.")]
    public float fadeDistance = 3f;
    [Range(0f, 1f)] public float maxDarkness = 0.92f;
    [ColorUsage(false)] public Color depthTint = Color.black;
    [Range(0.25f, 4f), Tooltip("Higher values keep more of the surface bright before darkening.")]
    public float falloffPower = 1f;

    [Header("Refresh")]
    [Tooltip("Rebuild after painting, erasing or changing tiles, including in Edit Mode.")]
    public bool autoRefresh = true;
    [Min(0f)] public float refreshDelay = 0.15f;

    // A guard against accidentally allocating textures for enormous sparse cellBounds.
    private const int MaxMaskCells = 4 * 1024 * 1024;
    private Tilemap tilemap;
    private TilemapRenderer tileRenderer;
    private Texture2D depthMask;
    private MaterialPropertyBlock properties;
    private BoundsInt maskBounds;
    private bool pendingRebuild;
    private double nextRebuild;

    private static readonly int MaskId = Shader.PropertyToID("_GroundDepthMask");
    private static readonly int MatrixId = Shader.PropertyToID("_WorldToGroundCell");
    private static readonly int RectId = Shader.PropertyToID("_GroundDepthRect");
    private static readonly int SettingsId = Shader.PropertyToID("_GroundDepthSettings");
    private static readonly int TintId = Shader.PropertyToID("_GroundDepthTint");
    private static readonly int EnabledId = Shader.PropertyToID("_GroundDepthEnabled");

    private void OnEnable()
    {
        tilemap = GetComponent<Tilemap>();
        tileRenderer = GetComponent<TilemapRenderer>();
        properties = new MaterialPropertyBlock();
        Tilemap.tilemapTileChanged += OnTilesChanged;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorTick;
#endif
        pendingRebuild = true;
        nextRebuild = 0;
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= OnTilesChanged;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTick;
#endif
        DisableEffect();
        ReleaseMask();
    }

    private void OnValidate()
    {
        surfaceWidth = Mathf.Max(0f, surfaceWidth);
        fadeDistance = Mathf.Max(0.1f, fadeDistance);
        refreshDelay = Mathf.Max(0f, refreshDelay);
        // Unity object/texture work is deferred out of OnValidate.
        pendingRebuild = true;
    }

    private void OnTilesChanged(Tilemap changed, Tilemap.SyncTile[] changes)
    {
        if (changed != tilemap || !autoRefresh) return;
        if (!pendingRebuild) nextRebuild = Time.realtimeSinceStartupAsDouble + refreshDelay;
        pendingRebuild = true;
    }

    private void Update()
    {
        if (Application.IsPlaying(gameObject)) Tick();
    }

#if UNITY_EDITOR
    private void EditorTick()
    {
        if (this != null && !Application.IsPlaying(gameObject)) Tick();
    }
#endif

    private void Tick()
    {
        if (pendingRebuild && Time.realtimeSinceStartupAsDouble >= nextRebuild)
            RebuildMask();
        if (depthMask != null) ApplyProperties();
    }

    [ContextMenu("Rebuild Depth Mask")]
    public void RebuildMask()
    {
        if (!isActiveAndEnabled) return;
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tileRenderer == null) tileRenderer = GetComponent<TilemapRenderer>();
        if (properties == null) properties = new MaterialPropertyBlock();
        pendingRebuild = false;

        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0 || bounds.size.z == 0)
        {
            DisableEffect();
            ReleaseMask();
            return;
        }
        if (bounds.size.z != 1 || tilemap.layoutGrid == null ||
            tilemap.layoutGrid.cellLayout != GridLayout.CellLayout.Rectangle ||
            tilemap.layoutGrid.cellSwizzle != GridLayout.CellSwizzle.XYZ)
        {
            DisableEffect();
            ReleaseMask();
            Debug.LogWarning("Ground Depth Shading requires a rectangular XY Tilemap with one Z layer.", this);
            return;
        }

        int width = bounds.size.x + 2;
        int height = bounds.size.y + 2;
        if ((long)width * height > MaxMaskCells || width > SystemInfo.maxTextureSize ||
            height > SystemInfo.maxTextureSize)
        {
            DisableEffect();
            ReleaseMask();
            Debug.LogWarning("Ground depth mask is too large. Compress Tilemap Bounds or split the ground into smaller maps.", this);
            return;
        }

        TileBase[] tiles = tilemap.GetTilesBlock(bounds);
        var solid = new bool[width * height];
        for (int y = 0; y < bounds.size.y; y++)
            for (int x = 0; x < bounds.size.x; x++)
                solid[(y + 1) * width + x + 1] = tiles[y * bounds.size.x + x] != null;

        float[] distances = GroundDepthDistanceField.Build(solid, width, height);
        var pixels = new Color[distances.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            // Empty-cell centers are half a cell beyond the visible surface.
            float depth = Mathf.Max(0f, Mathf.Sqrt(distances[i]) - 0.5f);
            pixels[i] = new Color(depth, 0f, 0f, 1f);
        }

        ReleaseMask();
        depthMask = new Texture2D(width, height, TextureFormat.RFloat, false, true)
        {
            name = name + " Ground Depth (Generated)",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        depthMask.SetPixels(pixels);
        depthMask.Apply(false, true);
        maskBounds = new BoundsInt(bounds.xMin - 1, bounds.yMin - 1, bounds.zMin, width, height, 1);
        ApplyProperties();
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    private void ApplyProperties()
    {
        // World-space lookup survives Tilemap chunk/dynamic batching and Grid transforms.
        Vector3 origin = tilemap.CellToLocal(Vector3Int.zero);
        Vector3 x = tilemap.CellToLocal(Vector3Int.right) - origin;
        Vector3 y = tilemap.CellToLocal(Vector3Int.up) - origin;
        var cellToLocal = Matrix4x4.identity;
        cellToLocal.SetColumn(0, new Vector4(x.x, x.y, x.z, 0));
        cellToLocal.SetColumn(1, new Vector4(y.x, y.y, y.z, 0));
        cellToLocal.SetColumn(3, new Vector4(origin.x, origin.y, origin.z, 1));
        Matrix4x4 worldToCell = (tilemap.transform.localToWorldMatrix * cellToLocal).inverse;
        tileRenderer.GetPropertyBlock(properties);
        properties.SetTexture(MaskId, depthMask);
        properties.SetMatrix(MatrixId, worldToCell);
        properties.SetVector(RectId, new Vector4(maskBounds.xMin, maskBounds.yMin,
            1f / maskBounds.size.x, 1f / maskBounds.size.y));
        properties.SetVector(SettingsId, new Vector4(surfaceWidth, fadeDistance, maxDarkness, falloffPower));
        properties.SetColor(TintId, depthTint);
        properties.SetFloat(EnabledId, 1f);
        tileRenderer.SetPropertyBlock(properties);
    }

    private void DisableEffect()
    {
        if (tileRenderer == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        tileRenderer.GetPropertyBlock(properties);
        properties.SetFloat(EnabledId, 0f);
        properties.SetTexture(MaskId, Texture2D.blackTexture);
        tileRenderer.SetPropertyBlock(properties);
    }

    private void ReleaseMask()
    {
        if (depthMask == null) return;
        if (Application.IsPlaying(gameObject)) Destroy(depthMask);
        else DestroyImmediate(depthMask);
        depthMask = null;
    }
}

/// <summary>Exact squared Euclidean distance to the nearest empty cell, in O(width * height).</summary>
internal static class GroundDepthDistanceField
{
    internal static float[] Build(bool[] solid, int width, int height)
    {
        var result = new float[solid.Length];
        int length = Math.Max(width, height);
        var input = new float[length];
        var output = new float[length];
        var sites = new int[length];
        var boundaries = new float[length + 1];
        // Finite infinity avoids Infinity - Infinity during parabola intersection.
        float far = (float)width * width + (float)height * height + 1f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++) input[x] = solid[y * width + x] ? far : 0f;
            Transform(input, output, width, sites, boundaries);
            for (int x = 0; x < width; x++) result[y * width + x] = output[x];
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++) input[y] = result[y * width + x];
            Transform(input, output, height, sites, boundaries);
            for (int y = 0; y < height; y++) result[y * width + x] = output[y];
        }
        return result;
    }

    private static void Transform(float[] input, float[] output, int length, int[] sites, float[] boundaries)
    {
        int k = 0;
        sites[0] = 0;
        boundaries[0] = float.NegativeInfinity;
        boundaries[1] = float.PositiveInfinity;
        for (int q = 1; q < length; q++)
        {
            float intersection;
            do
            {
                int p = sites[k];
                intersection = ((input[q] + (float)q * q) - (input[p] + (float)p * p)) / (2f * (q - p));
                if (intersection > boundaries[k]) break;
                k--;
            } while (k >= 0);
            k++;
            sites[k] = q;
            boundaries[k] = intersection;
            boundaries[k + 1] = float.PositiveInfinity;
        }
        k = 0;
        for (int q = 0; q < length; q++)
        {
            while (boundaries[k + 1] < q) k++;
            float delta = q - sites[k];
            output[q] = delta * delta + input[sites[k]];
        }
    }
}
