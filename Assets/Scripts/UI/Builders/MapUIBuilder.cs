#if UNITY_EDITOR && LUDENS_UI_TOOLS
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
            // 이 빌더는 MapPanel을 통째로 지우고 새로 만든다.
            // 손으로 끼운 맵 이미지와 맞춰둔 좌표 값까지 날아가면
            // 빌더를 한 번 더 돌릴 때마다 맵이 어긋난다. 먼저 빼놓았다가 되돌린다.
            Transform existing = popupRoot.transform.Find("MapPanel");
            KeptMapSettings kept = ReadKeptSettings(existing);

            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            MapPanel built = BuildInto(popupRoot.transform);
            ApplyKeptSettings(built, kept);

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
            "- 제단 / 유물 상자 / 포털 / 부술 수 있는 벽 / 몬스터를\n" +
            "  씬에서 자동으로 찾아 마커로 찍습니다\n" +
            "- 보스처럼 전용 스크립트가 없는 것은 MapMarkerSource를 붙이면 됩니다\n" +
            "- 몬스터가 너무 많으면 MapPanel의 Show Enemies를 끄세요\n" +
            "- " + note + "\n\n" +
            "맵 이미지 넣는 법\n" +
            "  1. 맵이 있는 씬을 연 채 Tools/UI > 맵 밑그림 뽑기\n" +
            "  2. 나온 PNG를 MapPanel > Viewport > Content > MapImage의\n" +
            "     Source Image에 끼웁니다\n" +
            "  3. 안내창이 알려준 World Center / Units To Pixels 두 값을\n" +
            "     MapPanel에 그대로 적습니다\n\n" +
            "한 번 넣어두면 이 빌더를 다시 돌려도 그 값은 유지됩니다.",
            "확인");
    }

    /// <summary>빌더가 다시 만들어도 살려둬야 하는 값.</summary>
    private struct KeptMapSettings
    {
        public bool HasValue;
        public Sprite Sprite;
        public Vector2 WorldCenter;
        public float UnitsToPixels;
    }

    private static KeptMapSettings ReadKeptSettings(Transform existing)
    {
        KeptMapSettings kept = new KeptMapSettings();

        if (existing == null) return kept;

        MapPanel old = existing.GetComponent<MapPanel>();

        if (old == null) return kept;

        SerializedObject so = new SerializedObject(old);

        SerializedProperty imageProp = so.FindProperty("mapImage");
        UnityEngine.UI.Image oldImage =
            imageProp != null ? imageProp.objectReferenceValue as UnityEngine.UI.Image : null;

        SerializedProperty centerProp = so.FindProperty("worldCenter");
        SerializedProperty scaleProp = so.FindProperty("unitsToPixels");

        kept.HasValue = true;
        kept.Sprite = oldImage != null ? oldImage.sprite : null;
        kept.WorldCenter = centerProp != null ? centerProp.vector2Value : Vector2.zero;
        kept.UnitsToPixels = scaleProp != null ? scaleProp.floatValue : 0f;

        return kept;
    }

    private static void ApplyKeptSettings(MapPanel panel, KeptMapSettings kept)
    {
        if (panel == null || !kept.HasValue) return;

        SerializedObject so = new SerializedObject(panel);

        if (kept.Sprite != null)
        {
            SerializedProperty imageProp = so.FindProperty("mapImage");
            UnityEngine.UI.Image image =
                imageProp != null ? imageProp.objectReferenceValue as UnityEngine.UI.Image : null;

            if (image != null) image.sprite = kept.Sprite;
        }

        SerializedProperty centerProp = so.FindProperty("worldCenter");
        if (centerProp != null) centerProp.vector2Value = kept.WorldCenter;

        // 0이면 예전 패널에서 못 읽은 것이다. 새로 만든 기본값을 그대로 둔다.
        SerializedProperty scaleProp = so.FindProperty("unitsToPixels");
        if (scaleProp != null && kept.UnitsToPixels > 0f) scaleProp.floatValue = kept.UnitsToPixels;

        so.ApplyModifiedPropertiesWithoutUndo();
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

        // 범례. 아이콘만으로는 무슨 표시인지 알기 어려우므로 이름을 같이 적는다.
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
        K.Place(legend, K.Anchor.BottomLeft, BoardSide, 22, 1500, 38);

        HorizontalLayoutGroup layout = legend.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        foreach (MapMarkerKind kind in MapMarkerView.LegendOrder)
            BuildLegendEntry(legend.transform, kind);
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

        UnityEngine.UI.Image dotImage = dot.GetComponent<UnityEngine.UI.Image>();
        dotImage.sprite = MarkerIcon(kind);
        dotImage.preserveAspect = true;

        LayoutElement dotSize = dot.AddComponent<LayoutElement>();
        dotSize.preferredWidth = 18f;
        dotSize.preferredHeight = 18f;

        // 글자 너비는 TextMeshProUGUI가 스스로 알려준다.
        // 고정값을 주면 "부술 수 있는 벽" 같은 긴 이름이 잘린다.
        K.Text("Label", entry.transform, MapMarkerView.NameOf(kind), 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
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
        FillKindIcons(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(marker, MarkerPrefabPath);
        Object.DestroyImmediate(marker);

        Debug.Log("[MapUIBuilder] MapMarkerView 프리팹 생성됨: " + MarkerPrefabPath);
        return saved;
    }

    // ── 마커 아이콘 ──────────────────────────
    //
    // 아트가 아직 없어서 64x64 PNG를 코드로 그려 프로젝트에 저장한다.
    // 한 번 만들면 다시 만들지 않으므로, 나중에 디자이너가 같은 경로의
    // 파일만 덮어쓰면 그대로 교체된다.

    private const string MarkerIconFolder = "Assets/Sprites/UI/MapMarker";

    /// <summary>MapMarkerKind 번호 순서대로 스프라이트를 채운다.</summary>
    private static void FillKindIcons(SerializedObject so)
    {
        SerializedProperty array = so.FindProperty("kindIcons");

        if (array == null) return;

        System.Array values = System.Enum.GetValues(typeof(MapMarkerKind));
        int count = 0;

        foreach (MapMarkerKind kind in values) count = Mathf.Max(count, (int)kind + 1);

        array.arraySize = count;

        foreach (MapMarkerKind kind in values)
            array.GetArrayElementAtIndex((int)kind).objectReferenceValue = MarkerIcon(kind);
    }

    /// <summary>
    /// 아이콘 파일을 지우지 않고 모양만 새로 뽑고 싶을 때. 코드의 그림을 고친 뒤 실행한다.
    /// 디자이너가 그려 넣은 것도 같이 덮어쓰므로, 평소 빌드에서는 부르지 않는다.
    /// </summary>
    [MenuItem("Tools/UI/맵 마커 아이콘 다시 만들기")]
    public static void RegenerateMarkerIcons()
    {
        if (!EditorUtility.DisplayDialog("맵 마커 아이콘 다시 만들기",
                MarkerIconFolder + " 안의 아이콘을 코드의 그림으로 덮어씁니다.\n" +
                "손으로 그려 넣은 것이 있으면 같이 지워집니다.\n\n진행할까요?",
                "덮어쓰기", "취소"))
        {
            return;
        }

        int count = 0;

        foreach (MapMarkerKind kind in System.Enum.GetValues(typeof(MapMarkerKind)))
        {
            if (MarkerIcon(kind, true) != null) count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료!",
            $"아이콘 {count}개를 다시 만들었습니다.\n\n" +
            "맵 프리팹에 반영하려면\nTools/UI > Build Map Panel In Prefab 을 한 번 더 돌려주세요.",
            "확인");
    }

    private static Sprite MarkerIcon(MapMarkerKind kind)
    {
        return MarkerIcon(kind, false);
    }

    private static Sprite MarkerIcon(MapMarkerKind kind, bool overwrite)
    {
        string path = MarkerIconFolder + "/Marker_" + kind + ".png";

        if (!overwrite)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
        }

        string[] pattern = MarkerPattern(kind);

        int cells = pattern.Length;
        int scale = Mathf.Max(1, IconSize / cells);
        int size = cells * scale;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            // 텍스처는 아래에서 위로 쌓이는데 글자 그림은 위에서 아래로 읽는다. 뒤집는다.
            string row = pattern[(size - 1 - y) / scale];

            for (int x = 0; x < size; x++)
            {
                char cell = row[x / scale];

                float alpha = cell == '#' ? 1f : cell == '+' ? 0.45f : 0f;

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/UI")) AssetDatabase.CreateFolder("Assets/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(MarkerIconFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/UI", "MapMarker");

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

    /// <summary>한 변 몇 픽셀로 저장할지. 실제로는 칸 수의 배수로 맞춰진다.</summary>
    private const int IconSize = 64;

    /// <summary>
    /// 마커 그림. 16x16 칸에 직접 그린다.
    ///
    /// 맵에서 이 아이콘은 13~30픽셀로 그려진다. 그보다 촘촘하게 그려봐야
    /// 화면에서는 안 보이므로 칸을 크게 잡고 실루엣이 읽히는 것만 노렸다.
    /// '#'은 꽉 찬 점, '+'는 옅은 점, '.'은 투명이다.
    /// 색은 MapMarkerView가 종류별로 입히므로 여기서는 흰색으로만 그린다.
    ///
    /// 디자이너가 같은 파일을 덮어쓰면 그대로 교체된다.
    /// (Assets/Sprites/UI/MapMarker/Marker_종류.png)
    /// </summary>
    private static string[] MarkerPattern(MapMarkerKind kind)
    {
        switch (kind)
        {
            // 보스: 해골
            case MapMarkerKind.Boss:
                return new[]
                {
                    "................",
                    "....########....",
                    "..############..",
                    ".##############.",
                    ".##############.",
                    ".##....##....##.",
                    ".##....##....##.",
                    ".##....##....##.",
                    ".##############.",
                    "..#####..#####..",
                    "..############..",
                    "...##########...",
                    "....########....",
                    "...#.#.#.#.#....",
                    "................",
                    "................"
                };

            // 정예: 뿔 달린 머리
            case MapMarkerKind.EliteEnemy:
                return new[]
                {
                    "................",
                    ".##..........##.",
                    ".###........###.",
                    "..###......###..",
                    "...##########...",
                    "..############..",
                    ".##############.",
                    ".###..####..###.",
                    ".###..####..###.",
                    ".##############.",
                    ".##############.",
                    "..############..",
                    "...##########...",
                    "....##.##.##....",
                    "................",
                    "................"
                };

            // 몬스터: 눈 둘 달린 작은 벌레
            case MapMarkerKind.Enemy:
                return new[]
                {
                    "................",
                    "................",
                    "....########....",
                    "..############..",
                    ".##############.",
                    ".##############.",
                    ".###..####..###.",
                    ".###..####..###.",
                    ".##############.",
                    ".##############.",
                    ".##############.",
                    "..############..",
                    "..##.##..##.##..",
                    "..#..#....#..#..",
                    "................",
                    "................"
                };

            // 유물 상자: 뚜껑과 자물쇠
            case MapMarkerKind.RelicChest:
                return new[]
                {
                    "................",
                    "................",
                    "..############..",
                    ".##############.",
                    ".##############.",
                    ".##############.",
                    "................",
                    ".##############.",
                    ".##############.",
                    ".#####.##.#####.",
                    ".#####.##.#####.",
                    ".##############.",
                    ".##############.",
                    "................",
                    "................",
                    "................"
                };

            // 제단: 앉는 의자
            case MapMarkerKind.Shrine:
                return new[]
                {
                    "................",
                    "................",
                    "...##########...",
                    "...##########...",
                    "...##......##...",
                    "...##......##...",
                    "...##########...",
                    "..############..",
                    "..############..",
                    "...##......##...",
                    "...##......##...",
                    "...##......##...",
                    "..####....####..",
                    "................",
                    "................",
                    "................"
                };

            // 출구: 아치형 문
            case MapMarkerKind.Exit:
                return new[]
                {
                    "................",
                    "................",
                    "....########....",
                    "..############..",
                    ".##############.",
                    ".####......####.",
                    ".####......####.",
                    ".####......####.",
                    ".####..##..####.",
                    ".####......####.",
                    ".####......####.",
                    ".####......####.",
                    ".####......####.",
                    ".##############.",
                    "................",
                    "................"
                };

            // 부술 수 있는 벽: 벽돌
            case MapMarkerKind.BreakableWall:
                return new[]
                {
                    "................",
                    "................",
                    ".##############.",
                    ".####.#####.###.",
                    ".####.#####.###.",
                    ".##############.",
                    ".##.#####.#####.",
                    ".##.#####.#####.",
                    ".##############.",
                    ".#####.###.####.",
                    ".#####.###.####.",
                    ".##############.",
                    ".###.#####.####.",
                    ".##############.",
                    "................",
                    "................"
                };

            // 그 밖: 속 빈 마름모
            default:
                return new[]
                {
                    "................",
                    "................",
                    "......####......",
                    "....########....",
                    "..####....####..",
                    ".###........###.",
                    ".##..........##.",
                    ".##..........##.",
                    ".##..........##.",
                    ".###........###.",
                    "..####....####..",
                    "....########....",
                    "......####......",
                    "................",
                    "................",
                    "................"
                };
        }
    }
}
#endif
