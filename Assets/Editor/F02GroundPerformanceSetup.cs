using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Synchronize the currently open scene too, so an older in-memory scene cannot
// overwrite the external scene-file fix on its next save.
[InitializeOnLoad]
public static class F02GroundPerformanceSetup
{
    private const string ScenePath = "Assets/Scenes/InProgress/F_02.unity";
    private static bool pending = true;

    static F02GroundPerformanceSetup()
    {
        EditorApplication.delayCall += ApplyWhenReady;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && pending) ApplyWhenReady();
    }

    private static void ApplyWhenReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        pending = false;
        ApplyLoadedScenes();
    }

    [MenuItem("Project Lx/Performance/Apply F_02 Ground Optimization")]
    public static void Apply()
    {
        pending = true;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("[F_02 Performance] Ground settings will be applied after leaving Play Mode.");
            return;
        }
        ApplyWhenReady();
    }

    private static void ApplyLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.path != ScenePath) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.name != "Tilemap_Ground") continue;
                var renderer = map.GetComponent<TilemapRenderer>();
                var tiles = map.GetComponent<TilemapCollider2D>();
                var composite = map.GetComponent<CompositeCollider2D>();
                if (renderer == null || tiles == null || composite == null) continue;
                if (renderer.mode == TilemapRenderer.Mode.Chunk
                    && tiles.compositeOperation == Collider2D.CompositeOperation.Merge
                    && composite.geometryType == CompositeCollider2D.GeometryType.Polygons) continue;

                Undo.RecordObjects(new Object[] { renderer, tiles, composite }, "Optimize F_02 Ground");
                renderer.mode = TilemapRenderer.Mode.Chunk;
                tiles.compositeOperation = Collider2D.CompositeOperation.Merge;
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                foreach (Object target in new Object[] { renderer, tiles, composite })
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    EditorUtility.SetDirty(target);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[F_02 Performance] Applied Chunk rendering and merged solid Ground colliders. Save the scene to retain the settings.", map);
            }
        }
    }
}
