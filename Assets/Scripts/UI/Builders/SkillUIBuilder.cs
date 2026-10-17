#if UNITY_EDITOR && LUDENS_UI_TOOLS
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using K = UIBuildKit;

/// <summary>
/// 스킬 창을 만든다. Unity 상단 메뉴 Tools/UI > Build Skill Panel In Prefab.
///
/// Popup_Canvas 프리팹을 직접 고치므로 씬 파일은 건드리지 않는다.
/// 여러 번 실행해도 안전하다. 기존 SkillPanel을 지우고 새로 만든다.
/// </summary>
public static class SkillUIBuilder
{
    private const string PopupCanvasPath = "Assets/Prefabs/UI/Popup_Canvas.prefab";
    private const string SlotPrefabPath = "Assets/Prefabs/UI/SkillPanelSlot.prefab";

    private const float SlotSize = 88f;

    /// <summary>장착칸 사이 간격 (10.16 시안).</summary>
    private const float EquipGap = 28f;

    /// <summary>보유 스킬 칸 크기와 간격. 시안처럼 창 가로를 꽉 채우는 13열.</summary>
    private const float StorageCell = 100f;
    private const float StorageGap = 10f;

    /// <summary>보유 스킬 격자 열 수.</summary>
    private const int StorageColumns = 13;

    /// <summary>뒤에 깔아둘 빈 칸 틀 개수 (두 줄).</summary>
    private const int StorageBackdropCells = StorageColumns * 2;

    [MenuItem("Tools/UI/Build Skill Panel In Prefab")]
    public static void BuildInPrefab()
    {
        if (!K.EnsureKoreanFont()) return;

        if (!EditorUtility.DisplayDialog("스킬 창 (프리팹에 직접)",
                "아래 두 프리팹을 직접 고칩니다. 씬은 건드리지 않습니다.\n\n" +
                "1. " + PopupCanvasPath + "\n   SkillPanel을 새로 만들고 참조 연결\n\n" +
                "2. " + InventoryPrefabFixer.UIRootPath + "\n   UIManager.skillPanel 연결\n\n" +
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
            Transform existing = popupRoot.transform.Find("SkillPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            BuildInto(popupRoot.transform);

            PrefabUtility.SaveAsPrefabAsset(popupRoot, PopupCanvasPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(popupRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string note = InventoryPrefabFixer.LinkPanelInUIRoot("skillPanel", typeof(SkillPanel));

        EditorUtility.DisplayDialog("완료!",
            "SkillPanel 생성 완료 (프리팹)\n\n" +
            "- K키로 엽니다\n" +
            "- 장착칸은 X / A / S / D / F 다섯 개입니다\n" +
            "- X칸은 검 유물 전용이라 잠겨 있습니다 (보여주기만 함)\n" +
            "- 보유 목록은 PlayerSkill.OwnedSkills를 읽습니다\n" +
            "- 조작: 올리면 설명 / 좌클릭 선택 / 우클릭 장착·해제\n" +
            "        드래그로 칸끼리 자리 바꾸기, 아래로 끌어내면 해제\n" +
            "- " + note + "\n\n" +
            "유물이 스킬을 주면 A→S→D→F 첫 빈칸에 자동으로 들어가고,\n" +
            "칸이 꽉 차 있으면 보유만 되어 아래 목록에 남습니다.",
            "확인");
    }

    private static void BuildInto(Transform popupCanvas)
    {
        // 루트
        GameObject panel = K.Obj("SkillPanel", popupCanvas);
        K.Stretch(panel, 0, 0, 0, 0);

        GameObject dim = K.Img("Dim", panel.transform, new Color(0f, 0f, 0f, 0.78f), true);
        K.Stretch(dim, 0, 0, 0, 0);

        // 창 테두리와 배경색을 유물창과 맞춘다. 탭으로 오가는 같은 창이라
        // 색이 다르면 전환할 때 깜빡이는 것처럼 보인다.
        GameObject frame = K.Img("WindowFrame", panel.transform, new Color(0.45f, 0.45f, 0.55f, 0.65f));
        K.Place(frame, K.Anchor.Center, 0, 0, 1524, 844);

        GameObject window = K.Img("Window", panel.transform, new Color(0.07f, 0.07f, 0.09f, 1f), true);
        K.Place(window, K.Anchor.Center, 0, 0, 1520, 840);
        Transform w = window.transform;

        K.TabBar(w, UITabBar.SkillTab);

        // 장착칸 (10.16 시안: 제목과 칸을 가운데로 모은다)
        GameObject equipHeader = K.Text("Header_Equip", w, "장착 중인 스킬", 30, FontStyles.Bold,
            TextAlignmentOptions.Center);
        K.Place(equipHeader, K.Anchor.TopCenter, 0, -100, 400, 40);

        GameObject equipLine = K.Img("Divider_Equip", w, K.Divider);
        K.Place(equipLine, K.Anchor.TopLeft, 40, -150, 1440, 2);

        GameObject equipSlots = K.Obj("EquipSlots", w);
        // 키 글자는 빈 칸 안에 크게 들어가므로 칸 위 여유는 필요 없다.
        K.Place(equipSlots, K.Anchor.TopCenter, 0, -180, SlotSize * 5 + EquipGap * 4, SlotSize);

        HorizontalLayoutGroup equipLayout = equipSlots.AddComponent<HorizontalLayoutGroup>();
        equipLayout.spacing = EquipGap;
        equipLayout.childAlignment = TextAnchor.MiddleCenter;
        equipLayout.childControlWidth = false;
        equipLayout.childControlHeight = false;
        equipLayout.childForceExpandWidth = false;
        equipLayout.childForceExpandHeight = false;

        // 보유 스킬
        GameObject storageHeader = K.Text("Header_Storage", w, "보유 스킬", 30, FontStyles.Bold,
            TextAlignmentOptions.Center);
        K.Place(storageHeader, K.Anchor.TopCenter, 0, -300, 400, 40);

        GameObject storageLine = K.Img("Divider_Storage", w, K.Divider);
        K.Place(storageLine, K.Anchor.TopLeft, 40, -350, 1440, 2);

        float storageWidth = StorageColumns * StorageCell + (StorageColumns - 1) * StorageGap;

        GameObject storage = K.Obj("StorageContainer", w);
        K.Place(storage, K.Anchor.TopCenter, 0, -366, storageWidth, StorageCell * 2 + StorageGap);

        GridLayoutGroup grid = storage.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(StorageCell, StorageCell);
        grid.spacing = new Vector2(StorageGap, StorageGap);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = StorageColumns;

        // 장착칸에서 끌어내려 해제할 수 있어야 하므로 레이캐스트를 받는다.
        UnityEngine.UI.Image storageHit = storage.AddComponent<UnityEngine.UI.Image>();
        storageHit.color = new Color(1f, 1f, 1f, 0f);
        storageHit.raycastTarget = true;

        // 칸과 칸 사이, 목록이 비어 있을 때도 놓을 수 있게.
        storage.AddComponent<SkillStorageDropZone>();

        // 시안처럼 비어 있어도 칸 틀이 보이게 뒤에 빈 칸을 깔아 둔다.
        // (예전의 "보유한 스킬이 없습니다" 안내는 칸 틀과 겹쳐서 뺐다.)
        K.FrameBackdrop(w, "StorageBackdrop", grid, StorageBackdropCells);

        // 설명창
        GameObject desc = K.Img("DescriptionPanel", w, K.SectionBg, true);
        K.StretchBottom(desc, 40, 36, 40, 200);
        Transform d = desc.transform;

        GameObject descIcon = K.Img("Desc_Icon", d, Color.white);
        K.Place(descIcon, K.Anchor.MiddleLeft, 28, 12, 100, 100);
        descIcon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject descName = K.Text("Desc_Name", d, "스킬을 선택하세요", 30, FontStyles.Bold);
        K.Place(descName, K.Anchor.TopLeft, 152, -20, 700, 40);

        GameObject descMeta = K.Text("Desc_Meta", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
        K.Place(descMeta, K.Anchor.TopLeft, 152, -62, 700, 30);

        GameObject descText = K.Text("Desc_Text", d, "", 20,
            FontStyles.Normal, TextAlignmentOptions.TopLeft, K.MutedText);
        K.Place(descText, K.Anchor.TopLeft, 152, -96, 1000, 80);

        GameObject hint = K.Text("HintText", d, "", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(hint, K.Anchor.BottomRight, -32, 16, 700, 30);

        GameObject controls = K.Text("WindowHint", w, "Q / E  전환      Esc  닫기", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(controls, K.Anchor.TopRight, -40, -34, 360, 34);

        // 드래그 고스트
        SkillDragLayer dragLayer = BuildDragLayer(panel.transform);

        // 컴포넌트 + 참조 연결
        SkillPanel comp = panel.AddComponent<SkillPanel>();
        GameObject slotPrefab = BuildSlotPrefab();

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "equipSlotContainer", equipSlots.transform);
        K.SetRef(so, "storageContainer", storage.transform);
        K.SetRef(so, "slotPrefab", slotPrefab);
        K.SetRef(so, "dragLayer", dragLayer);
        K.SetRef(so, "descIcon", descIcon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "descName", descName.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descMeta", descMeta.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "descText", descText.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "hintText", hint.GetComponent<TextMeshProUGUI>());

        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
    }

    private static SkillDragLayer BuildDragLayer(Transform panel)
    {
        GameObject root = K.Obj("SkillDragLayer", panel);
        K.Stretch(root, 0, 0, 0, 0);

        SkillDragLayer comp = root.AddComponent<SkillDragLayer>();

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
    /// 스킬 한 칸 프리팹. 실행할 때마다 새로 만든다.
    /// 칸 구조가 바뀌었는데 예전 프리팹이 남아 조용히 어긋나는 걸 막기 위해서다.
    /// </summary>
    private static GameObject BuildSlotPrefab()
    {
        GameObject slot = new GameObject("SkillPanelSlot", typeof(RectTransform));
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(SlotSize, SlotSize);

        // 루트 Image는 보이지 않지만 마우스 이벤트는 받는다.
        // 칸 색은 아래 Background가 칠한다. 선택 테두리가 먼저 그려져야
        // 칸 바깥으로 삐져나온 부분만 띠처럼 보이기 때문이다.
        UnityEngine.UI.Image hit = slot.AddComponent<UnityEngine.UI.Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        // 10.16 시안의 네모 칸: 모서리가 안으로 파인 판 + 얇은 테두리.
        Sprite fill = K.Redesign("Frame_Square_Fill");
        Sprite border = K.Redesign("Frame_Square_Border");

        GameObject outline = K.Img("SelectionOutline", slot.transform,
            new Color(1f, 0.85f, 0.35f, 0.75f));
        outline.GetComponent<UnityEngine.UI.Image>().sprite = fill;
        K.Stretch(outline, -5, -5, -5, -5);
        outline.GetComponent<UnityEngine.UI.Image>().enabled = false;

        GameObject bg = K.Img("Background", slot.transform, new Color(0.17f, 0.175f, 0.20f, 1f));
        bg.GetComponent<UnityEngine.UI.Image>().sprite = fill;
        K.Stretch(bg, 0, 0, 0, 0);

        // 키 글자: 빈 칸 안에 크고 흐리게 (X / S / D / F). 스킬이 들어오면 SkillPanelSlot이 숨긴다.
        GameObject key = K.Text("KeyText", slot.transform, "", 64,
            FontStyles.Bold, TextAlignmentOptions.Center);
        K.Stretch(key, 0, 0, 0, 0);

        GameObject icon = K.Img("Icon", slot.transform, Color.white);
        K.Stretch(icon, 5, 5, 5, 5);
        icon.GetComponent<UnityEngine.UI.Image>().enabled = false;

        // 아이콘이 없는 스킬에 이름을 대신 띄운다. 칸 안에 들어가야 하므로 자동 축소를 켠다.
        GameObject fallback = K.Text("IconFallback", slot.transform, "", 18,
            FontStyles.Bold, TextAlignmentOptions.Center);
        K.Stretch(fallback, 6, 6, 6, 6);

        TextMeshProUGUI fallbackTmp = fallback.GetComponent<TextMeshProUGUI>();
        fallbackTmp.enableAutoSizing = true;
        fallbackTmp.fontSizeMin = 10f;
        fallbackTmp.fontSizeMax = 20f;
        fallbackTmp.enabled = false;

        // 테두리는 맨 위에 덮는다. 색(장착칸 하늘색 / 보유칸 회색)은 SkillPanelSlot이 칠한다.
        GameObject frame = K.Img("FrameBorder", slot.transform, Color.white);
        frame.GetComponent<UnityEngine.UI.Image>().sprite = border;
        K.Stretch(frame, 0, 0, 0, 0);

        SkillPanelSlot view = slot.AddComponent<SkillPanelSlot>();

        SerializedObject so = new SerializedObject(view);
        K.SetRef(so, "background", bg.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "icon", icon.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "selectionOutline", outline.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "keyText", key.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "iconFallbackText", fallbackTmp);
        K.SetRef(so, "frameBorder", frame.GetComponent<UnityEngine.UI.Image>());
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(slot, SlotPrefabPath);
        Object.DestroyImmediate(slot);

        Debug.Log("[SkillUIBuilder] SkillPanelSlot 프리팹 생성됨: " + SlotPrefabPath);
        return saved;
    }
}
#endif
