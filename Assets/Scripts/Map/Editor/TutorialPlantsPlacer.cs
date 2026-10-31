using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 튜토리얼 맵에 근경 프리팹(수풀·덩굴·담쟁이)을 종류별로 하나 이상 놓는다.
/// 메뉴: Tools/Map/튜토리얼 맵에 수풀·덩굴 추가  →  확인 후 씬 저장(Ctrl+S)
///
/// - Foreground_Plants 아래에 놓는다. 다시 누르면 그 아래를 비우고 새로 놓는다 (Ctrl+Z로 되돌리기 가능).
///   직접 옮긴 위치를 지키고 싶으면 다시 누르지 말 것.
/// - 덩굴·담쟁이: 천장 아랫면에 매단다 (기준점 위쪽 가운데). 천장이 낮은 곳은 작게 줄여서
///   위아래 틈의 40%까지만 내려오게 한다 (점프할 때 머리를 가리지 않게).
/// - 수풀: 바닥 윗면에 세운다 (기준점 아래 가운데).
/// - 캐릭터 앞(Front)은 상자·제단·슬라임 자리를 피해서 조금만 놓는다.
/// 바닥·천장 높이는 TutorialMapBuilder의 Layout과 같은 값이다. 맵 모양을 바꾸면 여기 표도 같이 고칠 것.
/// </summary>
public static class TutorialPlantsPlacer
{
    private const string MenuPath = "Tools/Map/튜토리얼 맵에 수풀·덩굴 추가";
    private const string ScenePath = "Assets/Scenes/InProgress/Tutorial.unity";
    private const string Folder = "Assets/Prefabs/Foreground/";
    private const string RootName = "Foreground_Plants";

    // (시작 x, 끝 x, 높이) — TutorialMapBuilder.Layout 과 같게
    private static readonly (int x0, int x1, float y)[] Floors =
    {
        (2, 21, 1f), (22, 29, 3f), (30, 35, 5f), (36, 39, 3f), (40, 47, 5f), (48, 53, 3f),
        (54, 77, 5f), (78, 81, 11f), (82, 113, 3f), (114, 117, 15f), (118, 147, 5f),
    };
    private static readonly (int x0, int x1, float y)[] Ceilings =
    {
        (0, 21, 9f), (22, 35, 11f), (36, 53, 13f), (54, 77, 17f), (78, 81, 19f), (82, 121, 21f), (122, 125, 23f),
    };

    // 매다는 것: 담쟁이 22종 + 덩굴 2종은 땅 뒤(Back)로 한 번씩, 몇 개는 캐릭터 앞(Front)으로 더.
    // 제단 방 종유석(x 62~69) 아래는 비운다.
    private static readonly (string prefab, float x)[] Hanging =
    {
        ("FG_Back_Ivy_Teal_1",       4f),
        ("FG_Back_Vine_A",           9f),
        ("FG_Back_Ivy_Light_1",     18f),
        ("FG_Back_Ivy_Dark_1",      24f),
        ("FG_Back_Ivy_Cluster",     29f),
        ("FG_Back_Ivy_Teal_2",      33f),
        ("FG_Back_Ivy_Light_2",     38f),
        ("FG_Back_Ivy_GlowCluster", 43f),
        ("FG_Back_Ivy_Dark_2",      48f),
        ("FG_Back_Vine_B",          52f),
        ("FG_Back_Ivy_Teal_3",      56f),
        ("FG_Back_Ivy_Light_3",     72f),
        ("FG_Back_Ivy_Dark_3",      76f),
        ("FG_Back_Ivy_Glow_1",      80f),
        ("FG_Back_Ivy_OrbCluster",  85f),
        ("FG_Back_Ivy_Teal_4",      89f),
        ("FG_Back_Ivy_Light_4",     93f),
        ("FG_Back_Ivy_Dark_4",      97f),
        ("FG_Back_Ivy_Orb_1",      101f),
        ("FG_Back_Ivy_Teal_5",     105f),
        ("FG_Back_Ivy_Light_5",    109f),
        ("FG_Back_Ivy_Dark_5",     113f),
        ("FG_Back_Ivy_Plain_1",    117f),
        ("FG_Back_Ivy_Teal_6",     120f),
        ("FG_Back_Ivy_Light_6",    123f),
        ("FG_Back_Ivy_Dark_6",      59f),
        // 캐릭터 앞
        ("FG_Front_Ivy_Dark_2",     14f),
        ("FG_Front_Vine_B",         46f),
        ("FG_Front_Ivy_Plain_1",    99f),
        ("FG_Front_Ivy_Cluster",   124f),
    };

    // 바닥에 세우는 것: 수풀 3종 × 뒤·앞. 시작 위치(5), 상자(60), 제단(70), 슬라임(127~137)은 피한다.
    private static readonly (string prefab, float x)[] Ground =
    {
        ("FG_Back_Bush_Round",  11f),
        ("FG_Back_Bush_Spiky",  44f),
        ("FG_Back_Bush_Tall",   55f),
        ("FG_Back_Bush_Round",  90f),
        ("FG_Back_Bush_Spiky", 108f),
        ("FG_Back_Bush_Tall",  120f),
        ("FG_Front_Bush_Round", 25f),
        ("FG_Front_Bush_Spiky", 50f),
        ("FG_Front_Bush_Tall", 122f),
    };

    private const float MaxHangRatio = 0.4f;   // 천장~바닥 틈의 이만큼까지만 내려온다

    [MenuItem(MenuPath)]
    private static void Place()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("튜토리얼 근경", "Tutorial 씬(InProgress/Tutorial)을 열고 눌러 주세요.", "확인");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("튜토리얼 수풀·덩굴 추가");

        GameObject root = null;
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == RootName) { root = go; break; }
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "튜토리얼 수풀·덩굴 추가");
        }
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

        int placed = 0;
        HashSet<string> kinds = new HashSet<string>();

        for (int i = 0; i < Hanging.Length; i++)
        {
            var h = Hanging[i];
            if (!TryHeight(Ceilings, h.x, out float ceil) || !TryHeight(Floors, h.x, out float floor)) continue;
            GameObject go = Spawn(root.transform, h.prefab, new Vector3(h.x, ceil, 0f), "Hang_" + i + "_" + h.prefab);
            if (go == null) continue;

            // 천장이 낮으면 줄인다: 그림 아래 끝이 틈의 40% 아래로 내려가지 않게
            float hang = HangLength(go, ceil);
            float scale = hang > 0.01f ? Mathf.Clamp((ceil - floor) * MaxHangRatio / hang, 0.45f, 1.1f) : 1f;
            float flip = i % 2 == 0 ? 1f : -1f;
            go.transform.localScale = new Vector3(scale * flip, scale, 1f);
            kinds.Add(Kind(h.prefab));
            placed++;
        }

        for (int i = 0; i < Ground.Length; i++)
        {
            var g = Ground[i];
            if (!TryHeight(Floors, g.x, out float floor)) continue;
            GameObject go = Spawn(root.transform, g.prefab, new Vector3(g.x, floor - 0.1f, 0f), "Ground_" + i + "_" + g.prefab);
            if (go == null) continue;
            float flip = i % 2 == 0 ? 1f : -1f;
            go.transform.localScale = new Vector3(flip, 1f, 1f);
            kinds.Add(Kind(g.prefab));
            placed++;
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[TutorialPlantsPlacer] 수풀·덩굴 {placed}개 ({kinds.Count}종)를 {RootName} 아래에 놓았어요. 확인 후 Ctrl+S로 저장하세요.");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanPlace() => !EditorApplication.isPlaying;

    private static GameObject Spawn(Transform parent, string prefabName, Vector3 pos, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + prefabName + ".prefab");
        if (prefab == null)
        {
            Debug.LogWarning("[TutorialPlantsPlacer] 프리팹이 없어요: " + Folder + prefabName);
            return null;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(go, "튜토리얼 수풀·덩굴 추가");
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    /// <summary>기준점(천장)에서 그림 아래 끝까지 길이</summary>
    private static float HangLength(GameObject go, float ceil)
    {
        float bottom = ceil;
        bool any = false;
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null) continue;
            bottom = any ? Mathf.Min(bottom, sr.bounds.min.y) : sr.bounds.min.y;
            any = true;
        }
        return any ? ceil - bottom : 0f;
    }

    private static bool TryHeight((int x0, int x1, float y)[] table, float x, out float y)
    {
        int cx = Mathf.FloorToInt(x);
        foreach (var t in table)
            if (cx >= t.x0 && cx <= t.x1) { y = t.y; return true; }
        y = 0f;
        return false;
    }

    // FG_Back_Ivy_Teal_1 → Ivy_Teal_1 (앞·뒤는 같은 종류로 센다)
    private static string Kind(string prefab) => prefab.Replace("FG_Back_", "").Replace("FG_Front_", "");
}
