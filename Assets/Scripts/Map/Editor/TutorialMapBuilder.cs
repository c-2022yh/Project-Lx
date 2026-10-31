using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 튜토리얼 맵(Tutorial.unity)을 코드로 다시 그린다.
/// 메뉴: Tools/Map/튜토리얼 맵 다시 그리기  →  확인 후 씬 저장(Ctrl+S)
///
/// 흐름 (왼쪽 → 오른쪽)
///   1. 시작        이동·점프 안내 (TutorialDirector가 시작할 때 자동으로 띄움)
///   2. 점프 연습   계단 두 번 + 작은 구덩이 두 개
///   3. 상자        발판 위 상자에서 더블 점프 유물(깃털구름) 얻기 (상자 안내 자동)
///   4. 제단        불 밝히고 유물 장착 (제단 안내 자동). 장착해야 다음 벽을 넘을 수 있다
///   5. 더블 점프 벽 한 번 점프로는 못 넘는 높이 (+6칸)
///   6. 점프맵      구덩이 위 발판 4개, 마지막은 더블 점프. 떨어지면 왼쪽 사다리 발판으로 다시 올라온다
///   7. 전투        안 움직이는 슬라임 3마리 (공격 안내)
///   8. 출구        포탈 (원래 설정대로 F_01로 감)
///
/// 타일 규칙 (기존 맵과 같게)
///   땅   Tilemap_Ground 에 Ground_Rock 2×2 그림 (짝수 x = 왼쪽, 짝수 y = 위쪽).
///        빈 곳에 닿는 덩어리는 Blue, 안쪽은 Charcoal.
///   풀   Tilemap_Grass 에 Grass_Top. 땅 윗면과 같은 칸에 겹쳐 그린다.
///   발판 TIleMap_Platform 에 Platform_Rock (왼쪽·가운데·오른쪽). 아래에서 뚫고 올라갈 수 있는 발판.
///   가시 프리팹으로 뺄 예정이라 쓰지 않는다. Tilemap_Spike 는 비운다.
///
/// 높이 기준: 점프 약 4칸, 더블 점프 약 8칸 (PlayerMove jumpForce 20, 중력 5).
///
/// 다시 실행하면 Grid 안 타일을 전부 지우고 새로 그린다.
/// 상자·제단·포탈·시작 위치는 이름으로 찾아서 자리만 옮긴다 (없으면 프리팹으로 새로 놓는다).
/// 보주 줍기(OrbPickup)는 인트로 연출에서 보주를 주므로 지운다.
/// 슬라임과 안내 영역은 Field_Object/Tutorial_Enemies, Tutorial_Triggers 아래에 새로 만든다.
/// 카메라 영역(CameraConfiner)은 맵 크기에 맞춰 늘린다.
/// </summary>
public static class TutorialMapBuilder
{
    private const string ScenePath = "Assets/Scenes/InProgress/Tutorial.unity";

    private const string GroundDir = "Assets/Tiles/Ground_Rock/";
    private const string GrassDir = "Assets/Tiles/Grass/";
    private const string PlatformDir = "Assets/Tiles/Platform_Rock/Blue/";

    private const string SpawnPrefab = "Assets/Prefabs/Field_Object/Portal/SpawnPoint.prefab";
    private const string ChestPrefab = "Assets/Prefabs/Field_Object/Chest/RelicChest_FeatherCloud.prefab";
    private const string ShrinePrefab = "Assets/Prefabs/Field_Object/Shrine.prefab";
    private const string PortalPrefab = "Assets/Prefabs/Field_Object/Portal/AutoScenePortal.prefab";
    private const string SlimePrefab = "Assets/Prefabs/Enemies/Enemy_SapSlime.prefab";
    private const string TriggerPrefab = "Assets/Prefabs/Field_Object/TutorialTrigger.prefab";

    // 맵 범위 (타일, 양끝 포함)
    private const int MinX = 0, MaxX = 149, MinY = -5, MaxY = 24;
    private const int W = MaxX - MinX + 1, H = MaxY - MinY + 1;
    private const int CeilingBottom = 21;

    private static bool[,] solid;

    // 통과 발판: (줄 y, 시작 x, 길이). 서 있는 높이 = y + 1
    private static readonly (int y, int x, int len)[] Platforms =
    {
        (7, 58, 4),    // 3 상자 발판 (바닥에서 +3)
        (5, 82, 4),    // 6 구덩이에서 돌아오는 사다리
        (8, 82, 4),    //   → 더블 점프 벽 위로 (+2)
        (11, 88, 4),   //   점프맵 1 (+1)
        (13, 95, 4),   //          2 (+2)
        (11, 102, 4),  //          3 (-2)
        (16, 108, 4),  //          4 (+5, 더블 점프)
    };

    [MenuItem("Tools/Map/튜토리얼 맵 다시 그리기")]
    private static void Build()
    {
        if (!OpenTutorialScene()) return;
        if (!EditorUtility.DisplayDialog("튜토리얼 맵",
                "Tutorial 씬의 타일을 모두 지우고 다시 그립니다.\n상자·제단·포탈 등은 자리만 옮깁니다.\n\n계속할까요?",
                "다시 그리기", "취소")) return;

        Scene scene = SceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("튜토리얼 맵 다시 그리기");

        Layout();
        if (!PaintTiles(scene)) return;
        PlaceObjects(scene);
        FitCamera(scene);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[TutorialMapBuilder] 튜토리얼 맵을 다시 그렸습니다. 확인 후 씬을 저장하세요 (Ctrl+S).");
    }

    private static bool OpenTutorialScene()
    {
        if (SceneManager.GetActiveScene().path == ScenePath) return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return SceneManager.GetActiveScene().path == ScenePath;
    }

    // ── 지형 ───────────────────────────────
    // 땅 덩어리는 2×2 그림이 맞도록 x는 짝수에서 시작해 홀수에서 끝나고, 윗면 y는 짝수다.
    // 서 있는 높이 = 윗면 + 1.

    private static void Layout()
    {
        solid = new bool[W, H];
        Fill(0, 1, MinY, MaxY);              // 왼쪽 벽
        Fill(148, 149, MinY, MaxY);          // 오른쪽 벽
        Fill(0, 149, CeilingBottom, MaxY);   // 천장

        Floor(2, 21, 0);      // 1 시작
        Floor(22, 29, 2);     // 2 계단 +2
        Floor(30, 35, 4);     //   계단 +2
        Floor(36, 39, 2);     //   작은 구덩이
        Floor(40, 47, 4);
        Floor(48, 53, 2);     //   넓은 구덩이 (6칸)
        Floor(54, 77, 4);     // 3 상자 · 4 제단
        Floor(78, 81, 10);    // 5 더블 점프 벽 (+6)
        Floor(82, 113, 2);    // 6 점프맵 아래 구덩이 바닥
        Floor(114, 117, 14);  //   점프맵 끝 발판
        Floor(118, 147, 4);   // 7 전투 · 8 출구
    }

    private static void Floor(int x0, int x1, int top) => Fill(x0, x1, MinY, top);

    private static void Fill(int x0, int x1, int y0, int y1)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                solid[x - MinX, y - MinY] = true;
    }

    // 맵 밖은 막혀 있다고 본다 (바깥쪽 벽이 Charcoal이 되게)
    private static bool IsSolid(int x, int y)
    {
        if (x < MinX || x > MaxX || y < MinY || y > MaxY) return true;
        return solid[x - MinX, y - MinY];
    }

    // 2×2 덩어리 하나라도 옆·위·아래 덩어리에 빈 칸이 있으면 가장자리
    private static bool BlockTouchesAir(int x, int y)
    {
        int bx = x - (x & 1);
        int by = (y & 1) == 0 ? y - 1 : y;   // 덩어리 = 아래(홀수) 줄 + 위(짝수) 줄
        for (int cx = bx - 2; cx <= bx + 3; cx++)
            for (int cy = by - 2; cy <= by + 3; cy++)
                if (!IsSolid(cx, cy)) return true;
        return false;
    }

    // ── 타일 ───────────────────────────────

    private static bool PaintTiles(Scene scene)
    {
        Transform grid = FindGrid(scene);
        Tilemap ground = FindTilemap(grid, "Tilemap_Ground");
        Tilemap grass = FindTilemap(grid, "Tilemap_Grass");
        Tilemap platform = FindTilemap(grid, "TIleMap_Platform");
        if (ground == null || grass == null || platform == null)
        {
            EditorUtility.DisplayDialog("튜토리얼 맵", "Grid 안에서 Tilemap_Ground / Tilemap_Grass / TIleMap_Platform 을 찾지 못했습니다.", "확인");
            return false;
        }

        TileBase[] blue = LoadQuad("Blue");
        TileBase[] charcoal = LoadQuad("Charcoal");
        TileBase[] grassTops = { Load(GrassDir + "Grass_Top_V01"), Load(GrassDir + "Grass_Top_V02"), Load(GrassDir + "Grass_Top_V03") };
        TileBase[][] platformSets =
        {
            new[] { Load(PlatformDir + "Platform_Rock_left_01"), Load(PlatformDir + "Platform_Rock_middle_01"), Load(PlatformDir + "Platform_Rock_middle_02"), Load(PlatformDir + "Platform_Rock_right_01") },
            new[] { Load(PlatformDir + "Platform_Rock_left_02"), Load(PlatformDir + "Platform_Rock_middle_03"), Load(PlatformDir + "Platform_Rock_middle_04"), Load(PlatformDir + "Platform_Rock_right_02") },
        };
        if (blue == null || charcoal == null || HasNull(grassTops) || HasNull(platformSets[0]) || HasNull(platformSets[1]))
        {
            EditorUtility.DisplayDialog("튜토리얼 맵", "타일 에셋을 찾지 못했습니다. Console 경고를 확인하세요.", "확인");
            return false;
        }

        // Grid 안 타일맵만 비운다 (가시·비밀벽 포함)
        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            Undo.RegisterCompleteObjectUndo(tm, "튜토리얼 맵 다시 그리기");
            tm.ClearAllTiles();
        }

        var gPos = new List<Vector3Int>(); var gTiles = new List<TileBase>();
        var wPos = new List<Vector3Int>(); var wTiles = new List<TileBase>();
        for (int x = MinX; x <= MaxX; x++)
        {
            for (int y = MinY; y <= MaxY; y++)
            {
                if (!IsSolid(x, y)) continue;

                TileBase[] set = BlockTouchesAir(x, y) ? blue : charcoal;
                int idx = ((y & 1) == 0 ? 0 : 2) + ((x & 1) == 0 ? 0 : 1);   // 위왼·위오·아래왼·아래오
                gPos.Add(new Vector3Int(x, y, 0));
                gTiles.Add(set[idx]);

                // 윗면 풀. 가끔 비워서 덜 반복돼 보이게
                if (y < CeilingBottom && !IsSolid(x, y + 1))
                {
                    int h = Hash(x);
                    if (h % 7 != 0)
                    {
                        wPos.Add(new Vector3Int(x, y, 0));
                        wTiles.Add(grassTops[h % grassTops.Length]);
                    }
                }
            }
        }
        ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
        grass.SetTiles(wPos.ToArray(), wTiles.ToArray());

        var pPos = new List<Vector3Int>(); var pTiles = new List<TileBase>();
        for (int i = 0; i < Platforms.Length; i++)
        {
            (int y, int x0, int len) = Platforms[i];
            TileBase[] set = platformSets[i % platformSets.Length];
            for (int k = 0; k < len; k++)
            {
                TileBase t = k == 0 ? set[0] : k == len - 1 ? set[3] : set[1 + (k - 1) % 2];
                pPos.Add(new Vector3Int(x0 + k, y, 0));
                pTiles.Add(t);
            }
        }
        platform.SetTiles(pPos.ToArray(), pTiles.ToArray());

        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true)) tm.CompressBounds();
        return true;
    }

    private static TileBase[] LoadQuad(string color)
    {
        TileBase[] q =
        {
            Load(GroundDir + color + "/Ground_TopLeft"),
            Load(GroundDir + color + "/Ground_TopRight"),
            Load(GroundDir + color + "/Ground_BottomLeft"),
            Load(GroundDir + color + "/Ground_BottomRight"),
        };
        return HasNull(q) ? null : q;
    }

    private static TileBase Load(string pathNoExt)
    {
        TileBase t = AssetDatabase.LoadAssetAtPath<TileBase>(pathNoExt + ".asset");
        if (t == null) Debug.LogWarning("[TutorialMapBuilder] 타일 없음: " + pathNoExt + ".asset");
        return t;
    }

    private static bool HasNull(TileBase[] a)
    {
        foreach (TileBase t in a) if (t == null) return true;
        return false;
    }

    private static int Hash(int x)
    {
        unchecked
        {
            int h = x * 73856093 ^ 0x5bd1e995;
            h ^= h >> 13;
            return (h & 0x7fffffff);
        }
    }

    // ── 오브젝트 ───────────────────────────
    // 높이 = 서 있는 높이 + 기존 Tutorial 씬에서 쓰던 띄움값

    private static void PlaceObjects(Scene scene)
    {
        Transform field = FindInScene(scene, "Field_Object");
        if (field == null)
        {
            GameObject go = new GameObject("Field_Object");
            Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
            SceneManager.MoveGameObjectToScene(go, scene);
            field = go.transform;
        }

        Place(scene, "Spawn_From_START", SpawnPrefab, new Vector3(5f, 1.5f, 0f), field);    // 1 시작
        Remove(scene, "OrbPickup");   // 보주는 인트로 연출에서 받는다
        Place(scene, "RelicChest_FeatherCloud", ChestPrefab, new Vector3(60f, 9.5f, 0f), field);  // 3 발판 위
        Place(scene, "Shrine", ShrinePrefab, new Vector3(70f, 8.11f, 0f), field);           // 4
        Place(scene, "AutoScenePortal", PortalPrefab, new Vector3(145f, 7.5f, 0f), field);  // 8

        // 7 안 움직이는 슬라임
        Transform enemies = CleanGroup(field, "Tutorial_Enemies");
        foreach (float x in new[] { 124f, 130f, 136f })
        {
            GameObject slime = Spawn(scene, SlimePrefab, new Vector3(x, 6f, 0f), enemies);
            if (slime != null) slime.name = "Enemy_SapSlime";
        }

        // 안내 영역. 이동·점프·상자·제단 안내는 TutorialDirector가 알아서 띄운다.
        Transform triggers = CleanGroup(field, "Tutorial_Triggers");
        AddTrigger(scene, triggers, "Trigger_DoubleJump", new Vector2(75.5f, 7.5f), new Vector2(5f, 5f),
            TutorialPreset.Custom, "double_jump", "공중에서 한 번 더 점프하세요", "Jump");
        AddTrigger(scene, triggers, "Trigger_Attack", new Vector2(120.5f, 7.5f), new Vector2(3f, 5f),
            TutorialPreset.Attack, null, null, null);
    }

    private static void Place(Scene scene, string name, string prefabPath, Vector3 pos, Transform parent)
    {
        Transform t = FindInScene(scene, name);
        if (t == null)
        {
            GameObject go = Spawn(scene, prefabPath, pos, parent);
            if (go == null) return;
            go.name = name;
            return;
        }

        Undo.RecordObject(t, "튜토리얼 맵 다시 그리기");
        t.position = pos;
    }

    private static void Remove(Scene scene, string name)
    {
        Transform t = FindInScene(scene, name);
        if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
    }

    private static GameObject Spawn(Scene scene, string prefabPath, Vector3 pos, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[TutorialMapBuilder] 프리팹 없음: " + prefabPath);
            return null;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go;
    }

    // 이 빌더가 만든 묶음은 다시 실행할 때 비우고 새로 채운다
    private static Transform CleanGroup(Transform parent, string name)
    {
        Transform g = parent.Find(name);
        if (g == null)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        for (int i = g.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(g.GetChild(i).gameObject);
        return g;
    }

    private static void AddTrigger(Scene scene, Transform parent, string name, Vector2 center, Vector2 size,
        TutorialPreset preset, string id, string message, string action)
    {
        GameObject go = Spawn(scene, TriggerPrefab, center, parent);
        if (go == null) return;
        go.name = name;

        BoxCollider2D box = go.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.offset = Vector2.zero;
            box.size = size;
            box.isTrigger = true;
        }

        TutorialTrigger trigger = go.GetComponent<TutorialTrigger>();
        if (trigger == null) return;
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("preset").intValue = (int)preset;
        if (preset == TutorialPreset.Custom)
        {
            so.FindProperty("customId").stringValue = id;
            so.FindProperty("customMessage").stringValue = message;
            so.FindProperty("customAction").stringValue = action;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── 카메라 ─────────────────────────────

    private static void FitCamera(Scene scene)
    {
        Transform confiner = FindInScene(scene, "CameraConfiner");
        BoxCollider2D box = confiner != null ? confiner.GetComponent<BoxCollider2D>() : null;
        if (box == null)
        {
            Debug.LogWarning("[TutorialMapBuilder] CameraConfiner(BoxCollider2D)를 찾지 못해 카메라 영역은 그대로 둡니다.");
            return;
        }

        Undo.RecordObject(confiner, "튜토리얼 맵 다시 그리기");
        Undo.RecordObject(box, "튜토리얼 맵 다시 그리기");
        confiner.position = new Vector3((MinX + MaxX + 1) * 0.5f, (MinY + MaxY + 1) * 0.5f, 0f);
        box.offset = Vector2.zero;
        box.size = new Vector2(W, H);
    }

    // ── 찾기 ───────────────────────────────

    // UI(RectTransform) 안의 같은 이름은 건너뛴다
    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name && !(t is RectTransform)) return t;
        }
        return null;
    }

    // 타일 그리드: Tilemap_Ground 를 가진 Grid
    private static Transform FindGrid(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Grid g in root.GetComponentsInChildren<Grid>(true))
                if (FindTilemap(g.transform, "Tilemap_Ground") != null) return g.transform;
        }
        return null;
    }

    private static Tilemap FindTilemap(Transform grid, string name)
    {
        if (grid == null) return null;
        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true))
            if (tm.name == name) return tm;
        return null;
    }
}
