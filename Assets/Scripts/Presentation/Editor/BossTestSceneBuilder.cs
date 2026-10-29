using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        InstantiatePrefab(GameplayRootPath, Vector3.zero);
        InstantiatePrefab(UiRootPath, Vector3.zero);
        InstantiatePrefab(BackgroundPath, Vector3.zero);

        BuildGround();
        MovePlayerSpawn(new Vector3(SpawnX, GroundTop + 1.5f, 0f));
        FitCameraBounds();

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

    // 평평한 바닥과 양 끝 벽 (Ground 레이어)
    private static void BuildGround()
    {
        float width = ArenaRight - ArenaLeft;
        float midX = (ArenaLeft + ArenaRight) * 0.5f;

        CreateBlock("Ground", new Vector3(midX, GroundTop - 2f, 0f), new Vector2(width + 4f, 4f));
        CreateBlock("Wall_Left", new Vector3(ArenaLeft - 1f, GroundTop + 7f, 0f), new Vector2(2f, 18f));
        CreateBlock("Wall_Right", new Vector3(ArenaRight + 1f, GroundTop + 7f, 0f), new Vector2(2f, 18f));
    }

    private static void CreateBlock(string name, Vector3 center, Vector2 size)
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

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    // SceneSpawnManager의 기본 시작 위치를 옮긴다 (없으면 새로 만든다).
    private static void MovePlayerSpawn(Vector3 position)
    {
        SceneSpawnManager spawner = Object.FindAnyObjectByType<SceneSpawnManager>();
        if (spawner == null)
        {
            Debug.LogWarning("[BossTestSceneBuilder] SceneSpawnManager가 없어요. GamePlay_Root를 확인해 주세요.");
            return;
        }

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty prop = so.FindProperty("defaultSpawnPoint");
        Transform point = prop != null ? prop.objectReferenceValue as Transform : null;
        if (point == null)
        {
            point = new GameObject("TestSpawnPoint").transform;
            if (prop != null)
            {
                prop.objectReferenceValue = point;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        point.position = position;
    }

    // 카메라가 테스트 바닥 밖으로 나가지 않게 경계를 새로 잡는다.
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
