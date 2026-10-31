using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools/Map/튜토리얼 슬라임 방향 바꾸기
/// 지금 열린 씬의 Tutorial_Enemies 아래 슬라임 전부에 TutorialSlimeFacing을 붙이고,
/// 누를 때마다 왼쪽 ↔ 오른쪽으로 바꾼다. 처음 누르면 왼쪽. 끝나면 Ctrl+S.
/// 튜토리얼 맵을 다시 그리면(슬라임을 새로 만들면) 한 번 더 눌러야 한다.
/// </summary>
public static class TutorialSlimeFacingMenu
{
    private const string MenuPath = "Tools/Map/튜토리얼 슬라임 방향 바꾸기";
    private const string GroupName = "Tutorial_Enemies";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform group = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if ((group = FindDeep(root.transform, GroupName)) != null) break;
        if (group == null)
        {
            EditorUtility.DisplayDialog("슬라임 방향", "이 씬에서 " + GroupName + " 를 찾지 못했어요. Tutorial 씬을 열고 눌러 주세요.", "확인");
            return;
        }

        EnemyAnimation[] slimes = group.GetComponentsInChildren<EnemyAnimation>(true);
        if (slimes.Length == 0) return;

        // 처음(아직 안 붙음)이면 왼쪽, 붙어 있으면 반대로
        TutorialSlimeFacing first = slimes[0].GetComponent<TutorialSlimeFacing>();
        int next = first == null ? -1 : -first.direction;

        Undo.SetCurrentGroupName("튜토리얼 슬라임 방향 바꾸기");
        foreach (EnemyAnimation a in slimes)
        {
            TutorialSlimeFacing f = a.GetComponent<TutorialSlimeFacing>();
            if (f == null) f = Undo.AddComponent<TutorialSlimeFacing>(a.gameObject);
            Undo.RecordObject(f, "튜토리얼 슬라임 방향 바꾸기");
            f.direction = next;
            EditorUtility.SetDirty(f);
            a.SetDirection(next);   // 씬 화면에도 바로 보이게
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[튜토리얼] 슬라임 {slimes.Length}마리가 {(next < 0 ? "왼쪽" : "오른쪽")}을 보게 했어요. Ctrl+S로 저장하세요.");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanToggle() => !EditorApplication.isPlaying;

    private static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform found = FindDeep(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
