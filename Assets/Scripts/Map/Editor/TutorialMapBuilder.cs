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
/// 인트로에서 이어지는 동굴. 시작은 천장이 낮고 어둡고, 오른쪽으로 갈수록 천장이 높아지고 밝아지다가
/// 전투 구역(x 126~)에서 동굴 밖으로 나간다 (천장 없음). 밝기는 CaveLightRamp가 카메라 위치로 바꾼다.
///
/// 흐름 (왼쪽 → 오른쪽)
///   1. 시작        이동·점프 안내 (TutorialDirector가 시작할 때 자동으로 띄움)
///   2. 점프 연습   계단 두 번 + 작은 구덩이 두 개
///   3. 상자        발판 위 상자에서 더블 점프 유물(깃털구름) 얻기 (상자 안내 자동)
///   4. 제단        불 밝히고 유물 장착 (제단 안내 자동). 장착해야 다음 벽을 넘을 수 있다
///   5. 더블 점프 벽 한 번 점프로는 못 넘는 높이 (+6칸)
///   6. 점프맵      구덩이 위 발판 4개, 마지막은 더블 점프. 떨어지면 왼쪽 사다리 발판으로 다시 올라온다
///   7. 전투        안 움직이는 슬라임 3마리 (공격 안내)
///   8. 출구        포탈 (원래 설정대로 F_01로 감). 카메라 영역 밖에 있어서 화면에는 안 보이고,
///                  오른쪽으로 걸어 나가면 넘어간다 (원래 튜토리얼 맵과 같은 방식)
///
/// 타일 규칙 (기존 맵과 같게)
///   땅   Tilemap_Ground 에 Ground_Rock 2×2 그림 (짝수 x = 왼쪽, 짝수 y = 위쪽).
///        빈 곳에 닿는 덩어리는 Blue, 안쪽은 Charcoal.
///   풀   Tilemap_Grass 에 Grass_Top. 땅 윗면과 같은 칸에 겹쳐 그린다.
///   발판 TIleMap_Platform 에 Platform_Rock (왼쪽·가운데·오른쪽). 아래에서 뚫고 올라갈 수 있는 발판.
///   가시 프리팹으로 뺄 예정이라 쓰지 않는다. Tilemap_Spike 는 비운다.
///
/// 높이 기준: 점프 약 4칸, 더블 점프 약 8칸 (PlayerMove jumpForce 20, 중력 5).
/// 좌표는 전부 월드 기준. Grid 프리팹의 Tilemap_Ground·Grass는 (1, 1) 옮겨져 있어서
/// 칠할 때 타일맵마다 WorldToCell로 바꿔 넣는다 (오브젝트와 땅 높이가 어긋나지 않게).
///
/// 다시 실행하면 Grid 안 타일을 전부 지우고 새로 그린다.
/// 상자·제단·포탈·시작 위치는 이름으로 찾아서 자리만 옮긴다 (없으면 프리팹으로 새로 놓는다).
/// 보주 줍기(OrbPickup)는 인트로 연출에서 보주를 주므로 지운다.
/// 슬라임과 안내 영역은 Field_Object/Tutorial_Enemies, Tutorial_Triggers 아래에 새로 만든다.
/// 카메라 영역(CameraConfiner)은 맵 크기에 맞춰 늘리되, 출구 포탈 쪽(x 140~)은 뺀다.
/// 배경: 숲 배경(Background_Forest)은 끄고 동굴 배경(Background_Cave, CavePanoramaBackground)을 쓴다.
/// 안개: Tutorial_Fog (CaveFog). 화면 전체 옅은 안개 + 바닥 쪽 낮은 안개, 출구로 갈수록 옅어짐.
/// 근경: 근경 배치판(claude.ai 아티팩트)에서 놓은 자리 그대로 (Assets/Sprites/Backgrounds/Tutorial/TutorialForeground.json).
///       Foreground_Cave 아래에 먹 실루엣(바위·소나무·풀·돌탑)을 놓는다 (ForegroundParallax, GroundCover 레이어).
/// 그림: Assets/Sprites/Backgrounds/Tutorial/
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
    // 카메라가 보여주는 범위. 원래 튜토리얼처럼 바닥 아래·천장 위는 거의 안 보이게 붙인다.
    private const int CameraRight = 140;   // 출구 포탈(x 145)은 이 밖이라 화면에 안 나온다
    private const int CameraBottom = 0;    // 가장 낮은 바닥(시작, 높이 1) 아래로 1칸만
    private const int CameraTop = 22;      // 천장(21~) 1칸만

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
        AddLightRamp(scene);
        SetupBackground(scene);
        PlaceForeground(scene);   // 근경 배치판에서 저장한 배치 (TutorialForeground.json)

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

        // 천장: 동굴 안은 낮고, 출구 쪽으로 갈수록 높아진다. 아래쪽 y는 홀수 (2×2 그림 맞춤)
        // 점프 최고 높이 + 키(2.7)보다 높게 잡아서 점프하다 머리를 박지 않게 했다.
        Ceiling(0, 21, 9);       // 1 시작 (좁은 동굴)
        Ceiling(22, 35, 11);     // 2 계단
        Ceiling(36, 53, 13);     //   구덩이
        Ceiling(54, 77, 17);     // 3·4 상자·제단 방
        Ceiling(78, 81, 19);     // 5 더블 점프 벽 위
        Ceiling(82, 121, 21);    // 6 점프맵 (큰 동굴)
        Ceiling(122, 125, 23);   //   동굴 입구
                                 // 126~ 천장 없음 (밖)
        Fill(62, 69, 15, 16);    // 종유석 (제단 방): 계단처럼 위가 넓고 아래가 좁게
        Fill(64, 67, 13, 14);    //   점프해도 머리가 안 닿는 높이 (바닥 5 + 점프 4 + 키 2.7 < 13)

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
    private static void Ceiling(int x0, int x1, int bottom) => Fill(x0, x1, bottom, MaxY);

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

                // 전부 Blue. 안쪽이 어두워지는 건 GroundDepthShading이 부드럽게 처리한다
                // (전에는 안쪽 덩어리를 Charcoal로 바꿨는데, 2칸 단위로 색이 뚝 끊겨 보였다)
                TileBase[] set = blue;
                int idx = ((y & 1) == 0 ? 0 : 2) + ((x & 1) == 0 ? 0 : 1);   // 위왼·위오·아래왼·아래오
                gPos.Add(ToCell(ground, x, y));
                gTiles.Add(set[idx]);

                // 윗면 풀 (끊기지 않게 전부 깔고, 모양만 섞는다)
                if (y < MaxY && !IsSolid(x, y + 1))
                {
                    wPos.Add(ToCell(grass, x, y));
                    wTiles.Add(grassTops[Hash(x) % grassTops.Length]);
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
                pPos.Add(ToCell(platform, x0 + k, y));
                pTiles.Add(t);
            }
        }
        platform.SetTiles(pPos.ToArray(), pTiles.ToArray());

        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true)) tm.CompressBounds();

        ApplyDepthShading(ground);
        return true;
    }

    // 땅 깊이 그늘: 윗면에서 안쪽으로 서서히 까맣게. 벽·천장도 안쪽으로 갈수록 어두워진다.
    // Grid 프리팹 기본값(1칸 만에 까매짐)은 너무 뚝 끊겨서, 이 씬에서만 바꾼다 (프리팹은 그대로).
    private static void ApplyDepthShading(Tilemap ground)
    {
        GroundDepthShading depth = ground != null ? ground.GetComponent<GroundDepthShading>() : null;
        if (depth == null) return;
        Undo.RecordObject(depth, "튜토리얼 맵 다시 그리기");
        depth.surfaceWidth = 0.25f;  // 윗면 밝은 띠 (얇게: 가장자리 가까이부터 어두워짐)
        depth.fadeDistance = 2.5f;   // 이 칸 수에 걸쳐 어두워짐
        depth.maxDarkness = 0.98f;
        depth.falloffPower = 0.8f;   // 1보다 작으면 처음부터 빨리 어두워짐
        EditorUtility.SetDirty(depth);
        // 값만 바꾸면 그늘이 바로 다시 계산되지 않아서, 껐다 켜서 다시 그리게 한다
        depth.enabled = false;
        depth.enabled = true;
    }

    // 월드 칸 (x, y) = [x, x+1] × [y, y+1] 을 그 타일맵의 칸 번호로
    private static Vector3Int ToCell(Tilemap tm, int x, int y) => tm.WorldToCell(new Vector3(x + 0.5f, y + 0.5f, 0f));

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
    // 높이 = 서 있는 높이 + 원래 Tutorial 씬에서 쓰던 띄움값 (상자 +0.5, 제단 +2.11, 포탈 +1.5)

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

        // 1 시작. 시작 위치는 캐릭터 몸 가운데 기준이라 바닥(1)보다 2.5 위에 둔다 (낮으면 땅에 끼어서 시작함)
        Place(scene, "Spawn_From_START", SpawnPrefab, new Vector3(5f, 3.5f, 0f), field);
        Remove(scene, "OrbPickup");   // 보주는 인트로 연출에서 받는다
        Place(scene, "RelicChest_FeatherCloud", ChestPrefab, new Vector3(60f, 8.5f, 0f), field);  // 3 발판 위 (높이 8)
        Place(scene, "Shrine", ShrinePrefab, new Vector3(70f, 7.11f, 0f), field);           // 4 (바닥 높이 5)
        Place(scene, "AutoScenePortal", PortalPrefab, new Vector3(145f, 6.5f, 0f), field);  // 8

        // 7 안 움직이는 슬라임
        Transform enemies = CleanGroup(field, "Tutorial_Enemies");
        foreach (float x in new[] { 127f, 132f, 137f })
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
        confiner.position = new Vector3((MinX + CameraRight) * 0.5f, (CameraBottom + CameraTop) * 0.5f, 0f);
        box.offset = Vector2.zero;
        box.size = new Vector2(CameraRight - MinX, CameraTop - CameraBottom);
    }

    // 동굴 안은 어둡고 출구로 갈수록 밝게
    private static void AddLightRamp(Scene scene)
    {
        Transform t = FindInScene(scene, "Tutorial_Light");
        if (t == null)
        {
            GameObject go = new GameObject("Tutorial_Light");
            Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
            SceneManager.MoveGameObjectToScene(go, scene);
            t = go.transform;
        }
        if (t.GetComponent<CaveLightRamp>() == null) Undo.AddComponent<CaveLightRamp>(t.gameObject);

        // 안개 (화면 전체 옅은 안개 + 바닥 쪽 낮은 안개)
        Transform fog = FindOrCreateRoot(scene, "Tutorial_Fog");
        CaveFog fogComp = fog.GetComponent<CaveFog>();
        if (fogComp == null) fogComp = Undo.AddComponent<CaveFog>(fog.gameObject);
        // 화면 아래 그늘은 끈다 (땅이 깊이 그늘로 어두워지니까 화면에 붙은 그늘은 어색함)
        SerializedObject fso = new SerializedObject(fogComp);
        fso.FindProperty("bottomShadeAlpha").floatValue = 0f;
        fso.ApplyModifiedProperties();
    }

    // ── 배경·근경 ──────────────────────────

    private const string ArtDir = "Assets/Sprites/Backgrounds/Tutorial/";


    private static void SetupBackground(Scene scene)
    {
        Transform forest = FindInScene(scene, "Background_Forest");
        if (forest != null && forest.gameObject.activeSelf)
        {
            Undo.RecordObject(forest.gameObject, "튜토리얼 맵 다시 그리기");
            forest.gameObject.SetActive(false);
        }

        Transform cave = FindOrCreateRoot(scene, "Background_Cave");
        CavePanoramaBackground bg = cave.GetComponent<CavePanoramaBackground>();
        if (bg == null) bg = Undo.AddComponent<CavePanoramaBackground>(cave.gameObject);

        SerializedObject so = new SerializedObject(bg);
        // 동굴 안 → 출구가 한 장에 다 있는 그림. 끝에서 출구 구멍(그림의 77% 지점)이 화면 가운데.
        // 그림은 화면 높이의 65%만 차지하고 위쪽은 어두운 동굴 천장색으로 채워 두었다
        // (그림이 너무 크게 확대되지 않게 하면서 화면 위아래가 비지 않게).
        so.FindProperty("insideSprite").objectReferenceValue = null;
        so.FindProperty("exitSprite").objectReferenceValue = LoadSprite("BG_Cave_Panorama");
        so.FindProperty("exitFocus").floatValue = 0.79f;
        so.FindProperty("viewLeftX").floatValue = MinX;
        so.FindProperty("viewRightX").floatValue = CameraRight;
        so.ApplyModifiedProperties();
    }

    private static void RemoveForeground(Scene scene)
    {
        Transform root = FindInScene(scene, "Foreground_Cave");
        if (root != null) Undo.DestroyObjectImmediate(root.gameObject);
    }

    private const string ForegroundJson = ArtDir + "TutorialForeground.json";

    // 근경 배치판(claude.ai 아티팩트)에서 저장한 배치. x = 가운데, y = 그림 아래쪽, h = 높이 (전부 월드 칸)
    [System.Serializable] public class FgItem { public string sprite; public float x, y, h; public bool flip; public string layer; }
    [System.Serializable] public class FgLayout { public FgItem[] items; }

    // 근경만 다시 놓기 (맵은 그대로, 땅 그늘 값도 같이 맞춤). 배치판에서 바꾼 뒤 이것만 눌러도 된다.
    [MenuItem("Tools/Map/튜토리얼 근경만 다시 놓기")]
    private static void RebuildForegroundOnly()
    {
        if (!OpenTutorialScene()) return;
        Scene scene = SceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        PlaceForeground(scene);
        ApplyDepthShading(FindTilemap(FindGrid(scene), "Tilemap_Ground"));
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // 앞: 캐릭터 앞 (GroundCover). 뒤: 땅 타일 바로 뒤 (Ground 레이어 아래쪽). 둘 다 배치판에 놓은 자리에 그대로 고정
    private static void PlaceForeground(Scene scene)
    {
        RemoveForeground(scene);
        TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(ForegroundJson);
        if (json == null)
        {
            Debug.LogWarning("[TutorialMapBuilder] 근경 배치 파일이 없습니다: " + ForegroundJson);
            return;
        }
        FgLayout layout = JsonUtility.FromJson<FgLayout>(json.text);
        if (layout == null || layout.items == null || layout.items.Length == 0)
        {
            Debug.LogWarning("[TutorialMapBuilder] 근경 배치 파일에 근경이 없습니다.");
            return;
        }
        int placed = 0;

        Transform root = FindOrCreateRoot(scene, "Foreground_Cave");
        Material unlit = Resources.Load<Material>("Presentation/M_VfxSprite");
        foreach (FgItem it in layout.items)
        {
            Sprite s = LoadSprite(it.sprite);
            if (s == null || it.h <= 0f) continue;
            bool back = it.layer == "back";

            GameObject go = new GameObject(it.sprite);
            Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
            go.transform.SetParent(root, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.flipX = it.flip;
            if (unlit != null) sr.sharedMaterial = unlit;
            sr.sortingLayerName = back ? "Ground" : "GroundCover";
            sr.sortingOrder = back ? -5 : 10;

            float scale = it.h / s.bounds.size.y;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.position = new Vector3(it.x - s.bounds.center.x * scale, it.y - s.bounds.min.y * scale, 0f);

            // 패럴랙스는 안 붙인다: 붙이면 카메라 위치에 따라 배치판에서 놓은 자리와 어긋나 보인다
            placed++;
        }
        Debug.Log($"[TutorialMapBuilder] 근경 {placed}개를 놓았습니다 (Foreground_Cave). 씬을 저장하세요.");
    }

    private static Sprite LoadSprite(string name)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
        if (s == null) Debug.LogWarning("[TutorialMapBuilder] 그림 없음 (Sprite로 불러와졌는지 확인): " + ArtDir + name + ".png");
        return s;
    }

    private static Transform FindOrCreateRoot(Scene scene, string name)
    {
        Transform t = FindInScene(scene, name);
        if (t != null) return t;
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "튜토리얼 맵 다시 그리기");
        SceneManager.MoveGameObjectToScene(go, scene);
        return go.transform;
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
