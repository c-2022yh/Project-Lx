using UnityEditor;
using UnityEngine;

/// <summary>
/// 진짜 보스가 오기 전에 보스방 연출을 시험해 보는 도구.
/// 씬에서 적 하나를 선택하고 메뉴 [Tools/여울잔/보스방 테스트 만들기]를 누르면
/// 그 적을 보스 대역으로 삼아 트리거·문 두 개·BossMarker를 만들어 준다.
///
/// 시험용이니 씬은 저장하지 말 것. Ctrl+Z로 되돌릴 수도 있다.
/// </summary>
public static class BossRoomTestTool
{
    private const string MenuPath = "Tools/여울잔/보스방 테스트 만들기 (선택한 적)";
    private const float HalfWidth = 8f;
    private const float DoorHeight = 5f;
    private const float TestBossHp = 150f;

    [MenuItem(MenuPath)]
    private static void Create()
    {
        EnemyHealth boss = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<EnemyHealth>()
            : null;

        if (boss == null)
        {
            EditorUtility.DisplayDialog("보스방 테스트", "씬에서 보스 대역으로 쓸 적(EnemyHealth가 있는 오브젝트)을 먼저 선택해 주세요.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("보스방 테스트 만들기");
        int group = Undo.GetCurrentGroup();

        GameObject room = CreateRoom(boss, HalfWidth, DoorHeight).gameObject;

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = room;
        Debug.Log("[BossRoomTestTool] 보스방 테스트를 만들었어요. 플레이어가 빨간 상자 안으로 들어가면 시작합니다. 씬은 저장하지 마세요.", room);
    }

    /// <summary>
    /// boss를 가운데 두고 트리거·양쪽 문·BossMarker를 붙인 보스방을 만든다. 보스 테스트 씬 만들기에서도 쓴다.
    /// </summary>
    public static BossRoom CreateRoom(EnemyHealth boss, float halfWidth, float doorHeight)
    {
        Physics2D.SyncTransforms();   // 방금 만든 바닥도 레이캐스트에 잡히게
        Vector3 center = boss.transform.position;
        float groundY = FindGroundY(center);

        // 보스 대역 설정
        BossMarker marker = boss.GetComponent<BossMarker>();
        if (marker == null) marker = Undo.AddComponent<BossMarker>(boss.gameObject);
        SerializedObject markerSo = new SerializedObject(marker);
        markerSo.FindProperty("bossName").stringValue = "시험용 보스";
        markerSo.FindProperty("bossTitle").stringValue = "보스방 테스트";
        markerSo.ApplyModifiedProperties();

        SerializedObject bossSo = new SerializedObject(boss);
        SerializedProperty hp = bossSo.FindProperty("maxHp");
        if (hp != null)
        {
            hp.floatValue = Mathf.Max(hp.floatValue, TestBossHp);
            bossSo.ApplyModifiedProperties();
        }

        // 방
        GameObject room = new GameObject("BossRoom_Test");
        Undo.RegisterCreatedObjectUndo(room, "보스방 테스트");
        room.transform.position = new Vector3(center.x, groundY + doorHeight * 0.5f, 0f);

        BoxCollider2D trigger = room.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2((halfWidth - 2f) * 2f, doorHeight);

        Transform left = CreateDoor("Door_Left", room.transform, new Vector3(center.x - halfWidth, groundY + doorHeight * 0.5f, 0f), doorHeight);
        Transform right = CreateDoor("Door_Right", room.transform, new Vector3(center.x + halfWidth, groundY + doorHeight * 0.5f, 0f), doorHeight);

        BossRoom bossRoom = room.AddComponent<BossRoom>();
        SerializedObject roomSo = new SerializedObject(bossRoom);
        roomSo.FindProperty("boss").objectReferenceValue = boss;
        SerializedProperty doors = roomSo.FindProperty("doors");
        doors.arraySize = 2;
        doors.GetArrayElementAtIndex(0).objectReferenceValue = left;
        doors.GetArrayElementAtIndex(1).objectReferenceValue = right;
        roomSo.FindProperty("doorDropHeight").floatValue = doorHeight;
        roomSo.ApplyModifiedProperties();

        return bossRoom;
    }

    [MenuItem(MenuPath, true)]
    private static bool CanCreate() => !EditorApplication.isPlaying;

    private static Transform CreateDoor(string name, Transform parent, Vector3 position, float doorHeight)
    {
        GameObject door = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(door, "보스방 문");
        door.transform.SetParent(parent, true);
        door.transform.position = position;

        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) door.layer = ground;

        SpriteRenderer sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1f, doorHeight);
        sr.color = new Color(0.16f, 0.14f, 0.13f, 1f);
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 5;

        BoxCollider2D col = door.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, doorHeight);

        return door.transform;
    }

    // 적 발밑의 바닥 높이. 못 찾으면 적 위치 조금 아래.
    private static float FindGroundY(Vector3 from)
    {
        int mask = LayerMask.GetMask("Ground");
        if (mask == 0) mask = Physics2D.DefaultRaycastLayers;
        RaycastHit2D hit = Physics2D.Raycast(from + Vector3.up * 0.5f, Vector2.down, 30f, mask);
        return hit.collider != null ? hit.point.y : from.y - 1f;
    }
}
