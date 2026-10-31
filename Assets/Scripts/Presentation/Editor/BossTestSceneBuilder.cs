using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 연출 테스트용 씬을 한 번에 만든다. 메뉴 [Tools/여울잔/보스 테스트 씬 만들기]
///
/// Assets/Scenes/Test/Test_BossRoom.unity 에 이렇게 깔린다 (왼쪽 → 오른쪽):
///   시작 위치 → 제단 → 벽돌·화염벽 → 슬라임 한 마리 → 보스방(골렘)
/// 카메라·UI·배경은 게임과 같은 프리팹(GamePlay_Root, UI_Root, Background_Forest)을 쓰고,
/// Main Camera는 Test_Alpha에서 그대로 복사한다. 바닥은 평평한 임시 상자다.
///
/// 이 씬을 열고 바로 Play 하면 된다. 다시 만들고 싶으면 메뉴를 다시 누르면 덮어쓴다.
/// </summary>
public static class BossTestSceneBuilder
{
    private const string MenuPath = "Tools/여울잔/보스 테스트 씬 만들기";
    private const string SceneFolder = "Assets/Scenes/Test";
    private const string ScenePath = SceneFolder + "/Test_BossRoom.unity";

    private const string AlphaScenePath = "Assets/Scenes/Test_Alpha.unity";
    private const string GameplayRootPath = "Assets/Prefabs/Maps/GamePlay_Root.prefab";
    private const string UiRootPath = "Assets/Prefabs/UI/Core/UI_Root.prefab";
    private const string BackgroundPath = "Assets/Prefabs/Background/Background_Forest.prefab";
    private const string ShrinePath = "Assets/Prefabs/Field_Object/Shrine.prefab";
    private const string BrickPath = "Assets/Prefabs/Field_Object/BreakableBrick.prefab";
    private const string FireBrickPath = "Assets/Prefabs/Field_Object/FireBreakableBrick.prefab";
    private const string SlimePath = "Assets/Prefabs/Enemies/Enemy_Slime.prefab";
    private const string GolemPath = "Assets/Prefabs/Bosses/Boss_Golem.prefab";

    // 배치 (월드 좌표). 바닥 윗면이 y = 0.
    private const float GroundTop = 0f;
    private const float ArenaLeft = -40f;
    private const float ArenaRight = 40f;
    private const float SpawnX = -36f;
    private const float ShrineX = -31f;
    private const float BrickX = -24f;
    private const float FireBrickX = -21f;
    private const float SlimeX = -12f;
    private const float GolemX = 22f;
    private const float GolemRootAboveGround = 1.8f;   // 골렘 문서 기준
    private const float BossRoomHalfWidth = 13f;        // 골렘 회피 공간(좌우 약 10) + 여유
    private const float BossDoorHeight = 9f;

    [MenuItem(MenuPath)]
    private static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        CopyMainCamera(scene);
        GameObject gameplayRoot = InstantiatePrefab(GameplayRootPath, Vector3.zero);
        InstantiatePrefab(UiRootPath, Vector3.zero);
        GameObject background = InstantiatePrefab(BackgroundPath, Vector3.zero);
        MatchAlphaLighting(gameplayRoot, background);

        BuildGround();
        MovePlayerSpawn(new Vector3(SpawnX, GroundTop + 1.5f, 0f));
        FitCameraBounds();
        AddRuinsBackground(background);
        PlaceVines();

        SnapToGround(InstantiatePrefab(ShrinePath, new Vector3(ShrineX, GroundTop + 2f, 0f)));
        SnapToGround(InstantiatePrefab(BrickPath, new Vector3(BrickX, GroundTop + 2f, 0f)));
        SnapToGround(InstantiatePrefab(FireBrickPath, new Vector3(FireBrickX, GroundTop + 2f, 0f)));
        SnapToGround(InstantiatePrefab(SlimePath, new Vector3(SlimeX, GroundTop + 2f, 0f)));

        GameObject golem = InstantiatePrefab(GolemPath, new Vector3(GolemX, GroundTop + GolemRootAboveGround, 0f));
        EnemyHealth golemHealth = golem != null ? golem.GetComponentInChildren<EnemyHealth>() : null;
        if (golemHealth != null) BossRoomTestTool.CreateRoom(golemHealth, BossRoomHalfWidth, BossDoorHeight);
        else Debug.LogWarning("[BossTestSceneBuilder] 골렘 프리팹이나 EnemyHealth를 찾지 못해 보스방을 만들지 않았어요.");

        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets/Scenes", "Test");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[BossTestSceneBuilder] 테스트 씬을 만들었어요: " + ScenePath + "  (바로 Play 하면 됩니다)");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanBuild() => !EditorApplication.isPlaying;

    // ───────────── 만드는 도구 ─────────────

    private static GameObject InstantiatePrefab(string path, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[BossTestSceneBuilder] 프리팹이 없어요: " + path);
            return null;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = position;
        return go;
    }

    // Test_Alpha의 Main Camera(CinemachineBrain, 화면 효과 등)를 그대로 가져온다.
    private static void CopyMainCamera(Scene target)
    {
        Scene alpha = EditorSceneManager.OpenScene(AlphaScenePath, OpenSceneMode.Additive);
        GameObject source = null;
        foreach (GameObject root in alpha.GetRootGameObjects())
        {
            if (root.name != "Main Camera") continue;
            source = root;
            break;
        }

        if (source != null)
        {
            GameObject copy = Object.Instantiate(source);
            copy.name = "Main Camera";
            SceneManager.MoveGameObjectToScene(copy, target);
        }
        else
        {
            GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(CinemachineBrain));
            cam.tag = "MainCamera";
            Camera c = cam.GetComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            SceneManager.MoveGameObjectToScene(cam, target);
        }

        EditorSceneManager.CloseScene(alpha, true);
        SceneManager.SetActiveScene(target);
    }

    // Test_Alpha와 같은 조명: 배경 쪽 전역 조명은 끄고, GamePlay_Root 전역 조명 세기 0.15
    // (둘 다 켜져 있으면 "More than one global light" 경고가 나고 밝기도 달라진다)
    private const float AlphaGlobalLightIntensity = 0.15f;

    private static void MatchAlphaLighting(GameObject gameplayRoot, GameObject background)
    {
        if (background != null)
        {
            foreach (Light2D light in background.GetComponentsInChildren<Light2D>(true))
                if (light.lightType == Light2D.LightType.Global) light.gameObject.SetActive(false);
        }

        if (gameplayRoot != null)
        {
            foreach (Light2D light in gameplayRoot.GetComponentsInChildren<Light2D>(true))
                if (light.lightType == Light2D.LightType.Global) light.intensity = AlphaGlobalLightIntensity;
        }
    }

    // 평평한 바닥과 양 끝 벽 (Ground 레이어)
    // 땅: 튜토리얼처럼 Grid 프리팹 타일맵에 검은 바위(Charcoal) 타일을 깐다. 보스방 바닥에는 풀을 안 얹는다.
    // 충돌은 타일맵 것과 같은 모양의 보이지 않는 상자도 같이 둔다 (에디터에서 바로 바닥 높이를 잴 수 있게).
    private const string GridPath = "Assets/Prefabs/Maps/Grid.prefab";
    private const string RockDir = "Assets/Tiles/Ground_Rock/Charcoal/";
    private const string GrassDir = "Assets/Tiles/Grass/";
    private const int FloorDepth = 6;     // 바닥 두께 (칸)
    private const int WallHeight = 18;

    private static void BuildGround()
    {
        float width = ArenaRight - ArenaLeft;
        float midX = (ArenaLeft + ArenaRight) * 0.5f;

        bool tiled = PaintTiles();
        CreateBlock("Ground", new Vector3(midX, GroundTop - 2f, 0f), new Vector2(width + 4f, 4f), !tiled);
        CreateBlock("Wall_Left", new Vector3(ArenaLeft - 1f, GroundTop + 7f, 0f), new Vector2(2f, 18f), !tiled);
        CreateBlock("Wall_Right", new Vector3(ArenaRight + 1f, GroundTop + 7f, 0f), new Vector2(2f, 18f), !tiled);
    }

    private static bool PaintTiles()
    {
        GameObject grid = InstantiatePrefab(GridPath, Vector3.zero);
        if (grid == null) return false;
        Tilemap ground = null, grass = null;
        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true))
        {
            tm.ClearAllTiles();
            if (tm.name == "Tilemap_Ground") ground = tm;
            else if (tm.name == "Tilemap_Grass") grass = tm;
        }
        TileBase[] rock =
        {
            AssetDatabase.LoadAssetAtPath<TileBase>(RockDir + "Ground_TopLeft.asset"),
            AssetDatabase.LoadAssetAtPath<TileBase>(RockDir + "Ground_TopRight.asset"),
            AssetDatabase.LoadAssetAtPath<TileBase>(RockDir + "Ground_BottomLeft.asset"),
            AssetDatabase.LoadAssetAtPath<TileBase>(RockDir + "Ground_BottomRight.asset"),
        };
        if (ground == null || System.Array.Exists(rock, t => t == null))
        {
            Debug.LogWarning("[BossTestSceneBuilder] Grid 타일맵이나 Charcoal 타일을 못 찾아서 회색 상자로 땅을 만듭니다.");
            Object.DestroyImmediate(grid);
            return false;
        }
        System.Collections.Generic.List<TileBase> grassTops = new System.Collections.Generic.List<TileBase>();
        for (int i = 1; i <= 5; i++)
        {
            TileBase g = AssetDatabase.LoadAssetAtPath<TileBase>(GrassDir + "Grass_Top_V0" + i + ".asset");
            if (g != null) grassTops.Add(g);
        }

        int top = Mathf.RoundToInt(GroundTop);
        int left = Mathf.RoundToInt(ArenaLeft), right = Mathf.RoundToInt(ArenaRight);
        float roomL = GolemX - BossRoomHalfWidth - 1f, roomR = GolemX + BossRoomHalfWidth + 1f;

        bool Solid(int x, int y)
        {
            bool floor = y < top && y >= top - FloorDepth && x >= left - 4 && x <= right + 3;
            bool wall = y >= top && y < top + WallHeight && ((x >= left - 2 && x <= left - 1) || (x >= right && x <= right + 1));
            return floor || wall;
        }
        Vector3Int Cell(Tilemap tm, int x, int y) => tm.WorldToCell(new Vector3(x + 0.5f, y + 0.5f, 0f));

        var gPos = new System.Collections.Generic.List<Vector3Int>(); var gTiles = new System.Collections.Generic.List<TileBase>();
        var wPos = new System.Collections.Generic.List<Vector3Int>(); var wTiles = new System.Collections.Generic.List<TileBase>();
        for (int x = left - 4; x <= right + 3; x++)
            for (int y = top - FloorDepth; y < top + WallHeight; y++)
            {
                if (!Solid(x, y)) continue;
                // 2×2 그림: 바닥 맨 윗줄이 그림의 위쪽이 되게
                int rowFromTop = top - 1 - y;
                int idx = ((rowFromTop & 1) == 0 ? 0 : 2) + ((x & 1) == 0 ? 0 : 1);
                gPos.Add(Cell(ground, x, y));
                gTiles.Add(rock[idx]);

                // 바닥 윗면 풀 (보스방 바닥은 빼고)
                bool exposed = y == top - 1 && !Solid(x, y + 1);
                bool inRoom = x + 1 > roomL && x < roomR;
                if (grass != null && grassTops.Count > 0 && exposed && !inRoom)
                {
                    wPos.Add(Cell(grass, x, y));
                    wTiles.Add(grassTops[Mathf.Abs(x * 7 + 3) % grassTops.Count]);
                }
            }
        ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
        if (grass != null) grass.SetTiles(wPos.ToArray(), wTiles.ToArray());
        foreach (Tilemap tm in grid.GetComponentsInChildren<Tilemap>(true)) tm.CompressBounds();

        TilemapCollider2D tc = ground.GetComponent<TilemapCollider2D>();
        if (tc != null) tc.ProcessTilemapChanges();
        CompositeCollider2D cc = ground.GetComponent<CompositeCollider2D>();
        if (cc != null) cc.GenerateGeometry();

        // 땅 깊이 그늘 (튜토리얼과 같은 값)
        TilemapRenderer tr = ground.GetComponent<TilemapRenderer>();
        if (tr != null) tr.mode = TilemapRenderer.Mode.Chunk;
        GroundDepthShading depth = ground.GetComponent<GroundDepthShading>();
        if (depth != null)
        {
            depth.surfaceWidth = 0.25f;
            depth.fadeDistance = 2.5f;
            depth.maxDarkness = 0.98f;
            depth.falloffPower = 0.8f;
            depth.enabled = false;
            depth.enabled = true;
        }
        return true;
    }

    // 근경 덩굴: 화면 위쪽에서 늘어진다 (땅 뒤 여러 개 + 캐릭터 앞 몇 개, 보스방 가운데는 앞 덩굴을 피함)
    private static readonly (string prefab, float x, float topY, float scale)[] Vines =
    {
        ("FG_Back_Ivy_Teal_1",  -35f, 11.5f, 1.0f),
        ("FG_Back_Ivy_Light_2", -27f, 12.0f, 1.1f),
        ("FG_Back_Ivy_Dark_3",  -19f, 11.0f, 0.9f),
        ("FG_Back_Ivy_Teal_5",   -9f, 12.0f, 1.0f),
        ("FG_Back_Ivy_Light_4",   2f, 11.5f, 1.1f),
        ("FG_Back_Ivy_Dark_1",   11f, 11.0f, 0.9f),
        ("FG_Back_Ivy_Teal_6",   19f, 12.0f, 1.2f),
        ("FG_Back_Ivy_Light_6",  27f, 11.5f, 1.0f),
        ("FG_Back_Ivy_Teal_4",   33f, 12.0f, 1.1f),
        ("FG_Front_Ivy_Dark_2", -38f, 11.0f, 1.2f),
        ("FG_Front_Ivy_Plain_1",-15f, 10.5f, 1.0f),
        ("FG_Front_Ivy_Dark_5",  38f, 11.0f, 1.2f),
    };

    private static void PlaceVines()
    {
        GameObject root = new GameObject("Foreground_Vines");
        foreach (var v in Vines)
        {
            GameObject go = InstantiatePrefab("Assets/Prefabs/Foreground/" + v.prefab + ".prefab", new Vector3(v.x, GroundTop + v.topY, 0f));
            if (go == null) continue;
            go.transform.SetParent(root.transform, true);
            float flip = Mathf.Abs(Mathf.RoundToInt(v.x)) % 2 == 0 ? 1f : -1f;
            go.transform.localScale = new Vector3(v.scale * flip, v.scale, 1f);
        }
    }

    private static void CreateBlock(string name, Vector3 center, Vector2 size, bool visible = true)
    {
        GameObject go = new GameObject(name);
        go.transform.position = center;
        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) go.layer = ground;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        sr.sortingLayerName = "Ground";
        sr.enabled = visible;   // 타일을 깔았으면 충돌용으로만 남긴다

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    // 이 씬 전용 시작 위치를 만들어 SceneSpawnManager에 연결한다.
    // 기본 시작 위치는 프리팹(SpawnPoint.prefab)을 가리키고 있어서 옮기면 다른 씬까지 바뀐다. 그래서 건드리지 않는다.
    private static void MovePlayerSpawn(Vector3 position)
    {
        SceneSpawnManager spawner = Object.FindAnyObjectByType<SceneSpawnManager>();
        if (spawner == null)
        {
            Debug.LogWarning("[BossTestSceneBuilder] SceneSpawnManager가 없어요. GamePlay_Root를 확인해 주세요.");
            return;
        }

        Transform point = new GameObject("TestSpawnPoint").transform;
        point.position = position;

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty prop = so.FindProperty("defaultSpawnPoint");
        if (prop == null) return;
        prop.objectReferenceValue = point;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 카메라가 테스트 바닥 밖으로 나가지 않게 경계를 새로 잡는다.
    // 폐허 배경 (Resources/Presentation/Backgrounds/BG_Boss_Ruins). 숲 배경은 끈다.
    // 카메라 이동 범위에 맞춰 그림 끝이 안 보이게 따라 움직인다 (FieldPanoramaBackground).
    private const string RuinsPath = "Assets/Resources/Presentation/Backgrounds/BG_Boss_Ruins.png";

    private static void AddRuinsBackground(GameObject forest)
    {
        Sprite ruins = AssetDatabase.LoadAssetAtPath<Sprite>(RuinsPath);
        if (ruins == null) { Debug.LogWarning("[BossTestSceneBuilder] 폐허 배경 그림이 없어서 숲 배경을 그대로 둡니다."); return; }
        if (forest != null) forest.SetActive(false);
        GameObject go = new GameObject("Background_Ruins");
        FieldPanoramaBackground bg = go.AddComponent<FieldPanoramaBackground>();
        SerializedObject so = new SerializedObject(bg);
        so.FindProperty("sprite").objectReferenceValue = ruins;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FitCameraBounds()
    {
        GameObject bounds = new GameObject("TestCameraBounds");
        PolygonCollider2D poly = bounds.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        float l = ArenaLeft - 2f, r = ArenaRight + 2f, b = GroundTop - 4f, t = GroundTop + 16f;
        poly.SetPath(0, new[] { new Vector2(l, b), new Vector2(r, b), new Vector2(r, t), new Vector2(l, t) });

        foreach (CinemachineConfiner2D confiner in Object.FindObjectsByType<CinemachineConfiner2D>(FindObjectsInactive.Include))
        {
            SerializedObject so = new SerializedObject(confiner);
            SerializedProperty shape = so.FindProperty("BoundingShape2D");
            if (shape == null) continue;
            shape.objectReferenceValue = poly;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // 그림·콜라이더 아래쪽이 바닥 윗면에 닿게 내린다.
    private static void SnapToGround(GameObject go)
    {
        if (go == null) return;
        Physics2D.SyncTransforms();

        bool found = false;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>())
        {
            if (c.isTrigger) continue;
            if (!found) { b = c.bounds; found = true; }
            else b.Encapsulate(c.bounds);
        }
        if (!found)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!found) { b = r.bounds; found = true; }
                else b.Encapsulate(r.bounds);
            }
        }
        if (!found) return;

        go.transform.position += new Vector3(0f, GroundTop - b.min.y, 0f);
    }
}
