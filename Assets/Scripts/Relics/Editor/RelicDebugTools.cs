#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 유물 인벤토리를 손으로 확인하기 위한 에디터 전용 도구.
///
/// 이 게임은 유물을 먹어야 보유 목록에 들어가므로, 인벤토리 UI만 확인하려 해도
/// 매번 픽업이 있는 곳까지 걸어가야 한다. Play 중에 메뉴 한 번으로 채운다.
///
/// #if UNITY_EDITOR 안에 있어서 빌드에는 들어가지 않는다.
/// </summary>
public static class RelicDebugTools
{
    [MenuItem("Tools/Relics/Debug - 모든 유물 보유 처리")]
    public static void GrantAllRelics()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Play 중에만 됩니다",
                "진행도(RunState)는 실행 중에만 존재합니다.\nPlay 버튼을 누른 뒤 다시 실행해주세요.", "확인");
            return;
        }

        List<RelicData> relics = LoadAll();

        if (relics.Count == 0)
        {
            EditorUtility.DisplayDialog("유물 없음", "프로젝트에서 RelicData를 찾지 못했습니다.", "확인");
            return;
        }

        foreach (RelicData relic in relics)
            RunState.Current.AddOwnedRelic(relic.RelicId);

        RefreshOpenPanel();

        Debug.Log($"[RelicDebugTools] 유물 {relics.Count}개를 보유 처리했습니다. 인벤토리(I)를 열어보세요.");
    }

    [MenuItem("Tools/Relics/Debug - 보유 유물 비우기")]
    public static void ClearRelics()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Play 중에만 됩니다",
                "진행도(RunState)는 실행 중에만 존재합니다.", "확인");
            return;
        }

        RelicManager manager = Object.FindAnyObjectByType<RelicManager>();

        if (manager != null)
        {
            // 장착 중인 것부터 벗겨야 RunState와 어긋나지 않는다.
            List<RelicData> equipped = new List<RelicData>(manager.EquippedRelics);

            foreach (RelicData relic in equipped)
                manager.UnequipRelic(relic);
        }

        RunState.Current.Clear();
        RefreshOpenPanel();

        Debug.Log("[RelicDebugTools] 진행도를 비웠습니다.");
    }

    private static List<RelicData> LoadAll()
    {
        List<RelicData> result = new List<RelicData>();
        string[] guids = AssetDatabase.FindAssets("t:RelicData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);

            if (relic == null) continue;
            if (string.IsNullOrEmpty(relic.RelicId))
            {
                Debug.LogWarning($"[RelicDebugTools] {relic.name} 의 RelicId가 비어 있어 건너뜁니다.", relic);
                continue;
            }

            result.Add(relic);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    /// <summary>열려 있는 인벤토리가 있으면 즉시 다시 그린다.</summary>
    private static void RefreshOpenPanel()
    {
        RelicInventoryPanel panel = Object.FindAnyObjectByType<RelicInventoryPanel>(FindObjectsInactive.Include);

        if (panel != null && panel.gameObject.activeInHierarchy) panel.Refresh();
    }
}
#endif
