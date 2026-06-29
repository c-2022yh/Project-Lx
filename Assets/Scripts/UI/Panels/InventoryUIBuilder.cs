#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인벤토리 UI를 자동으로 생성하는 에디터 도구.
/// Unity 상단 메뉴 Tools/UI → Build Inventory Panel 클릭으로 실행.
/// </summary>
public static class InventoryUIBuilder
{
    [MenuItem("Tools/UI/Build Inventory Panel")]
    public static void BuildInventoryPanel()
    {
        // Popup_Canvas 찾기
        GameObject popupCanvas = GameObject.Find("Popup_Canvas");
        if (popupCanvas == null)
        {
            EditorUtility.DisplayDialog(
                "Popup_Canvas 없음",
                "Hierarchy에 Popup_Canvas가 없습니다. 먼저 Popup_Canvas를 만들어주세요.",
                "확인");
            return;
        }

        // 이미 InventoryPanel이 있으면 경고
        Transform existing = popupCanvas.transform.Find("InventoryPanel");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "이미 존재함",
                "InventoryPanel이 이미 있습니다. 삭제하고 새로 만들까요?",
                "삭제하고 새로 만들기", "취소");
            if (!replace) return;
            Object.DestroyImmediate(existing.gameObject);
        }

        // === InventoryPanel 루트 ===
        GameObject inventoryPanel = CreateUIObject("InventoryPanel", popupCanvas.transform);
        SetStretch(inventoryPanel, 0, 0, 0, 0);

        // === 반투명 배경 ===
        GameObject bg = CreateImage("Background", inventoryPanel.transform, new Color(0, 0, 0, 200f / 255f));
        SetStretch(bg, 0, 0, 0, 0);

        // === 타이틀: 유물 ===
        GameObject titleRelic = CreateTMPText("Title_Relic", inventoryPanel.transform, "유물", 40, FontStyles.Bold);
        SetAnchorAndPos(titleRelic, AnchorType.TopLeft, 80, -60, 300, 50);

        // === 유물 그리드 영역 ===
        GameObject relicGrid = CreateUIObject("RelicGrid", inventoryPanel.transform);
        SetStretch(relicGrid, 80, 120, 1100, 400);
        var gridLayout = relicGrid.AddComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        gridLayout.cellSize = new Vector2(100, 100);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;

        // === 설명 영역 ===
        GameObject descPanel = CreateUIObject("DescriptionPanel", inventoryPanel.transform);
        SetStretch(descPanel, 1150, 120, 80, 400);

        // 큰 아이콘
        GameObject descIcon = CreateImage("Desc_Icon", descPanel.transform, Color.white);
        SetAnchorAndPos(descIcon, AnchorType.TopCenter, 0, -100, 150, 150);
        descIcon.GetComponent<Image>().enabled = false; // 초기엔 안 보이게

        // 유물 이름
        GameObject descName = CreateTMPText("Desc_Name", descPanel.transform, "유물 이름", 36, FontStyles.Bold);
        SetStretchTop(descName, 0, 260, 0, 60);
        descName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 유물 설명
        GameObject descText = CreateTMPText("Desc_Text", descPanel.transform, "여기에 유물 설명이 표시됩니다.", 22, FontStyles.Normal);
        SetStretchTop(descText, 20, 330, 20, 200);
        descText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

        // 장착 버튼 4개 (A/S/D/F)
        string[] slotKeys = { "A", "S", "D", "F" };
        for (int i = 0; i < 4; i++)
        {
            GameObject btn = CreateButton($"Button_Equip{slotKeys[i]}", descPanel.transform, $"{slotKeys[i]}에 장착");
            SetAnchorAndPos(btn, AnchorType.BottomLeft, i * 130, 100, 120, 40);
        }

        // 해제 버튼
        GameObject btnUnequip = CreateButton("Button_Unequip", descPanel.transform, "해제");
        SetAnchorAndPos(btnUnequip, AnchorType.BottomLeft, 0, 40, 200, 40);

        // === 장착 영역 타이틀 ===
        GameObject titleEquip = CreateTMPText("Title_Equip", inventoryPanel.transform, "장착 중", 28, FontStyles.Bold);
        SetStretchBottom(titleEquip, 0, 340, 0, 40);
        titleEquip.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // === 장착 슬롯 4개 ===
        GameObject equipContainer = CreateUIObject("EquipSlotContainer", inventoryPanel.transform);
        SetStretchBottom(equipContainer, 400, 120, 400, 200);
        var hLayout = equipContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 30;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        for (int i = 0; i < 4; i++)
        {
            GameObject equipSlot = CreateImage($"EquipSlot_{slotKeys[i]}", equipContainer.transform,
                new Color(40f / 255f, 40f / 255f, 50f / 255f, 200f / 255f));
            RectTransform rt = equipSlot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 120);

            // 아이콘 자리
            GameObject icon = CreateImage("Icon", equipSlot.transform, Color.white);
            SetStretch(icon, 15, 15, 15, 15);
            icon.GetComponent<Image>().enabled = false; // 초기엔 안 보이게

            // 라벨
            GameObject label = CreateTMPText("Label", equipSlot.transform, slotKeys[i], 24, FontStyles.Bold);
            SetAnchorAndPos(label, AnchorType.BottomCenter, 0, 15, 40, 30);
            label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        // === 시작 시엔 비활성 ===
        inventoryPanel.SetActive(false);

        // 선택해서 보여주기
        Selection.activeGameObject = inventoryPanel;
        EditorUtility.DisplayDialog(
            "완료!",
            "InventoryPanel 생성 완료!\nHierarchy에서 체크박스 켜서 확인하세요.",
            "확인");
    }

    // ─────────────────────────────────────────
    //  헬퍼 함수들
    // ─────────────────────────────────────────

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

        // 버튼 안에 텍스트
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
            case AnchorType.TopLeft: rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); break;
            case AnchorType.TopCenter: rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1); break;
            case AnchorType.TopRight: rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); break;
            case AnchorType.BottomLeft: rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0); break;
            case AnchorType.BottomCenter: rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = new Vector2(0.5f, 0); break;
            case AnchorType.BottomRight: rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); break;
            case AnchorType.Center: rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); break;
        }
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
#endif