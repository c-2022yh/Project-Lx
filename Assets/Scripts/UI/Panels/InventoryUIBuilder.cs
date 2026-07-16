#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 UI를 자동으로 생성하는 에디터 도구.
/// Unity 상단 메뉴 Tools/UI → Build Inventory Panel 클릭으로 실행.
///
/// [코스트제 버전]
/// - A/S/D/F 고정 슬롯 없음. 장착/해제 버튼 2개 + 코스트 예산 표시.
/// - 모든 TMP 텍스트에 한글 폰트(TerrarumSans)를 자동으로 지정.
/// - InventoryPanel 컴포넌트를 붙이고 인스펙터 참조까지 자동 연결.
/// - UIManager의 inventoryPanel 참조도 자동 재연결.
/// </summary>
public static class InventoryUIBuilder
{
    // 한글 폰트 에셋 경로
    private const string KoreanFontPath = "Assets/TextMesh Pro/Fonts/TerrarumSansBitmap SDF.asset";

    // 유물 칸 프리팹 경로 (없으면 자동 생성)
    private const string RelicSlotPrefabPath = "Assets/Prefabs/UI/RelicSlot.prefab";

    private static TMP_FontAsset koreanFont;

    private static TMP_FontAsset KoreanFont
    {
        get
        {
            if (koreanFont == null)
                koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            return koreanFont;
        }
    }

    [MenuItem("Tools/UI/Build Inventory Panel")]
    public static void BuildInventoryPanel()
    {
        // 한글 폰트가 없으면 만들어봐야 사각형만 나오니 미리 막음
        if (KoreanFont == null)
        {
            EditorUtility.DisplayDialog(
                "한글 폰트 없음",
                "폰트 에셋을 찾을 수 없습니다:\n" + KoreanFontPath + "\n\n" +
                "경로가 바뀌었다면 InventoryUIBuilder.cs의 KoreanFontPath를 수정해주세요.",
                "확인");
            return;
        }

        // Popup_Canvas 찾기
        GameObject popupCanvas = GameObject.Find("Popup_Canvas");
        if (popupCanvas == null)
        {
            EditorUtility.DisplayDialog(
                "Popup_Canvas 없음",
                "Hierarchy에 Popup_Canvas가 없습니다.\n씬에 UI_Root를 먼저 올려주세요.",
                "확인");
            return;
        }

        // 이미 InventoryPanel이 있으면 지우고 새로
        Transform existing = popupCanvas.transform.Find("InventoryPanel");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "이미 있음",
                "InventoryPanel이 이미 있습니다.\n지우고 코스트제 버전으로 새로 만들까요?\n\n" +
                "(인스펙터 참조는 빌더가 자동으로 다시 연결합니다)",
                "지우고 새로 만들기", "취소");
            if (!replace) return;
            Object.DestroyImmediate(existing.gameObject);
        }

        // InventoryPanel 루트
        GameObject inventoryPanel = CreateUIObject("InventoryPanel", popupCanvas.transform);
        SetStretch(inventoryPanel, 0, 0, 0, 0);

        // 반투명 배경
        GameObject bg = CreateImage("Background", inventoryPanel.transform, new Color(0f, 0f, 0f, 200f / 255f));
        SetStretch(bg, 0, 0, 0, 0);

        // 타이틀: 유물
        GameObject titleRelic = CreateTMPText("Title_Relic", inventoryPanel.transform, "유물", 40, FontStyles.Bold);
        SetAnchorAndPos(titleRelic, AnchorType.TopLeft, 80, -60, 300, 50);

        // 보유 유물 그리드
        GameObject relicGrid = CreateUIObject("RelicGrid", inventoryPanel.transform);
        SetStretch(relicGrid, 80, 120, 1100, 400);
        GridLayoutGroup gridLayout = relicGrid.AddComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.cellSize = new Vector2(100, 100);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;

        // 설명창
        GameObject descPanel = CreateUIObject("DescriptionPanel", inventoryPanel.transform);
        SetStretch(descPanel, 1150, 120, 80, 400);

        // 큰 아이콘
        GameObject descIcon = CreateImage("Desc_Icon", descPanel.transform, Color.white);
        SetAnchorAndPos(descIcon, AnchorType.TopCenter, 0, -100, 150, 150);
        descIcon.GetComponent<Image>().enabled = false;

        // 유물 이름
        GameObject descName = CreateTMPText("Desc_Name", descPanel.transform, "유물 이름", 36, FontStyles.Bold);
        SetStretchTop(descName, 0, 260, 0, 60);
        descName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 유물 설명
        GameObject descText = CreateTMPText("Desc_Text", descPanel.transform, "여기에 유물 설명이 표시됩니다.", 22, FontStyles.Normal);
        SetStretchTop(descText, 20, 330, 20, 200);
        descText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

        // 장착 / 해제 버튼 2개 (코스트제)
        GameObject btnEquip = CreateButton("Button_Equip", descPanel.transform, "장착");
        SetAnchorAndPos(btnEquip, AnchorType.BottomLeft, 0, 100, 120, 40);

        GameObject btnUnequip = CreateButton("Button_Unequip", descPanel.transform, "해제");
        SetAnchorAndPos(btnUnequip, AnchorType.BottomLeft, 140, 100, 120, 40);

        // 코스트 예산 표시
        GameObject budgetText = CreateTMPText("BudgetText", descPanel.transform, "코스트: 0 / 5", 28, FontStyles.Bold);
        SetAnchorAndPos(budgetText, AnchorType.BottomLeft, 0, 40, 300, 40);
        budgetText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // 장착 목록 타이틀
        GameObject titleEquip = CreateTMPText("Title_Equip", inventoryPanel.transform, "장착 중", 28, FontStyles.Bold);
        SetStretchBottom(titleEquip, 0, 340, 0, 40);
        titleEquip.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 장착 슬롯 컨테이너 (빈 채로 둠. 런타임에 InventoryPanel이 채움)
        GameObject equipContainer = CreateUIObject("EquipSlotContainer", inventoryPanel.transform);
        SetStretchBottom(equipContainer, 400, 120, 400, 200);
        HorizontalLayoutGroup hLayout = equipContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 30;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // InventoryPanel 컴포넌트 붙이고 참조 자동 연결
        InventoryPanel panel = inventoryPanel.AddComponent<InventoryPanel>();
        GameObject relicSlotPrefab = GetOrCreateRelicSlotPrefab();

        SerializedObject so = new SerializedObject(panel);
        SetRef(so, "relicGrid", relicGrid.transform);
        SetRef(so, "relicSlotPrefab", relicSlotPrefab);
        SetRef(so, "descIcon", descIcon.GetComponent<Image>());
        SetRef(so, "descName", descName.GetComponent<TextMeshProUGUI>());
        SetRef(so, "descText", descText.GetComponent<TextMeshProUGUI>());
        SetRef(so, "equipContainer", equipContainer.transform);
        SetRef(so, "budgetText", budgetText.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        // 버튼 onClick 자동 연결
        UnityEventTools.AddPersistentListener(btnEquip.GetComponent<Button>().onClick, panel.OnEquipButton);
        UnityEventTools.AddPersistentListener(btnUnequip.GetComponent<Button>().onClick, panel.OnUnequipButton);

        // UIManager의 inventoryPanel 참조 재연결
        string uiManagerNote = RelinkUIManager(panel);

        // 시작 시엔 비활성
        inventoryPanel.SetActive(false);

        EditorUtility.SetDirty(inventoryPanel);
        Selection.activeGameObject = inventoryPanel;

        EditorUtility.DisplayDialog(
            "완료!",
            "InventoryPanel(코스트제) 생성 완료!\n\n" +
            "- 한글 폰트 자동 지정됨\n" +
            "- 장착/해제 버튼 2개 + 코스트 예산 표시\n" +
            "- InventoryPanel 인스펙터 참조 자동 연결됨\n" +
            "- " + uiManagerNote + "\n\n" +
            "남은 수동 작업: InventoryPanel의 Owned Relics 목록에 RelicData를 넣어주세요.\n" +
            "그리고 Popup_Canvas 프리팹에 Apply 하는 것도 잊지 마세요!",
            "확인");
    }

    // 유물 칸(RelicSlot) 프리팹 - 없으면 만들어줌
    private static GameObject GetOrCreateRelicSlotPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RelicSlotPrefabPath);
        if (existing != null) return existing;

        GameObject slot = new GameObject("RelicSlot", typeof(RectTransform));
        Image slotBg = slot.AddComponent<Image>();
        slotBg.color = new Color(40f / 255f, 40f / 255f, 50f / 255f, 200f / 255f);
        Button slotBtn = slot.AddComponent<Button>();
        slotBtn.targetGraphic = slotBg;
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);

        // 아이콘 (InventoryPanel이 Find("Icon")으로 찾음)
        GameObject icon = CreateImage("Icon", slot.transform, Color.white);
        SetStretch(icon, 10, 10, 10, 10);
        icon.GetComponent<Image>().enabled = false;

        // 코스트 표시 (InventoryPanel이 Find("Cost")로 찾음)
        GameObject cost = CreateTMPText("Cost", slot.transform, "0", 20, FontStyles.Bold);
        SetAnchorAndPos(cost, AnchorType.BottomRight, -6, 6, 30, 26);
        cost.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(slot, RelicSlotPrefabPath);
        Object.DestroyImmediate(slot);
        Debug.Log("[InventoryUIBuilder] RelicSlot 프리팹 생성됨: " + RelicSlotPrefabPath);
        return saved;
    }

    // UIManager의 inventoryPanel 필드를 새 패널로 다시 연결
    private static string RelinkUIManager(InventoryPanel panel)
    {
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
            return "UIManager를 못 찾아서 재연결 못 했습니다 (직접 연결 필요)";

        SerializedObject so = new SerializedObject(uiManager);
        SerializedProperty prop = so.FindProperty("inventoryPanel");
        if (prop == null)
            return "UIManager에 inventoryPanel 필드가 없습니다";

        prop.objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);
        return "UIManager.inventoryPanel 자동 재연결됨";
    }

    // SerializedObject로 private [SerializeField] 필드에 값 넣기
    private static void SetRef(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning("[InventoryUIBuilder] InventoryPanel에 '" + fieldName + "' 필드가 없습니다. 직접 연결해주세요.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    // ---------------- 보조 함수들 ----------------

    private enum AnchorType { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Center }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize, FontStyles style)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

        // 한글 폰트 지정 (이게 없으면 기본 LiberationSans라 한글이 네모로 나옴)
        if (KoreanFont != null) tmp.font = KoreanFont;

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject go = CreateUIObject(name, parent);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(80f / 255f, 180f / 255f, 255f / 255f, 100f / 255f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textGO = CreateTMPText("Text", go.transform, text, 18, FontStyles.Normal);
        SetStretch(textGO, 0, 0, 0, 0);
        textGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static void SetStretch(GameObject go, float left, float top, float right, float bottom)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void SetStretchTop(GameObject go, float left, float topOffset, float right, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2((left - right) / 2f, -topOffset);
        rt.sizeDelta = new Vector2(-(left + right), height);
    }

    private static void SetStretchBottom(GameObject go, float left, float bottomOffset, float right, float height)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2((left - right) / 2f, bottomOffset);
        rt.sizeDelta = new Vector2(-(left + right), height);
    }

    private static void SetAnchorAndPos(GameObject go, AnchorType anchor, float x, float y, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        switch (anchor)
        {
            case AnchorType.TopLeft:
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); break;
            case AnchorType.TopCenter:
                rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1); break;
            case AnchorType.TopRight:
                rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); break;
            case AnchorType.BottomLeft:
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0); break;
            case AnchorType.BottomCenter:
                rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = new Vector2(0.5f, 0); break;
            case AnchorType.BottomRight:
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); break;
            case AnchorType.Center:
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); break;
        }
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
#endif
