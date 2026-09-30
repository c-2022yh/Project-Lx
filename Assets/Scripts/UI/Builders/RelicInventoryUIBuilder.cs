#if UNITY_EDITOR && LUDENS_UI_TOOLS
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

    /// <summary>보관함 격자 열 수. RelicInventoryPanel.StorageColumns와 같아야 한다.</summary>
    private const int StorageColumns = 6;

    private const float TooltipWidth = 400f;

    private const string CircleSlotPrefabPath = "Assets/Prefabs/UI/RelicSlotCircle.prefab";
    private const string FilterIconFolder = "Assets/Sprites/UI/RelicFilter";

    // 창을 좌우로 나눈다. 왼쪽은 장착, 오른쪽은 보관함.
    private const float WindowWidth = 1520f;
    private const float WindowHeight = 840f;

    private const float LeftWidth = 700f;
    private const float LeftCenterX = 40f + LeftWidth / 2f;
    private const float RightX = 824f;
    private const float RightWidth = 656f;

    // 장착칸은 원형이고, 중요한 것일수록 크다.
    // 검과 보주는 일직선으로 쌓지 않고 좌우로 엇갈리게 놓는다 (B안).
    // 일러스트가 들어올 자리라 가운데를 비워두는 배치다.
    private const float DiagonalOffset = 72f;

    private const float SwordSlotSize = 130f;
    private const float OrbSlotSize = 105f;
    private const float BodySlotSize = 66f;

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
        return "- 배치는 와이어프레임 B안 (왼쪽 원형 장착칸 / 오른쪽 보관함)\n" +
               "- 검 1칸 / 보주 1칸 / 신체는 코스트 5, 개수 5\n" +
               "- 보관함은 가지고 있지만 장착하지 않은 유물\n" +
               "- 보관함 위 필터로 계열별 보기 / 정렬 버튼으로 이름순·획득순 전환\n" +
               "- 조작: 올리면 툴팁 / 좌클릭 선택 / 우클릭 장착·해제\n" +
               "        드래그로 칸 옮기기, 보관함으로 빼면 해제\n" +
               "        화살표로 이동, Z로 집고 놓기, X로 취소\n" +
               "- " + uiNote + "\n\n" +
               "Tools/Relics > Rebuild Relic Database 를 아직 안 하셨다면 먼저 해주세요.\n";
    }

    /// <summary>패널을 실제로 만드는 본체. 씬에서도 프리팹 에셋에서도 호출할 수 있다.</summary>
    /// <summary>
    /// 패널을 실제로 만드는 본체. 씬에서도 프리팹 에셋에서도 호출할 수 있다.
    ///
    /// 배치는 팀 와이어프레임 B안을 따른다.
    ///   왼쪽  검(큰 원) / 보주(원) / 신체(작은 원 가로줄)
    ///   오른쪽 필터 줄 + 보관함 격자
    ///   아래  설명창 (B안 그림에는 없지만 유지하기로 함)
    /// 배경 일러스트는 쓰지 않는다.
    /// </summary>
    public static RelicInventoryPanel BuildInto(Transform popupCanvas, bool relinkUIManager)
    {
        // ── 루트 ───────────────────────────
        GameObject panel = K.Obj("RelicInventoryPanel", popupCanvas);
        K.Stretch(panel, 0, 0, 0, 0);

        GameObject dim = K.Img("Dim", panel.transform, new Color(0f, 0f, 0f, 0.78f), true);
        K.Stretch(dim, 0, 0, 0, 0);

        // 창 테두리. 배경이 게임 화면과 비슷한 어두운 색이라
        // 테두리가 없으면 창이 어디서 시작하는지 안 보인다.
        GameObject frame = K.Img("WindowFrame", panel.transform, new Color(0.45f, 0.45f, 0.55f, 0.65f));
        K.Place(frame, K.Anchor.Center, 0, 0, WindowWidth + 4f, WindowHeight + 4f);

        GameObject window = K.Img("Window", panel.transform, new Color(0.07f, 0.07f, 0.09f, 1f), true);
        K.Place(window, K.Anchor.Center, 0, 0, WindowWidth, WindowHeight);
        Transform w = window.transform;

        // 제목 대신 탭 줄. 스킬창과 같은 자리에 같은 모양으로 그려야
        // Q/E로 오갈 때 위쪽이 튀지 않는다.
        K.TabBar(w, UITabBar.RelicTab);

        GameObject closeHint = K.Text("CloseHint", w, "Esc  닫기", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(closeHint, K.Anchor.TopRight, -40, -36, 300, 34);

        // ── 왼쪽: 장착 ──────────────────────
        GameObject equipHeader = K.Text("Header_Equip", w, "장착 중인 유물", 28, FontStyles.Bold);
        K.Place(equipHeader, K.Anchor.TopLeft, 40, -100, 400, 40);

        GameObject equipLine = K.Img("Divider_Equip", w, K.Divider);
        K.Place(equipLine, K.Anchor.TopLeft, 40, -142, LeftWidth, 2);

        // 검: 가장 크게, 왼쪽 위로
        Transform swordSlots = EquipSpot(w, "검", LeftCenterX - DiagonalOffset, -185, SwordSlotSize,
                                         RelicDropTargetKind.SwordRow);

        // 보주: 검의 오른쪽 아래로 엇갈리게
        Transform orbSlots = EquipSpot(w, "보주", LeftCenterX + DiagonalOffset, -352, OrbSlotSize,
                                       RelicDropTargetKind.OrbRow);

        // 신체: 아래쪽 가로줄. 개수가 늘 바뀌므로 가로 레이아웃에 맡긴다.
        GameObject bodyLabel = K.Text("Label_신체", w, "신체", 22,
            FontStyles.Normal, TextAlignmentOptions.Center, K.MutedText);
        K.Place(bodyLabel, K.Anchor.TopLeft, LeftCenterX - 100, -470, 200, 26);

        GameObject bodyRow = K.Obj("Slots_신체", w);
        K.Place(bodyRow, K.Anchor.TopLeft, LeftCenterX - LeftWidth / 2f, -500, LeftWidth, BodySlotSize);

        HorizontalLayoutGroup bodyLayout = bodyRow.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 14f;
        bodyLayout.childAlignment = TextAnchor.MiddleCenter;
        bodyLayout.childControlWidth = false;
        bodyLayout.childControlHeight = false;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = false;

        AddDropZone(bodyRow, RelicDropTargetKind.BodyRow);

        GameObject costText = K.Text("BodyCostText", w, "코스트  0 / 5", 22,
            FontStyles.Bold, TextAlignmentOptions.Center);
        K.Place(costText, K.Anchor.TopLeft, LeftCenterX - 150, -578, 300, 30);

        // ── 가운데 세로 구분선 ───────────────
        GameObject vDivider = K.Img("Divider_Vertical", w, K.Divider);
        K.Place(vDivider, K.Anchor.TopLeft, RightX - 42, -100, 2, 520);

        // ── 오른쪽: 보관함 ──────────────────
        GameObject storageHeader = K.Text("Header_Storage", w, "보관함", 28, FontStyles.Bold);
        K.Place(storageHeader, K.Anchor.TopLeft, RightX, -100, 220, 40);

        RelicFilterBar filterBar = BuildFilterBar(w);

        GameObject storageLine = K.Img("Divider_Storage", w, K.Divider);
        K.Place(storageLine, K.Anchor.TopLeft, RightX, -142, RightWidth, 2);

        GameObject storage = K.Obj("StorageContainer", w);
        K.Place(storage, K.Anchor.TopLeft, RightX, -160, RightWidth, 440);

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
        K.StretchBottom(desc, 40, 30, 40, 170);
        Transform d = desc.transform;

        GameObject descIcon = K.Img("Desc_Icon", d, Color.white);
        K.Place(descIcon, K.Anchor.MiddleLeft, 28, 8, 96, 96);
        descIcon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject descName = K.Text("Desc_Name", d, "유물을 선택하세요", 30, FontStyles.Bold);
        K.Place(descName, K.Anchor.TopLeft, 148, -18, 700, 40);

        GameObject descMeta = K.Text("Desc_Meta", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
        K.Place(descMeta, K.Anchor.TopLeft, 148, -58, 700, 30);

        GameObject descText = K.Text("Desc_Text", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.TopLeft, K.MutedText);
        K.Place(descText, K.Anchor.TopLeft, 148, -92, 950, 60);

        GameObject equipBtn = K.Button("Button_Equip", d, "장착", 26);
        K.Place(equipBtn, K.Anchor.MiddleRight, -32, 10, 200, 58);

        GameObject hint = K.Text("HintText", d, "", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(hint, K.Anchor.BottomRight, -32, 12, 640, 28);

        // ── 툴팁 / 드래그 고스트 ────────────
        RelicTooltip tooltip = BuildTooltip(panel.transform);
        RelicDragLayer dragLayer = BuildDragLayer(panel.transform);

        // ── 컴포넌트 + 참조 연결 ────────────
        RelicInventoryPanel comp = panel.AddComponent<RelicInventoryPanel>();
        GameObject slotPrefab = BuildSlotPrefab(false, SlotSize, SlotPrefabPath);
        GameObject circleSlotPrefab = BuildSlotPrefab(true, OrbSlotSize, CircleSlotPrefabPath);

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "swordSlotContainer", swordSlots);
        K.SetRef(so, "orbSlotContainer", orbSlots);
        K.SetRef(so, "bodySlotContainer", bodyRow.transform);
        K.SetRef(so, "bodyCostText", costText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "storageContainer", storage.transform);
        K.SetRef(so, "slotPrefab", slotPrefab);
        K.SetRef(so, "circleSlotPrefab", circleSlotPrefab);
        K.SetRef(so, "descIcon", descIcon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "descName", descName.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descMeta", descMeta.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descText", descText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "equipButton", equipBtn.GetComponent<UnityEngine.UI.Button>());
        K.SetRef(so, "equipButtonLabel", equipBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "hintText", hint.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "tooltip", tooltip);
        K.SetRef(so, "dragLayer", dragLayer);
        K.SetRef(so, "filterAllButton", filterBar.All);
        K.SetRef(so, "filterSwordButton", filterBar.Sword);
        K.SetRef(so, "filterOrbButton", filterBar.Orb);
        K.SetRef(so, "filterBodyButton", filterBar.Body);
        K.SetRef(so, "sortButton", filterBar.Sort);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(
            equipBtn.GetComponent<UnityEngine.UI.Button>().onClick, comp.OnEquipButton);

        UnityEventTools.AddPersistentListener(filterBar.All.onClick, comp.OnFilterAll);
        UnityEventTools.AddPersistentListener(filterBar.Sword.onClick, comp.OnFilterSword);
        UnityEventTools.AddPersistentListener(filterBar.Orb.onClick, comp.OnFilterOrb);
        UnityEventTools.AddPersistentListener(filterBar.Body.onClick, comp.OnFilterBody);
        UnityEventTools.AddPersistentListener(filterBar.Sort.onClick, comp.OnToggleSort);

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

    /// <summary>
    /// 칸 하나짜리 장착 자리(검 / 보주). 라벨을 위에 얹고 가운데에 칸을 놓는다.
    /// 칸이 하나뿐이라 가로 레이아웃으로 가운데 정렬만 해두면 된다.
    /// </summary>
    private static Transform EquipSpot(Transform window, string label, float centerX, float y,
                                       float size, RelicDropTargetKind kind)
    {
        GameObject labelGO = K.Text("Label_" + label, window, label, 22,
            FontStyles.Normal, TextAlignmentOptions.Center, K.MutedText);
        K.Place(labelGO, K.Anchor.TopLeft, centerX - 100, y + 28, 200, 26);

        GameObject container = K.Obj("Slots_" + label, window);
        K.Place(container, K.Anchor.TopLeft, centerX - size / 2f, y, size, size);

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

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
    ///
    /// circular가 켜지면 장착칸용 원형 칸이 된다. 유니티 기본 Knob 스프라이트가
    /// 동그라미라서 아트 없이도 원형을 만들 수 있다.
    /// </summary>
    private static GameObject BuildSlotPrefab(bool circular, float size, string path)
    {
        Sprite round = circular ? K.Builtin("UI/Skin/Knob.psd") : null;

        GameObject slot = new GameObject(circular ? "RelicSlotCircle" : "RelicSlotView",
                                         typeof(RectTransform));
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

        // 루트 Image는 보이지 않지만 마우스 이벤트는 받는다.
        // (알파가 0이어도 uGUI는 사각형 안이면 레이캐스트를 받는다.)
        // 칸 색은 아래 Background가 칠한다. 테두리들이 Background보다 먼저 그려져야
        // 칸 바깥으로 삐져나온 부분만 띠처럼 보이기 때문에 루트에서 색을 뺐다.
        // Button을 쓰면 좌클릭만 들어오고 hover 색까지 덧칠해버려서 쓰지 않는다.
        UnityEngine.UI.Image hit = slot.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        // 그리는 순서는 자식 순서를 따른다. 큰 사각형부터 깔고 작은 것으로 덮어서
        // 가장자리에 3px씩 띠가 남게 만든다.
        //   FocusOutline(-9) → SelectionOutline(-6) → CategoryOutline(-3) → Background(0)
        GameObject focus = Shape("FocusOutline", slot.transform, new Color(1f, 1f, 1f, 0.75f), round);
        K.Stretch(focus, -9, -9, -9, -9);
        focus.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject outline = Shape("SelectionOutline", slot.transform,
            new Color(1f, 0.85f, 0.35f, 0.55f), round);
        K.Stretch(outline, -6, -6, -6, -6);
        outline.GetComponent<UnityEngine.UI.Image>().enabled = false;

        // 계열 테두리. 색은 RelicSlotView.Bind가 유물 카테고리에 맞춰 덮어쓴다.
        GameObject category = Shape("CategoryOutline", slot.transform, Color.white, round);
        K.Stretch(category, -3, -3, -3, -3);

        GameObject bg = Shape("Background", slot.transform, new Color(0.20f, 0.20f, 0.24f, 1f), round);
        K.Stretch(bg, 0, 0, 0, 0);

        // 아이콘 여백은 비율로 잡는다.
        // 픽셀로 잡으면 같은 프리팹을 신체 칸(72)으로 줄였을 때
        // 여백만 그대로 남아서 그림이 지나치게 작아진다.
        float margin = circular ? 0.20f : 0.12f;

        GameObject icon = K.Img("Icon", slot.transform, Color.white);
        FillRelative(icon, margin);
        icon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        // 아이콘이 아직 없는 유물을 위한 글자 자리.
        // 칸 크기가 제각각이라 글자도 자동으로 맞춘다.
        GameObject fallback = K.Text("IconFallback", slot.transform, "", 32f,
            FontStyles.Bold, TextAlignmentOptions.Center);
        FillRelative(fallback, 0.16f);

        TextMeshProUGUI fallbackText = fallback.GetComponent<TextMeshProUGUI>();
        fallbackText.enableAutoSizing = true;
        fallbackText.fontSizeMin = 12f;
        fallbackText.fontSizeMax = 64f;
        fallbackText.enabled = false;

        GameObject cost = K.Text("Cost", slot.transform, "0", 18,
            FontStyles.Bold, TextAlignmentOptions.BottomRight);
        K.Place(cost, K.Anchor.BottomRight, -6, 4, 28, 24);

        RelicSlotView view = slot.AddComponent<RelicSlotView>();

        SerializedObject so = new SerializedObject(view);
        K.SetRef(so, "background", bg.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "categoryOutline", category.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "icon", icon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "iconFallbackText", fallback.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "costText", cost.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "selectionOutline", outline.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "focusOutline", focus.GetComponent<UnityEngine.UI.Image>());
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);

        Debug.Log("[RelicInventoryUIBuilder] 칸 프리팹 생성됨: " + path);
        return saved;
    }

    /// <summary>
    /// 부모 사각형을 비율로 채운다. margin은 각 변에서 몇 퍼센트를 비울지.
    /// 앵커로 잡아야 칸 크기가 바뀌어도 같은 비율을 유지한다.
    /// </summary>
    private static void FillRelative(GameObject go, float margin)
    {
        RectTransform rect = go.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(margin, margin);
        rect.anchorMax = new Vector2(1f - margin, 1f - margin);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>사각형이나 원형 판 하나. round가 null이면 그냥 사각형이다.</summary>
    private static GameObject Shape(string name, Transform parent, Color color, Sprite round)
    {
        GameObject go = K.Img(name, parent, color);

        if (round != null)
        {
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = round;
            image.type = UnityEngine.UI.Image.Type.Simple;
        }

        return go;
    }

    /// <summary>보관함 위에 붙는 필터 줄의 버튼들.</summary>
    private struct RelicFilterBar
    {
        public UnityEngine.UI.Button All;
        public UnityEngine.UI.Button Sword;
        public UnityEngine.UI.Button Orb;
        public UnityEngine.UI.Button Body;
        public UnityEngine.UI.Button Sort;
    }

    /// <summary>
    /// 보관함 위 필터 줄. 전체는 글자, 나머지는 아이콘이다.
    /// 아트가 아직 없어서 아이콘은 코드로 그려서 PNG로 구워 쓴다.
    /// </summary>
    private static RelicFilterBar BuildFilterBar(Transform window)
    {
        GameObject bar = K.Obj("FilterBar", window);
        K.Place(bar, K.Anchor.TopRight, -40, -96, 420, 44);

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RelicFilterBar result = new RelicFilterBar
        {
            All = FilterButton(bar.transform, "Filter_All", "전체", null, 92f),
            Sword = FilterButton(bar.transform, "Filter_Sword", "", FilterIcon("sword"), 44f),
            Orb = FilterButton(bar.transform, "Filter_Orb", "", FilterIcon("orb"), 44f),
            Body = FilterButton(bar.transform, "Filter_Body", "", FilterIcon("body"), 44f),
            Sort = FilterButton(bar.transform, "Filter_Sort", "", FilterIcon("sort"), 44f)
        };

        return result;
    }

    private static UnityEngine.UI.Button FilterButton(Transform parent, string name,
                                                      string label, Sprite icon, float width)
    {
        GameObject go = K.Img(name, parent, K.ButtonBg, true);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 40f);

        LayoutElement size = go.AddComponent<LayoutElement>();
        size.preferredWidth = width;
        size.preferredHeight = 40f;

        UnityEngine.UI.Button button = go.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();

        if (icon != null)
        {
            GameObject iconGO = K.Img("Icon", go.transform, K.AccentText);
            K.Stretch(iconGO, 9, 9, 9, 9);
            iconGO.GetComponent<UnityEngine.UI.Image>().sprite = icon;
        }
        else
        {
            GameObject text = K.Text("Label", go.transform, label, 20,
                FontStyles.Normal, TextAlignmentOptions.Center);
            K.Stretch(text, 0, 0, 0, 0);
        }

        return button;
    }

    /// <summary>
    /// 필터 아이콘을 코드로 그려 PNG로 굽는다. 이미 있으면 그대로 쓴다.
    /// 아트가 나오면 같은 경로의 파일만 갈아 끼우면 된다.
    /// </summary>
    private static Sprite FilterIcon(string key)
    {
        string path = FilterIconFolder + "/Filter_" + key + ".png";

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int Size = 64;
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[Size * Size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (!IconPixel(key, x, y, Size)) continue;

                pixels[y * Size + x] = Color.white;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/UI")) AssetDatabase.CreateFolder("Assets/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(FilterIconFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/UI", "RelicFilter");

        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>아이콘 한 점을 칠할지. 단순한 도형이라 수식 몇 줄로 끝난다.</summary>
    private static bool IconPixel(string key, int x, int y, int size)
    {
        float half = size / 2f;
        float dx = x - half + 0.5f;
        float dy = y - half + 0.5f;

        switch (key)
        {
            // 보주: 속이 빈 동그라미
            case "orb":
            {
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                return distance <= half - 6f && distance >= half - 13f;
            }

            // 검: 세로로 긴 마름모 (칼날)
            case "sword":
            {
                float shape = Mathf.Abs(dx) / (half - 14f) + Mathf.Abs(dy) / (half - 4f);
                float inner = Mathf.Abs(dx) / (half - 21f) + Mathf.Abs(dy) / (half - 11f);
                return shape <= 1f && inner >= 1f;
            }

            // 신체: 모서리를 깎은 사각형 테두리
            case "body":
            {
                float shape = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                bool outside = shape <= half - 7f && Mathf.Abs(dx) + Mathf.Abs(dy) <= size - 24f;
                bool hole = shape <= half - 14f && Mathf.Abs(dx) + Mathf.Abs(dy) <= size - 38f;
                return outside && !hole;
            }

            // 정렬: 아래를 가리키는 화살표
            default:
            {
                bool stem = Mathf.Abs(dx) <= 4f && dy >= -half + 10f && dy <= half - 20f;
                bool head = dy >= -half + 8f && dy <= -half + 22f &&
                            Mathf.Abs(dx) <= (dy + half - 8f) * 0.9f &&
                            Mathf.Abs(dx) >= (dy + half - 8f) * 0.9f - 9f;
                return stem || head;
            }
        }
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
