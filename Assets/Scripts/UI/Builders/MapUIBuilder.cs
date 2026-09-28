#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using K = UIBuildKit;

/// <summary>
/// 지도 화면을 만든다. Unity 상단 메뉴 Tools/UI > Build Map Panel In Prefab.
///
/// Popup_Canvas 프리팹을 직접 고치므로 씬 파일은 건드리지 않는다.
/// (씬에 만드는 메뉴는 두지 않았다. 씬에 만들면 Popup_Canvas 프리팹에 Apply 하는 것을
///  잊기 쉽고, UIManager 참조가 프리팹 에셋을 가리켜 엉뚱한 곳에 연결되기 때문이다.)
///
/// 여러 번 실행해도 안전하다. 기존 MapPanel을 지우고 새로 만든다.
/// </summary>
public static class MapUIBuilder
{
    private const string PopupCanvasPath = "Assets/Prefabs/UI/Popup_Canvas.prefab";
    private const string MarkerPrefabPath = "Assets/Prefabs/UI/MapMarkerView.prefab";

    private const float WindowWidth = 1560f;
    private const float WindowHeight = 880f;

    /// <summary>맵 판 안쪽 여백. 제목과 범례가 들어갈 자리.</summary>
    private const float BoardTop = 92f;
    private const float BoardBottom = 88f;
    private const float BoardSide = 36f;

    [MenuItem("Tools/UI/Build Map Panel In Prefab")]
    public static void BuildInPrefab()
    {
        if (!K.EnsureKoreanFont()) return;

        if (!EditorUtility.DisplayDialog("지도 (프리팹에 직접)",
                "아래 두 프리팹을 직접 고칩니다. 씬은 건드리지 않습니다.\n\n" +
                "1. " + PopupCanvasPath + "\n   MapPanel을 새로 만들고 참조 연결\n\n" +
                "2. " + InventoryPrefabFixer.UIRootPath + "\n   UIManager.mapPanel 연결\n\n" +
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
            Transform existing = popupRoot.transform.Find("MapPanel");
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

        string note = InventoryPrefabFixer.LinkPanelInUIRoot("mapPanel", typeof(MapPanel));

        EditorUtility.DisplayDialog("완료!",
            "MapPanel 생성 완료 (프리팹)\n\n" +
            "- 탭을 누르고 있는 동안만 보입니다\n" +
            "- 창(인벤토리·일시정지·조작법)이 떠 있으면 탭은 무시됩니다\n" +
            "- 제단 / 유물 상자 / 포털은 씬에서 자동으로 찾아 마커로 찍습니다\n" +
            "- 보스처럼 전용 스크립트가 없는 것은 MapMarkerSource를 붙이면 됩니다\n" +
            "- " + note + "\n\n" +
            "맵 이미지가 없어도 동작합니다. 마커 위치만 먼저 확인해보세요.\n" +
            "이미지가 나오면 MapPanel의 Map Image에 넣고\n" +
            "World Center / Units To Pixels 두 값만 맞추면 됩니다.",
            "확인");
    }

    private static MapPanel BuildInto(Transform popupCanvas)
    {
        // 루트
        GameObject panel = K.Obj("MapPanel", popupCanvas);
        K.Stretch(panel, 0, 0, 0, 0);

        // 반투명으로 보여야 하므로 인벤토리(0.78)보다 옅게 깐다.
        GameObject dim = K.Img("Dim", panel.transform, new Color(0f, 0f, 0f, 0.55f), true);
        K.Stretch(dim, 0, 0, 0, 0);

        GameObject window = K.Img("Window", panel.transform,
            new Color(0.08f, 0.08f, 0.09f, 0.82f), true);
        K.Place(window, K.Anchor.Center, 0, 0, WindowWidth, WindowHeight);
        Transform w = window.transform;

        GameObject title = K.Text("Title", w, "지도", 38, FontStyles.Bold);
        K.Place(title, K.Anchor.TopLeft, BoardSide + 4, -26, 400, 50);

        GameObject titleLine = K.Img("Divider_Title", w, K.Divider);
        K.Place(titleLine, K.Anchor.TopLeft, BoardSide, -BoardTop + 8, WindowWidth - BoardSide * 2, 2);

        // 맵이 놓이는 판. 밖으로 나간 마커가 창을 넘지 않도록 잘라낸다.
        GameObject viewport = K.Img("Viewport", w, new Color(0.04f, 0.04f, 0.05f, 0.65f));
        K.Stretch(viewport, BoardSide, BoardTop, BoardSide, BoardBottom);
        viewport.AddComponent<RectMask2D>();

        // 마커와 플레이어 점의 좌표 기준. 자식들은 이 사각형의 한가운데를 원점으로 놓인다.
        GameObject content = K.Obj("MapContent", viewport.transform);
        K.Stretch(content, 0, 0, 0, 0);

        GameObject mapImage = K.Img("MapImage", content.transform, Color.white);
        K.Stretch(mapImage, 0, 0, 0, 0);

        UnityEngine.UI.Image mapImageComp = mapImage.GetComponent<UnityEngine.UI.Image>();
        mapImageComp.preserveAspect = true;

        // 스프라이트가 없으면 흰 사각형이 되므로 꺼둔다. MapPanel이 이미지가 생기면 켠다.
        mapImageComp.enabled = false;

        GameObject notice = K.Text("NoticeText", content.transform, "", 22,
            FontStyles.Normal, TextAlignmentOptions.Center, K.MutedText);
        K.Place(notice, K.Anchor.Center, 0, 0, 900, 90);

        GameObject playerDot = K.Img("PlayerDot", content.transform, new Color(1f, 1f, 1f, 1f));
        K.Place(playerDot, K.Anchor.Center, 0, 0, 16, 16);

        // 범례. 마커가 색으로만 구분되므로 이게 없으면 무슨 표시인지 알 수 없다.
        BuildLegend(w);

        GameObject hint = K.Text("HintText", w, "탭을 떼면 닫힙니다", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(hint, K.Anchor.BottomRight, -BoardSide, 26, 520, 30);

        // 컴포넌트 + 참조 연결
        MapPanel comp = panel.AddComponent<MapPanel>();
        GameObject markerPrefab = BuildMarkerPrefab();

        SerializedObject so = new SerializedObject(comp);
        K.SetRef(so, "mapContent", content.GetComponent<RectTransform>());
        K.SetRef(so, "mapImage", mapImageComp);
        K.SetRef(so, "playerDot", playerDot.GetComponent<RectTransform>());
        K.SetRef(so, "noticeText", notice.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "markerPrefab", markerPrefab);
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);

        return comp;
    }

    /// <summary>색이 무슨 뜻인지 알려주는 줄. 창 아래쪽에 가로로 놓는다.</summary>
    private static void BuildLegend(Transform window)
    {
        GameObject legend = K.Obj("Legend", window);
        K.Place(legend, K.Anchor.BottomLeft, BoardSide, 22, 900, 38);

        HorizontalLayoutGroup layout = legend.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 26;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        MapMarkerKind[] kinds =
        {
            MapMarkerKind.Shrine,
            MapMarkerKind.RelicChest,
            MapMarkerKind.Boss,
            MapMarkerKind.Exit
        };

        foreach (MapMarkerKind kind in kinds) BuildLegendEntry(legend.transform, kind);
    }

    private static void BuildLegendEntry(Transform legend, MapMarkerKind kind)
    {
        GameObject entry = K.Obj("Legend_" + kind, legend);

        HorizontalLayoutGroup layout = entry.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject dot = K.Img("Dot", entry.transform, MapMarkerView.ColorOf(kind));
        LayoutElement dotSize = dot.AddComponent<LayoutElement>();
        dotSize.preferredWidth = 14f;
        dotSize.preferredHeight = 14f;

        GameObject label = K.Text("Label", entry.transform, MapMarkerView.NameOf(kind), 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);

        LayoutElement labelSize = label.AddComponent<LayoutElement>();
        labelSize.preferredWidth = 84f;
        labelSize.preferredHeight = 26f;
    }

    /// <summary>
    /// 마커 한 개 프리팹. 실행할 때마다 새로 만든다.
    /// 앵커와 피벗을 가운데로 고정해야 MapPanel이 넣는 anchoredPosition이
    /// 판 한가운데를 원점으로 읽힌다.
    /// </summary>
    private static GameObject BuildMarkerPrefab()
    {
        GameObject marker = new GameObject("MapMarkerView", typeof(RectTransform));

        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(12f, 12f);

        UnityEngine.UI.Image icon = marker.AddComponent<UnityEngine.UI.Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;

        GameObject label = K.Text("Label", marker.transform, "", 16,
            FontStyles.Normal, TextAlignmentOptions.Center, K.AccentText);
        K.Place(label, K.Anchor.Center, 0, -18, 160, 22);
        label.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        label.GetComponent<TextMeshProUGUI>().enabled = false;

        MapMarkerView view = marker.AddComponent<MapMarkerView>();

        SerializedObject so = new SerializedObject(view);
        K.SetRef(so, "icon", icon);
        K.SetRef(so, "labelText", label.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(marker, MarkerPrefabPath);
        Object.DestroyImmediate(marker);

        Debug.Log("[MapUIBuilder] MapMarkerView 프리팹 생성됨: " + MarkerPrefabPath);
        return saved;
    }
}
#endif
