#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using K = UIBuildKit;

/// <summary>
/// 타이틀 씬을 통째로 만든다: 메인 메뉴 + 저장 슬롯 선택 + 설정 화면.
/// Unity 상단 메뉴 Tools/UI → Build Title Scene 으로 실행.
///
/// 여러 번 실행해도 안전하다. 기존 Title 씬 내용을 지우고 새로 만든다.
/// </summary>
public static class TitleSceneUIBuilder
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string SlotEntryPrefabPath = "Assets/Prefabs/UI/SaveSlotEntry.prefab";

    [MenuItem("Tools/UI/Build Title Scene")]
    public static void BuildTitleScene()
    {
        if (!K.EnsureKoreanFont()) return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = PrepareScene();

        // ── 카메라 & EventSystem ────────────
        GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;

        if (Object.FindAnyObjectByType<EventSystem>() == null)
            CreateEventSystem();

        // ── Canvas ─────────────────────────
        GameObject canvasGO = new GameObject("Title_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ── 패널 3개 ───────────────────────
        TitleMenuPanel titlePanel = BuildTitleMenu(canvasGO.transform);
        SaveSlotPanel slotPanel = BuildSaveSlotScreen(canvasGO.transform);
        SettingsPanel settingsPanel = SettingsUIBuilder.Build(canvasGO.transform);

        // ── 타이틀 -> 나머지 참조 연결 ───────
        SerializedObject so = new SerializedObject(titlePanel);
        K.SetRef(so, "saveSlotPanel", slotPanel);
        K.SetRef(so, "settingsPanel", settingsPanel);
        so.ApplyModifiedPropertiesWithoutUndo();

        slotPanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);

        // ── 저장 & 빌드 세팅 ────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TitleScenePath);

        string buildNote = FixBuildSettings();

        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog(
            "완료!",
            "타이틀 씬 생성 완료: " + TitleScenePath + "\n\n" +
            "- 메인 메뉴 / 저장 슬롯 / 설정 화면 3개 모두 연결됨\n" +
            "- 한글 폰트 자동 지정됨\n" +
            "- " + buildNote + "\n\n" +
            "남은 수동 작업:\n" +
            "1. TitleMenuPanel의 Background에 타이틀 아트를 넣어주세요\n" +
            "2. '가이드' 버튼을 쓰려면 TitleMenuPanel의 Control Guide Panel 칸에\n" +
            "   조작법 패널을 연결해주세요 (비워두면 버튼이 경고만 찍습니다)\n" +
            "3. GameFlow.GameplaySceneName이 실제 시작 씬인지 확인해주세요",
            "확인");
    }

    // ── 씬 준비 ────────────────────────────

    private static Scene PrepareScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    /// <summary>
    /// 이 프로젝트는 Input System 패키지를 쓰므로 InputSystemUIInputModule이 필요하다.
    /// 타입이 없으면(패키지 제거 등) 레거시 모듈로 떨어진다.
    /// </summary>
    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));

        System.Type moduleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (moduleType != null)
            go.AddComponent(moduleType);
        else
            go.AddComponent<StandaloneInputModule>();
    }

    // ── 1. 타이틀 메인 메뉴 ──────────────────

    private static TitleMenuPanel BuildTitleMenu(Transform canvas)
    {
        GameObject panel = K.Obj("TitleMenuPanel", canvas);
        K.Stretch(panel, 0, 0, 0, 0);

        // 배경 (아트가 들어올 자리)
        GameObject bg = K.Img("Background", panel.transform, new Color(0.07f, 0.09f, 0.08f, 1f));
        K.Stretch(bg, 0, 0, 0, 0);

        // 게임 타이틀
        GameObject title = K.Text("GameTitle", panel.transform, "LUDENS", 96,
            FontStyles.Bold, TextAlignmentOptions.Center);
        K.Place(title, K.Anchor.TopCenter, 0, -120, 900, 130);

        // 메인 메뉴 버튼 3개 (좌측)
        GameObject playBtn = K.Button("Button_Play", panel.transform, "게임하기", 30);
        K.Place(playBtn, K.Anchor.MiddleLeft, 220, 90, 380, 72);

        GameObject guideBtn = K.Button("Button_Guide", panel.transform, "가이드", 30);
        K.Place(guideBtn, K.Anchor.MiddleLeft, 220, 0, 380, 72);

        GameObject quitBtn = K.Button("Button_Quit", panel.transform, "게임 종료", 30);
        K.Place(quitBtn, K.Anchor.MiddleLeft, 220, -90, 380, 72);

        // 우측 상단 아이콘 2개
        GameObject settingsBtn = K.Button("Button_Settings", panel.transform, "설정", 20);
        K.Place(settingsBtn, K.Anchor.TopRight, -126, -40, 72, 44);

        GameObject exitBtn = K.Button("Button_ExitIcon", panel.transform, "종료", 20);
        K.Place(exitBtn, K.Anchor.TopRight, -40, -40, 72, 44);

        TitleMenuPanel panelComp = panel.AddComponent<TitleMenuPanel>();

        Wire(playBtn, panelComp.OnPlayButton);
        Wire(guideBtn, panelComp.OnGuideButton);
        Wire(quitBtn, panelComp.OnQuitButton);
        Wire(settingsBtn, panelComp.OnSettingsButton);
        Wire(exitBtn, panelComp.OnExitIconButton);

        return panelComp;
    }

    // ── 2. 저장 슬롯 선택 ────────────────────

    private static SaveSlotPanel BuildSaveSlotScreen(Transform canvas)
    {
        GameObject panel = K.Obj("SaveSlotPanel", canvas);
        K.Stretch(panel, 0, 0, 0, 0);

        GameObject bg = K.Img("Background", panel.transform, new Color(0.04f, 0.04f, 0.05f, 0.94f), true);
        K.Stretch(bg, 0, 0, 0, 0);

        GameObject title = K.Text("Title", panel.transform, "저장 슬롯 선택", 44,
            FontStyles.Bold, TextAlignmentOptions.Center);
        K.Place(title, K.Anchor.TopCenter, 0, -70, 800, 60);

        // 슬롯 목록
        GameObject listBg = K.Img("SlotListBackground", panel.transform, K.PanelBg);
        K.Place(listBg, K.Anchor.Center, 0, 20, 1000, 560);

        GameObject container = K.Obj("SlotContainer", listBg.transform);
        K.Stretch(container, 20, 20, 20, 110);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // "+ 새 게임 시작"
        GameObject newGameBtn = K.Button("Button_NewGame", listBg.transform, "+  새 게임 시작", 26);
        K.StretchBottom(newGameBtn, 20, 20, 20, 70);

        // "뒤로가기"
        GameObject backBtn = K.Button("Button_Back", panel.transform, "뒤로가기", 24);
        K.Place(backBtn, K.Anchor.BottomCenter, 0, 80, 220, 56);

        SaveSlotPanel panelComp = panel.AddComponent<SaveSlotPanel>();
        GameObject slotPrefab = GetOrCreateSlotEntryPrefab();

        SerializedObject so = new SerializedObject(panelComp);
        K.SetRef(so, "slotContainer", container.transform);
        K.SetRef(so, "slotEntryPrefab", slotPrefab);
        so.ApplyModifiedPropertiesWithoutUndo();

        Wire(newGameBtn, panelComp.OnNewGameButton);
        Wire(backBtn, panelComp.OnBackButton);

        return panelComp;
    }

    /// <summary>슬롯 한 칸 프리팹. 없으면 만든다.</summary>
    private static GameObject GetOrCreateSlotEntryPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(SlotEntryPrefabPath);
        if (existing != null) return existing;

        GameObject slot = new GameObject("SaveSlotEntry", typeof(RectTransform));
        UnityEngine.UI.Image slotBg = slot.AddComponent<UnityEngine.UI.Image>();
        slotBg.color = K.SlotBg;

        UnityEngine.UI.Button slotButton = slot.AddComponent<UnityEngine.UI.Button>();
        slotButton.targetGraphic = slotBg;

        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(940, 120);
        K.FixedHeight(slot, 120);

        // 번호
        GameObject number = K.Text("Number", slot.transform, "1", 44,
            FontStyles.Bold, TextAlignmentOptions.Center, K.MutedText);
        K.Place(number, K.Anchor.MiddleLeft, 10, 0, 70, 120);

        // 썸네일
        GameObject thumb = K.Img("Thumbnail", slot.transform, new Color(0.35f, 0.35f, 0.35f, 1f));
        K.Place(thumb, K.Anchor.MiddleLeft, 90, 0, 150, 90);

        // 슬롯 이름
        GameObject slotName = K.Text("SlotName", slot.transform, "슬롯 이름", 26, FontStyles.Bold);
        K.Place(slotName, K.Anchor.TopLeft, 260, -26, 460, 34);

        // 상세 (플레이 시간 / Lv / 위치)
        GameObject detail = K.Text("Detail", slot.transform, "플레이 시간 00:00:00   Lv.1   시작 지점", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, K.MutedText);
        K.Place(detail, K.Anchor.TopLeft, 260, -64, 560, 30);

        // 저장 시각
        GameObject savedAt = K.Text("SavedAt", slot.transform, "", 19,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight, K.MutedText);
        K.Place(savedAt, K.Anchor.MiddleRight, -24, 0, 260, 30);

        SaveSlotEntry entry = slot.AddComponent<SaveSlotEntry>();

        SerializedObject so = new SerializedObject(entry);
        K.SetRef(so, "numberText", number.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "thumbnailImage", thumb.GetComponent<UnityEngine.UI.Image>());
        K.SetRef(so, "slotNameText", slotName.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "detailText", detail.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "savedAtText", savedAt.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "button", slotButton);
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder("Assets/Prefabs", "UI");

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(slot, SlotEntryPrefabPath);
        Object.DestroyImmediate(slot);

        Debug.Log("[TitleSceneUIBuilder] SaveSlotEntry 프리팹 생성됨: " + SlotEntryPrefabPath);
        return saved;
    }

    // ── 빌드 세팅 정리 ──────────────────────

    /// <summary>
    /// Title 씬만 0번에 추가한다.
    /// 기존 항목은 순서·내용 모두 그대로 두고, 씬 파일을 새로 추가하지도 제거하지도 않는다.
    /// </summary>
    private static string FixBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        int existing = scenes.FindIndex(s => s.path == TitleScenePath);
        if (existing >= 0)
        {
            if (existing == 0) return "빌드 씬 목록 변경 없음 (Title이 이미 0번)";

            EditorBuildSettingsScene entry = scenes[existing];
            scenes.RemoveAt(existing);
            scenes.Insert(0, entry);
        }
        else
        {
            scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();

        // 파일이 없는 항목은 알려만 주고 건드리지 않는다.
        foreach (EditorBuildSettingsScene s in scenes)
            if (!System.IO.File.Exists(s.path))
                Debug.LogWarning($"[TitleSceneUIBuilder] 빌드 목록에 실제 파일이 없는 씬이 있습니다(그대로 두었습니다): {s.path}");

        return "빌드 씬 목록에 Title을 0번으로 추가함 (다른 항목은 그대로)";
    }

    // ── 보조 ───────────────────────────────

    private static void Wire(GameObject buttonGO, UnityEngine.Events.UnityAction action)
    {
        UnityEngine.UI.Button btn = buttonGO.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;
        UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (AssetDatabase.IsValidFolder(parent + "/" + child)) return;
        if (!AssetDatabase.IsValidFolder(parent))
            AssetDatabase.CreateFolder("Assets", parent.Substring("Assets/".Length));
        AssetDatabase.CreateFolder(parent, child);
    }
}

#endif
