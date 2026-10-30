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
    // Keep the real asset through scene backups and script/domain reloads. The
    // generated preview material must never become the only material reference.
    [SerializeField, HideInInspector] private Material sourceMaterial;
    private Material depthMaterial;
    private bool previewSuspendedForSave;
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
        Material assigned = tileRenderer.sharedMaterial;
        if (assigned != null && (assigned.hideFlags & HideFlags.DontSaveInEditor) == 0)
            sourceMaterial = assigned;
#if UNITY_EDITOR
        // Repair scenes that were backed up with a missing temporary material by
        // the previous implementation, before the persistent reference existed.
        if (sourceMaterial == null)
            sourceMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_GroundDepthLit.mat");
        previewSuspendedForSave = UnityEditor.EditorApplication.isPlaying !=
            UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif
        if (sourceMaterial != null) tileRenderer.sharedMaterial = sourceMaterial;
        // A renderer-wide property block replaces Tilemap's per-texture draw data in
        // Individual mode. Clear the block left by older versions of this component.
        tileRenderer.SetPropertyBlock(null);
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
        ReleaseMaterial();
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
        if (previewSuspendedForSave) return;
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
        if (previewSuspendedForSave || !EnsureMaterial()) return;
        // World-space lookup survives Tilemap chunk/dynamic batching and Grid transforms.
        Vector3 origin = tilemap.CellToLocal(Vector3Int.zero);
        Vector3 x = tilemap.CellToLocal(Vector3Int.right) - origin;
        Vector3 y = tilemap.CellToLocal(Vector3Int.up) - origin;
        var cellToLocal = Matrix4x4.identity;
        cellToLocal.SetColumn(0, new Vector4(x.x, x.y, x.z, 0));
        cellToLocal.SetColumn(1, new Vector4(y.x, y.y, y.z, 0));
        cellToLocal.SetColumn(3, new Vector4(origin.x, origin.y, origin.z, 1));
        Matrix4x4 worldToCell = (tilemap.transform.localToWorldMatrix * cellToLocal).inverse;
        depthMaterial.SetTexture(MaskId, depthMask);
        depthMaterial.SetMatrix(MatrixId, worldToCell);
        depthMaterial.SetVector(RectId, new Vector4(maskBounds.xMin, maskBounds.yMin,
            1f / maskBounds.size.x, 1f / maskBounds.size.y));
        depthMaterial.SetVector(SettingsId, new Vector4(surfaceWidth, fadeDistance, maxDarkness, falloffPower));
        depthMaterial.SetColor(TintId, depthTint);
        depthMaterial.SetFloat(EnabledId, 1f);
    }

    private void DisableEffect()
    {
        if (tileRenderer == null) return;
        if (depthMaterial != null && tileRenderer.sharedMaterial == depthMaterial)
            tileRenderer.sharedMaterial = sourceMaterial;
    }

    private bool EnsureMaterial()
    {
        Material current = tileRenderer.sharedMaterial;
        if (current != null && current != depthMaterial && current != sourceMaterial)
        {
            ReleaseMaterial();
            sourceMaterial = current;
        }
        if (sourceMaterial == null || sourceMaterial.shader == null ||
            sourceMaterial.shader.name != "ProjectLx/Ground Depth Lit") return false;
        if (depthMaterial == null)
        {
            // Each map owns its parameters while Unity continues selecting each tile's
            // sprite texture. Never change the shared project material.
            depthMaterial = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + " (Ground Depth Preview)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        if (current != depthMaterial) tileRenderer.sharedMaterial = depthMaterial;
        return true;
    }

    private void ReleaseMaterial()
    {
        if (depthMaterial == null) return;
        if (Application.IsPlaying(gameObject)) Destroy(depthMaterial);
        else DestroyImmediate(depthMaterial);
        depthMaterial = null;
    }

#if UNITY_EDITOR
    internal void SuspendPreviewForSave()
    {
        previewSuspendedForSave = true;
        DisableEffect();
    }

    internal void ResumePreviewAfterSave()
    {
        // A delayed save callback can run while Unity is making a Play Mode
        // backup. Wait for EnteredPlayMode/EnteredEditMode before reassigning a clone.
        if (UnityEditor.EditorApplication.isPlaying !=
            UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        previewSuspendedForSave = false;
        if (isActiveAndEnabled && depthMask != null) ApplyProperties();
    }
#endif

    private void ReleaseMask()
    {
        if (depthMask == null) return;
        if (Application.IsPlaying(gameObject)) Destroy(depthMask);
        else DestroyImmediate(depthMask);
        depthMask = null;
    }
}

#if UNITY_EDITOR
// Persist the source material reference, never an unsaved preview material, in
// scenes and prefabs. Rendering resumes on the next editor callback after saving.
internal sealed class GroundDepthShadingSaveProcessor : UnityEditor.AssetModificationProcessor
{
    [UnityEditor.InitializeOnLoadMethod]
    private static void Initialize()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {
        bool suspend = state == UnityEditor.PlayModeStateChange.ExitingEditMode ||
            state == UnityEditor.PlayModeStateChange.ExitingPlayMode;
        foreach (GroundDepthShading preview in Resources.FindObjectsOfTypeAll<GroundDepthShading>())
        {
            if (!preview.isActiveAndEnabled) continue;
            if (suspend) preview.SuspendPreviewForSave();
            else preview.ResumePreviewAfterSave();
        }
    }

    private static string[] OnWillSaveAssets(string[] paths)
    {
        GroundDepthShading[] previews = Resources.FindObjectsOfTypeAll<GroundDepthShading>();
        foreach (GroundDepthShading preview in previews)
            if (preview.isActiveAndEnabled) preview.SuspendPreviewForSave();
        UnityEditor.EditorApplication.delayCall += () =>
        {
            foreach (GroundDepthShading preview in previews)
                if (preview != null) preview.ResumePreviewAfterSave();
        };
        return paths;
    }
}
#endif

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
