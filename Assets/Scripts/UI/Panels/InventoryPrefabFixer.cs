#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 인벤토리의 인스펙터 연결을 "프리팹 에셋에 직접" 채워 넣는 에디터 도구.
///
/// 왜 필요한가:
///   기존 Tools/UI/Build Inventory Panel 은 GameObject.Find("Popup_Canvas") 로
///   "열려 있는 씬"의 오브젝트를 찾는다. 그래서 씬에서 실행하면 그 씬에만 반영되고,
///   프리팹에 Apply 하는 것을 잊으면 다른 씬에서는 참조가 비어 있는 채로 남는다.
///   실제로 그 상태 때문에 InventoryPanel.BuildRelicGrid()에서
///   NullReferenceException이 발생했다.
///
/// 이 도구는 씬을 전혀 건드리지 않고 Popup_Canvas.prefab / UIManager.prefab 두
/// 에셋만 직접 열어서 고치므로, 이 프리팹을 쓰는 모든 씬에 한 번에 반영된다.
/// </summary>
public static class InventoryPrefabFixer
{
    private const string PopupCanvasPath = "Assets/Prefabs/UI/Popup_Canvas.prefab";
    /// <summary>UIManager와 Popup_Canvas가 나란히 중첩되어 있는 프리팹.</summary>
    public const string UIRootPath = "Assets/Prefabs/UI/UI_Root.prefab";

    [MenuItem("Tools/UI/Fix Inventory Panel In Prefab")]
    public static void FixInPrefab()
    {
        if (!EditorUtility.DisplayDialog(
                "인벤토리 프리팹 수정",
                "아래 두 프리팹을 직접 수정합니다. 씬은 건드리지 않습니다.\n\n" +
                "1. " + PopupCanvasPath + "\n" +
                "   - InventoryPanel을 코스트제 버전으로 다시 만들고\n" +
                "   - 인스펙터 참조 7개를 전부 연결\n" +
                "   - Owned Relics에 프로젝트의 RelicData를 전부 넣음\n\n" +
                "2. " + UIRootPath + "\n" +
                "   - UIManager.inventoryPanel 을 위 패널로 연결\n\n" +
                "진행할까요?",
                "진행", "취소"))
        {
            return;
        }

        List<RelicData> relics = LoadAllRelicData();

        // ---------- 1단계: Popup_Canvas.prefab ----------
        GameObject popupRoot = PrefabUtility.LoadPrefabContents(PopupCanvasPath);
        if (popupRoot == null)
        {
            EditorUtility.DisplayDialog("실패", "프리팹을 열지 못했습니다:\n" + PopupCanvasPath, "확인");
            return;
        }

        int relicCount = 0;
        try
        {
            Transform existing = popupRoot.transform.Find("InventoryPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 씬의 UIManager를 잘못 건드리지 않도록 relinkUIManager는 false.
            InventoryPanel panel = InventoryUIBuilder.BuildInto(popupRoot.transform, false);

            relicCount = FillOwnedRelics(panel, relics);

            PrefabUtility.SaveAsPrefabAsset(popupRoot, PopupCanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(popupRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---------- 2단계: UIManager.prefab ----------
        string uiManagerNote = LinkPanelInUIRoot("inventoryPanel", typeof(InventoryPanel));

        EditorUtility.DisplayDialog(
            "완료!",
            "프리팹 수정 완료.\n\n" +
            "- InventoryPanel 인스펙터 참조 7개 연결됨\n" +
            "- Owned Relics: " + relicCount + "개\n" +
            "- " + uiManagerNote + "\n\n" +
            "이 프리팹을 쓰는 모든 씬에 자동으로 반영됩니다.\n" +
            "씬 파일은 하나도 바뀌지 않았습니다.",
            "확인");
    }

    /// <summary>
    /// 패널을 다시 만들지 않고 UI_Root의 연결만 고친다.
    /// 패널은 멀쩡한데 UIManager가 엉뚱한 것(프리팹 에셋)을 가리키고 있을 때 쓴다.
    /// </summary>
    [MenuItem("Tools/UI/Link Inventory Panel In UI_Root")]
    public static void LinkInventoryPanel()
    {
        string note = LinkPanelInUIRoot("inventoryPanel", typeof(InventoryPanel));

        EditorUtility.DisplayDialog("UI_Root 연결", note, "확인");
    }

    /// <summary>프로젝트 안의 RelicData를 전부 찾아서 이름순으로 돌려준다.</summary>
    private static List<RelicData> LoadAllRelicData()
    {
        List<RelicData> result = new List<RelicData>();
        string[] guids = AssetDatabase.FindAssets("t:RelicData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
            if (relic != null) result.Add(relic);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    /// <summary>ownedRelics 리스트를 채운다. 채운 개수를 돌려준다.</summary>
    private static int FillOwnedRelics(InventoryPanel panel, List<RelicData> relics)
    {
        SerializedObject so = new SerializedObject(panel);
        SerializedProperty list = so.FindProperty("ownedRelics");

        if (list == null)
        {
            Debug.LogWarning("[InventoryPrefabFixer] InventoryPanel에 ownedRelics 필드가 없습니다.");
            return 0;
        }

        list.ClearArray();
        for (int i = 0; i < relics.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = relics[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return relics.Count;
    }

    /// <summary>
    /// UI_Root.prefab 안에서 UIManager의 패널 참조를 "같은 UI_Root 안의 인스턴스"로 연결한다.
    ///
    /// UIManager.prefab에 직접 넣으면 안 되는 이유:
    ///   프리팹 에셋에 저장된 참조는 다른 프리팹 "에셋"을 가리킨다. 씬에 올라온 뒤에도
    ///   에셋을 가리킨 채로 남아서, 그 안의 컨테이너를 부모로 Instantiate하면
    ///   "Cannot instantiate objects with a parent which is persistent" 에러가 난다.
    ///   두 프리팹이 함께 중첩된 UI_Root에서 연결해야 씬 인스턴스로 이어진다.
    ///   (팀이 hudPanel/pausePanel/controlGuidePanel을 이미 이 방식으로 연결해 두었다.)
    /// </summary>
    /// <param name="fieldName">UIManager의 직렬화 필드 이름</param>
    /// <param name="panelType">그 필드에 넣을 패널 컴포넌트 타입</param>
    public static string LinkPanelInUIRoot(string fieldName, System.Type panelType)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UIRootPath);

        if (root == null)
            return "UI_Root.prefab을 열지 못했습니다";

        try
        {
            UIManager uiManager = root.GetComponentInChildren<UIManager>(true);

            if (uiManager == null)
                return "UI_Root 안에서 UIManager를 찾지 못했습니다";

            Component panel = root.GetComponentInChildren(panelType, true);

            if (panel == null)
                return "UI_Root 안에서 " + panelType.Name + "을(를) 찾지 못했습니다";

            SerializedObject so = new SerializedObject(uiManager);
            SerializedProperty prop = so.FindProperty(fieldName);

            if (prop == null)
                return "UIManager에 " + fieldName + " 필드가 없습니다 (컴파일 후 다시 실행)";

            prop.objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, UIRootPath);

            return "UI_Root에서 UIManager." + fieldName + " 연결됨";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
