#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using K = UIBuildKit;

/// <summary>
/// 유물 인벤토리 화면을 만든다.
/// Unity 상단 메뉴 Tools/UI > Build Relic Inventory.
///
/// 씬의 Popup_Canvas 아래에 만들고, UIManager의 참조까지 자동으로 연결한다.
/// 여러 번 실행해도 안전하다 (기존 것을 지우고 새로 만든다).
/// </summary>
public static class RelicInventoryUIBuilder
{
    private const string SlotPrefabPath = "Assets/Prefabs/UI/RelicSlotView.prefab";
    private const string PopupCanvasPath = "Assets/Prefabs/UI/Popup_Canvas.prefab";

    /// <summary>BuildInto()가 마지막으로 남긴 UIManager 연결 결과 메시지.</summary>
    public static string LastUIManagerNote { get; private set; } = "";

    private const float SlotSize = 88f;
    private const float SlotGap = 12f;
    private const float RowLabelWidth = 100f;

    /// <summary>보관함 격자 열 수. RelicInventoryPanel.StorageColumns와 같아야 한다.</summary>
    private const int StorageColumns = 6;

    private const float TooltipWidth = 400f;

    [MenuItem("Tools/UI/Build Relic Inventory")]
    public static void Build()
    {
        if (!K.EnsureKoreanFont()) return;

        GameObject popupCanvas = GameObject.Find("Popup_Canvas");

        if (popupCanvas == null)
        {
            EditorUtility.DisplayDialog("Popup_Canvas 없음",
                "Hierarchy에 Popup_Canvas가 없습니다.\n씬에 UI_Root를 먼저 올려주세요.", "확인");
            return;
        }

        Transform existing = popupCanvas.transform.Find("RelicInventoryPanel");

        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("이미 있음",
                "RelicInventoryPanel이 이미 있습니다.\n지우고 새로 만들까요?", "새로 만들기", "취소");

            if (!replace) return;

            // Popup_Canvas 프리팹에 Apply한 뒤라면 인스턴스 안의 오브젝트라 지울 수 없다.
            // 그냥 진행하면 DestroyImmediate가 실패하고 패널이 두 개가 된다.
            if (PrefabUtility.IsPartOfPrefabInstance(existing.gameObject) &&
                !PrefabUtility.IsAddedGameObjectOverride(existing.gameObject))
            {
                EditorUtility.DisplayDialog("프리팹 안에 있음",
                    "기존 RelicInventoryPanel이 Popup_Canvas 프리팹의 일부입니다.\n" +
                    "Prefab Mode에서 먼저 지운 뒤 다시 실행해주세요.", "확인");
                return;
            }

            Object.DestroyImmediate(existing.gameObject);
        }

        RelicInventoryPanel built = BuildInto(popupCanvas.transform, true);
        Selection.activeGameObject = built.gameObject;

        EditorUtility.DisplayDialog("완료!",
            "RelicInventoryPanel 생성 완료\n\n" + Summary(LastUIManagerNote) +
            "\n씬에 만들었으므로 Popup_Canvas 프리팹에 Apply 해야 다른 씬에도 반영됩니다.\n" +
            "(Tools/UI > Build Relic Inventory In Prefab 을 쓰면 Apply가 필요 없습니다)",
            "확인");
    }

    /// <summary>
    /// 씬을 열지 않고 Popup_Canvas 프리팹 자체에 만든다.
    ///
    /// 위의 메뉴는 GameObject.Find로 "열려 있는 씬"을 고치기 때문에,
    /// 프리팹에 Apply 하는 것을 잊으면 그 씬에서만 패널이 생긴다.
    /// 이쪽은 프리팹 에셋을 직접 고치므로 이 프리팹을 쓰는 모든 씬에 한 번에 반영된다.
    /// </summary>
    [MenuItem("Tools/UI/Build Relic Inventory In Prefab")]
    public static void BuildInPrefab()
    {
        if (!K.EnsureKoreanFont()) return;

        if (!EditorUtility.DisplayDialog("유물 인벤토리 (프리팹에 직접)",
                "아래 두 프리팹을 직접 고칩니다. 씬은 건드리지 않습니다.\n\n" +
                "1. " + PopupCanvasPath + "\n   RelicInventoryPanel을 새로 만들고 참조 연결\n\n" +
                "2. " + InventoryPrefabFixer.UIRootPath + "\n   UIManager.relicInventoryPanel 연결\n\n" +
                "진행할까요?",
                "진행", "취소"))
        {
            return;
        }

        GameObject popupRoot = PrefabUtility.LoadPrefabContents(PopupCanvasPath);

        if (popupRoot == null)
        {
            EditorUtility.DisplayDialog("실패", "프리팹을 열지 못했습니다:\n" + PopupCanvasPath, "확인");
            return;
        }

        try
        {
            Transform existing = popupRoot.transform.Find("RelicInventoryPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 프리팹 편집 중에는 씬의 UIManager를 잘못 건드리게 되므로 false.
            BuildInto(popupRoot.transform, false);

            PrefabUtility.SaveAsPrefabAsset(popupRoot, PopupCanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(popupRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string note = InventoryPrefabFixer.LinkPanelInUIRoot(
            "relicInventoryPanel", typeof(RelicInventoryPanel));

        EditorUtility.DisplayDialog("완료!",
            "RelicInventoryPanel 생성 완료 (프리팹)\n\n" + Summary(note) +
            "\n이 프리팹을 쓰는 모든 씬에 자동으로 반영됩니다. 씬 파일은 바뀌지 않았습니다.",
            "확인");
    }

    private static string Summary(string uiNote)
    {
        return "- 검 1칸 / 보주 1칸 / 신체는 코스트 5 예산\n" +
               "- 보관함은 가지고 있지만 장착하지 않은 유물\n" +
               "- 조작: 올리면 툴팁 / 좌클릭 선택 / 우클릭 장착·해제\n" +
               "        드래그로 칸 옮기기, 보관함으로 빼면 해제\n" +
               "        화살표로 이동, Z로 집고 놓기, X로 취소\n" +
               "- " + uiNote + "\n\n" +
               "Tools/Relics > Rebuild Relic Database 를 아직 안 하셨다면 먼저 해주세요.\n";
    }

    /// <summary>패널을 실제로 만드는 본체. 씬에서도 프리팹 에셋에서도 호출할 수 있다.</summary>
    public static RelicInventoryPanel BuildInto(Transform popupCanvas, bool relinkUIManager)
    {
        // ── 루트 ───────────────────────────
        GameObject panel = K.Obj("RelicInventoryPanel", popupCanvas);
        K.Stretch(panel, 0, 0, 0, 0);

        GameObject dim = K.Img("Dim", panel.transform, new Color(0f, 0f, 0f, 0.78f), true);
        K.Stretch(dim, 0, 0, 0, 0);

        GameObject window = K.Img("Window", panel.transform, K.PanelBg, true);
        K.Place(window, K.Anchor.Center, 0, 0, 1520, 840);
        Transform w = window.transform;

        GameObject title = K.Text("Title", w, "유물", 40, FontStyles.Bold);
        K.Place(title, K.Anchor.TopLeft, 40, -28, 400, 54);

        // ── 왼쪽: 장착 ──────────────────────
        GameObject equipHeader = K.Text("Header_Equip", w, "장착", 28, FontStyles.Bold);
        K.Place(equipHeader, K.Anchor.TopLeft, 40, -100, 300, 40);

        GameObject equipLine = K.Img("Divider_Equip", w, K.Divider);
        K.Place(equipLine, K.Anchor.TopLeft, 40, -142, 700, 2);

        Transform swordSlots = EquipRow(w, "검", -160, RelicDropTargetKind.SwordRow);
        Transform orbSlots = EquipRow(w, "보주", -160 - (SlotSize + 18), RelicDropTargetKind.OrbRow);
        Transform bodySlots = EquipRow(w, "신체", -160 - (SlotSize + 18) * 2, RelicDropTargetKind.BodyRow);

        GameObject costText = K.Text("BodyCostText", w, "코스트  0 / 5", 24,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        K.Place(costText, K.Anchor.TopLeft, 40 + RowLabelWidth, -160 - (SlotSize + 18) * 3 - 6, 400, 36);

        // ── 가운데 세로 구분선 ───────────────
        GameObject vDivider = K.Img("Divider_Vertical", w, K.Divider);
        K.Place(vDivider, K.Anchor.TopLeft, 782, -100, 2, 430);

        // ── 오른쪽: 보관함 ──────────────────
        GameObject storageHeader = K.Text("Header_Storage", w, "보관함", 28, FontStyles.Bold);
        K.Place(storageHeader, K.Anchor.TopLeft, 824, -100, 300, 40);

        GameObject storageLine = K.Img("Divider_Storage", w, K.Divider);
        K.Place(storageLine, K.Anchor.TopLeft, 824, -142, 656, 2);

        GameObject storage = K.Obj("StorageContainer", w);
        K.Place(storage, K.Anchor.TopLeft, 824, -160, 656, 370);

        GridLayoutGroup grid = storage.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(SlotSize, SlotSize);
        grid.spacing = new Vector2(SlotGap, SlotGap);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = StorageColumns;

        // 장착된 유물을 여기로 끌어다 놓으면 해제된다.
        AddDropZone(storage, RelicDropTargetKind.Storage);

        // ── 아래: 설명창 ────────────────────
        GameObject desc = K.Img("DescriptionPanel", w, K.SectionBg, true);
        K.StretchBottom(desc, 40, 36, 40, 250);
        Transform d = desc.transform;

        GameObject descIcon = K.Img("Desc_Icon", d, Color.white);
        K.Place(descIcon, K.Anchor.MiddleLeft, 28, 20, 110, 110);
        descIcon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject descName = K.Text("Desc_Name", d, "유물을 선택하세요", 30, FontStyles.Bold);
        K.Place(descName, K.Anchor.TopLeft, 164, -22, 700, 40);

        GameObject descMeta = K.Text("Desc_Meta", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
        K.Place(descMeta, K.Anchor.TopLeft, 164, -66, 700, 30);

        GameObject descText = K.Text("Desc_Text", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.TopLeft, K.MutedText);
        K.Place(descText, K.Anchor.TopLeft, 164, -102, 950, 100);

        GameObject equipBtn = K.Button("Button_Equip", d, "장착", 26);
        K.Place(equipBtn, K.Anchor.MiddleRight, -32, 18, 200, 62);

        GameObject hint = K.Text("HintText", d, "", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(hint, K.Anchor.BottomRight, -32, 16, 640, 30);

        // ── 툴팁 / 드래그 고스트 ────────────
        RelicTooltip tooltip = BuildTooltip(panel.transform);
        RelicDragLayer dragLayer = BuildDragLayer(panel.transform);

        // ── 컴포넌트 + 참조 연결 ────────────
        RelicInventoryPanel comp = panel.AddComponent<RelicInventoryPanel>();
        GameObject slotPrefab = BuildSlotPrefab();

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "swordSlotContainer", swordSlots);
        K.SetRef(so, "orbSlotContainer", orbSlots);
        K.SetRef(so, "bodySlotContainer", bodySlots);
        K.SetRef(so, "bodyCostText", costText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "storageContainer", storage.transform);
        K.SetRef(so, "slotPrefab", slotPrefab);
        K.SetRef(so, "descIcon", descIcon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "descName", descName.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descMeta", descMeta.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descText", descText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "equipButton", equipBtn.GetComponent<UnityEngine.UI.Button>());
        K.SetRef(so, "equipButtonLabel", equipBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "hintText", hint.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "tooltip", tooltip);
        K.SetRef(so, "dragLayer", dragLayer);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(
            equipBtn.GetComponent<UnityEngine.UI.Button>().onClick, comp.OnEquipButton);

        LastUIManagerNote = relinkUIManager
            ? RelinkUIManager(comp)
            : "프리팹 편집 중이라 씬 UIManager 재연결은 건너뜀";

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);

        return comp;
    }

    /// <summary>
    /// 마우스를 올렸을 때 뜨는 상세 툴팁. 커서를 따라다닌다.
    /// 가로 폭은 고정하고 세로만 내용에 맞춰 늘어나게 한다
    /// (가로까지 자동이면 설명이 긴 유물에서 화면을 가로지른다).
    /// </summary>
    private static RelicTooltip BuildTooltip(Transform panel)
    {
        GameObject root = K.Obj("RelicTooltip", panel);
        K.Stretch(root, 0, 0, 0, 0);

        RelicTooltip comp = root.AddComponent<RelicTooltip>();

        GameObject tip = K.Img("Tip", root.transform, new Color(0.04f, 0.04f, 0.06f, 0.94f));
        RectTransform tipRect = tip.GetComponent<RectTransform>();

        // 위치는 RelicTooltip이 캔버스 좌표로 직접 넣는다.
        // 그래서 앵커는 가운데 한 점, 피벗은 좌상단(커서 오른쪽 아래로 펼쳐짐).
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.pivot = new Vector2(0f, 1f);
        tipRect.sizeDelta = new Vector2(TooltipWidth, 160f);

        VerticalLayoutGroup vertical = tip.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(18, 18, 16, 16);
        vertical.spacing = 8;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter fitter = tip.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 머리: 아이콘 + (이름 / 계열)
        GameObject header = K.Obj("Header", tip.transform);

        HorizontalLayoutGroup horizontal = header.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 12;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        K.FixedHeight(header, 64f);

        GameObject icon = K.Img("Icon", header.transform, Color.white);
        LayoutElement iconSize = icon.AddComponent<LayoutElement>();
        iconSize.preferredWidth = 64f;
        iconSize.preferredHeight = 64f;
        icon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject names = K.Obj("Names", header.transform);

        VerticalLayoutGroup nameColumn = names.AddComponent<VerticalLayoutGroup>();
        nameColumn.spacing = 2;
        nameColumn.childControlWidth = true;
        nameColumn.childControlHeight = true;
        nameColumn.childForceExpandWidth = true;
        nameColumn.childForceExpandHeight = false;

        LayoutElement nameWidth = names.AddComponent<LayoutElement>();
        nameWidth.flexibleWidth = 1f;

        GameObject titleText = K.Text("Title", names.transform, "", 26, FontStyles.Bold);
        K.FixedHeight(titleText, 34f);

        GameObject subtitleText = K.Text("Subtitle", names.transform, "", 18,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
        K.FixedHeight(subtitleText, 24f);

        GameObject line = K.Img("Divider", tip.transform, K.Divider);
        K.FixedHeight(line, 1f);

        GameObject bodyText = K.Text("Body", tip.transform, "", 19,
            FontStyles.Normal, TextAlignmentOptions.TopLeft, K.MutedText);

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "panel", tipRect);
        K.SetRef(so, "titleText", titleText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "subtitleText", subtitleText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "iconImage", icon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "bodyText", bodyText.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        tip.SetActive(false);
        return comp;
    }

    /// <summary>
    /// 드래그하는 동안 커서를 따라다니는 아이콘 한 장.
    /// 레이캐스트를 받으면 드롭 대상을 가로채므로 raycastTarget은 꺼둔다.
    /// </summary>
    private static RelicDragLayer BuildDragLayer(Transform panel)
    {
        GameObject root = K.Obj("RelicDragLayer", panel);
        K.Stretch(root, 0, 0, 0, 0);

        RelicDragLayer comp = root.AddComponent<RelicDragLayer>();

        GameObject ghost = K.Img("Ghost", root.transform, new Color(1f, 1f, 1f, 0.85f));
        RectTransform ghostRect = ghost.GetComponent<RectTransform>();

        ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.sizeDelta = new Vector2(SlotSize - 12f, SlotSize - 12f);

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "ghost", ghostRect);
        K.SetRef(so, "ghostImage", ghost.GetComponent<UnityEngine.UI.Image>());
        so.ApplyModifiedPropertiesWithoutUndo();

        ghost.SetActive(false);
        return comp;
    }

    /// <summary>라벨 + 슬롯이 가로로 놓이는 한 줄. 슬롯을 담을 Transform을 돌려준다.</summary>
    private static Transform EquipRow(Transform window, string label, float y, RelicDropTargetKind kind)
    {
        GameObject labelGO = K.Text("Label_" + label, window, label, 24,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        K.Place(labelGO, K.Anchor.TopLeft, 40, y - (SlotSize - 36) / 2f, RowLabelWidth, 36);

        GameObject container = K.Obj("Slots_" + label, window);
        K.Place(container, K.Anchor.TopLeft, 40 + RowLabelWidth, y, 620, SlotSize);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = SlotGap;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 칸과 칸 사이 빈 곳에 놓아도 장착되도록.
        AddDropZone(container, kind);

        return container.transform;
    }

    /// <summary>
    /// 컨테이너를 드롭 받는 판으로 만든다.
    /// 레이캐스트를 받으려면 Graphic이 있어야 해서 투명 Image를 같이 붙인다
    /// (알파 0이어도 uGUI는 사각형 안이면 레이캐스트를 받는다).
    /// </summary>
    private static void AddDropZone(GameObject go, RelicDropTargetKind kind)
    {
        UnityEngine.UI.Image hit = go.GetComponent<UnityEngine.UI.Image>();
        if (hit == null) hit = go.AddComponent<UnityEngine.UI.Image>();

        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        RelicDropZone zone = go.AddComponent<RelicDropZone>();

        SerializedObject so = new SerializedObject(zone);
        SerializedProperty prop = so.FindProperty("kind");

        if (prop != null)
        {
            prop.enumValueIndex = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>
    /// 유물 한 칸 프리팹. 실행할 때마다 새로 만든다.
    /// 칸의 구조가 바뀌었는데 예전 프리팹이 그대로 남아 조용히 어긋나는 걸 막기 위해서다.
    /// </summary>
    private static GameObject BuildSlotPrefab()
    {
        GameObject slot = new GameObject("RelicSlotView", typeof(RectTransform));
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(SlotSize, SlotSize);

        UnityEngine.UI.Image bg = slot.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.20f, 0.20f, 0.24f, 1f);

        // 마우스 이벤트는 이 Image가 받아서 RelicSlotView로 전달된다.
        // Button을 쓰면 좌클릭만 들어오고 hover 색까지 덧칠해버려서 쓰지 않는다.
        bg.raycastTarget = true;

        // 키보드 포커스 테두리 (하양). 선택 테두리보다 크고 뒤에 깔린다.
        GameObject focus = K.Img("FocusOutline", slot.transform, new Color(1f, 1f, 1f, 0.75f));
        K.Stretch(focus, -6, -6, -6, -6);
        focus.GetComponent<UnityEngine.UI.Image>().enabled = false;

        // 선택 테두리 (노랑, 기본은 꺼둔다)
        GameObject outline = K.Img("SelectionOutline", slot.transform, new Color(1f, 0.85f, 0.35f, 0.55f));
        K.Stretch(outline, -3, -3, -3, -3);
        outline.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject icon = K.Img("Icon", slot.transform, Color.white);
        K.Stretch(icon, 10, 10, 10, 10);
        icon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject cost = K.Text("Cost", slot.transform, "0", 18,
            FontStyles.Bold, TextAlignmentOptions.BottomRight);
        K.Place(cost, K.Anchor.BottomRight, -6, 4, 28, 24);

        RelicSlotView view = slot.AddComponent<RelicSlotView>();

        SerializedObject so = new SerializedObject(view);
        K.SetRef(so, "background", bg);
        K.SetRef(so, "icon", icon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "costText", cost.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "selectionOutline", outline.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "focusOutline", focus.GetComponent<UnityEngine.UI.Image>());
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(slot, SlotPrefabPath);
        Object.DestroyImmediate(slot);

        Debug.Log("[RelicInventoryUIBuilder] RelicSlotView 프리팹 생성됨: " + SlotPrefabPath);
        return saved;
    }

    /// <summary>UIManager가 I키로 이 패널을 열도록 연결한다.</summary>
    private static string RelinkUIManager(RelicInventoryPanel panel)
    {
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();

        if (uiManager == null)
            return "UIManager를 못 찾아 자동 연결하지 못했습니다 (직접 연결 필요)";

        SerializedObject so = new SerializedObject(uiManager);
        SerializedProperty prop = so.FindProperty("relicInventoryPanel");

        if (prop == null)
            return "UIManager에 relicInventoryPanel 필드가 없습니다 (스크립트 컴파일 후 다시 실행해주세요)";

        prop.objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(uiManager);

        return "UIManager에 자동 연결됨 (I키로 열림)";
    }
}
#endif
