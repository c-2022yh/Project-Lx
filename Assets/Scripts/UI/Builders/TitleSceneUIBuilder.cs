#if UNITY_EDITOR && LUDENS_UI_TOOLS
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
    private const string SlotEntryPrefabPath = "Assets/Prefabs/UI/System_Canvas/SaveSlotEntry.prefab";

    // 10.16 시안 두 가지. 디자이너 PDF 6쪽 = A, 7쪽 = B.
    // 바꾸고 싶으면 아래 두 줄만 "A"로 고치고 Build Title Scene을 다시 누르면 된다.
    private const string TitleBackground = "Title_BG_B";
    private const string TitleLogo = "Title_Logo_C"; // 10.19 최종 로고 (여우 + 꼬리)

    private static readonly Color MenuIdle = new Color(0.62f, 0.64f, 0.70f, 1f);

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

        // 배경 그림 (10.16 시안). 화면 비율이 16:9가 아니어도 늘어나지 않고 꽉 차게 덮는다.
        GameObject bg = K.Img("Background", panel.transform, Color.white);
        bg.GetComponent<UnityEngine.UI.Image>().sprite = K.Redesign(TitleBackground);
        K.Stretch(bg, 0, 0, 0, 0);
        AspectRatioFitter fitter = bg.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        // 배경 위 빛 번짐 + 반딧불 (배경 그림을 따라 움직이도록 배경의 자식으로 둔다)
        BuildAmbience(bg.transform);

        // 게임 제목 (붓글씨 로고 그림) + 라이팅
        BuildTitleLogo(panel.transform);

        // 메인 메뉴 (좌측): 상자 없이 글자만. 고른 항목만 하얗게 밝아진다.
        GameObject playBtn = MenuText("Button_Play", panel.transform, "게임하기");
        K.Place(playBtn, K.Anchor.TopLeft, 200, -584, 380, 64);

        GameObject guideBtn = MenuText("Button_Guide", panel.transform, "가이드");
        K.Place(guideBtn, K.Anchor.TopLeft, 200, -670, 380, 64);

        GameObject quitBtn = MenuText("Button_Quit", panel.transform, "게임종료");
        K.Place(quitBtn, K.Anchor.TopLeft, 200, -756, 380, 64);

        // 우측 상단 아이콘 2개 (설정 톱니, 나가기)
        GameObject settingsBtn = IconButton("Button_Settings", panel.transform, "Icon_Settings");
        K.Place(settingsBtn, K.Anchor.TopRight, -118, -46, 52, 52);

        GameObject exitBtn = IconButton("Button_ExitIcon", panel.transform, "Icon_Exit");
        K.Place(exitBtn, K.Anchor.TopRight, -40, -46, 52, 52);

        // 키보드·패드로도 고를 수 있게 첫 항목을 골라 둔다. 고른 항목이 하얗게 보인다.
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null) eventSystem.firstSelectedGameObject = playBtn;

        TitleMenuPanel panelComp = panel.AddComponent<TitleMenuPanel>();

        Wire(playBtn, panelComp.OnPlayButton);
        Wire(guideBtn, panelComp.OnGuideButton);
        Wire(quitBtn, panelComp.OnQuitButton);
        Wire(settingsBtn, panelComp.OnSettingsButton);
        Wire(exitBtn, panelComp.OnExitIconButton);

        return panelComp;
    }

    private const float LogoX = 150f, LogoY = -150f, LogoW = 780f, LogoH = 307f;

    /// <summary>
    /// 로고 뒤 푸른 빛 + 로고 그림 + 로고 모양 안에서만 보이는 빛줄기.
    /// 빛 그림(Title_Logo_?_Glow)은 로고 그림 사방에 로고 너비의 12%만큼 여백을 두고 만든 것이다.
    /// </summary>
    private static void BuildTitleLogo(Transform panel)
    {
        Sprite logoSprite = K.Redesign(TitleLogo);
        Sprite glowSprite = K.Redesign(TitleLogo + "_Glow");

        // 1) 글자 뒤 빛
        UnityEngine.UI.Image glowImage = null;
        if (glowSprite != null && logoSprite != null)
        {
            // 10.23: 가장 밝을 때 조금 세다고 해서 기본 진하기 0.6 → 0.55, 오르내림 ±30% → ±20% (가장 밝을 때 0.78 → 0.66).
            // 10.27: 하늘색 → 바닥 안개와 같은 차분한 푸른 보라 톤
            GameObject glow = K.Img("GameTitleGlow", panel, new Color(0.62f, 0.70f, 0.92f, 0.55f));
            glowImage = glow.GetComponent<UnityEngine.UI.Image>();
            glowImage.sprite = glowSprite;

            // preserveAspect로 그려진 로고의 실제 크기 기준
            float drawnW = Mathf.Min(LogoW, LogoH * logoSprite.rect.width / logoSprite.rect.height);
            float gw = drawnW * 1.24f;
            float gh = gw * glowSprite.rect.height / glowSprite.rect.width;
            float cx = LogoX + LogoW * 0.5f, cy = LogoY - LogoH * 0.5f;
            K.Place(glow, K.Anchor.TopLeft, cx - gw * 0.5f, cy + gh * 0.5f, gw, gh);
        }

        // 2) 로고. 빛줄기가 보이도록 아주 살짝 푸른 회색
        GameObject title = K.Img("GameTitle", panel, new Color(0.90f, 0.92f, 0.97f, 1f));
        UnityEngine.UI.Image titleImage = title.GetComponent<UnityEngine.UI.Image>();
        titleImage.sprite = logoSprite;
        titleImage.preserveAspect = true;
        K.Place(title, K.Anchor.TopLeft, LogoX, LogoY, LogoW, LogoH);

        // 3) 빛줄기: 로고의 여우 부분(머리·꼬리)에서만 보인다. 글자에는 지나가지 않는다.
        //    _FoxMask는 로고 그림에서 글자를 지우고 여우만 남긴 그림이다 (크기 같음).
        GameObject shineMask = K.Img("GameTitleShine", panel, Color.white);
        UnityEngine.UI.Image maskImage = shineMask.GetComponent<UnityEngine.UI.Image>();
        Sprite foxMask = K.Redesign(TitleLogo + "_FoxMask");
        maskImage.sprite = foxMask != null ? foxMask : logoSprite;
        maskImage.preserveAspect = true;
        Mask mask = shineMask.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        K.Place(shineMask, K.Anchor.TopLeft, LogoX, LogoY, LogoW, LogoH);

        GameObject bar = K.Img("ShineBar", shineMask.transform, new Color(0.86f, 0.90f, 1f, 0.9f));
        bar.GetComponent<UnityEngine.UI.Image>().sprite = K.Redesign("Shine_Bar");
        K.Place(bar, K.Anchor.Center, -LogoW * 0.68f, 0f, 150f, LogoH + 180f);
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);

        TitleLogoLight logoLight = title.AddComponent<TitleLogoLight>();
        SerializedObject so = new SerializedObject(logoLight);
        so.FindProperty("glow").objectReferenceValue = glowImage;
        so.FindProperty("shineBar").objectReferenceValue = bar.GetComponent<RectTransform>();
        so.FindProperty("shineTravel").floatValue = LogoW * 1.36f; // 로고 왼쪽 밖 → 오른쪽 밖
        so.FindProperty("glowPulse").floatValue = 0.2f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 바닥을 따라 깔리는 U자 모양 안개 3겹(가운데는 낮고 양옆으로 올라간다)을 깔고, 반딧불 연출을 붙인다.
    /// 그림 이름은 예전 그대로 Aurora_0~2 (10.23에 세로 줄기 빛을 뭉게뭉게한 안개로 바꿨다).
    /// 오로라는 겹마다 밝은 곳이 달라서, 번갈아 밝아지면 빛이 옮겨 다니는 것처럼 보인다.
    /// </summary>
    private static void BuildAmbience(Transform background)
    {
        GameObject amb = K.Obj("Ambience", background);
        K.Stretch(amb, 0, 0, 0, 0);

        // 진하기는 색의 알파로 조절한다. 채도를 살짝 낮춘 푸른색.
        Color[] colors =
        {
            // 10.23: 줄기 빛 → 안개. 안개답게 채도를 조금 더 낮췄다.
            new Color(0.55f, 0.64f, 0.84f, 0.16f),
            new Color(0.52f, 0.66f, 0.82f, 0.12f),
            new Color(0.58f, 0.62f, 0.84f, 0.14f),
        };

        Graphic[] glows = new Graphic[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            GameObject g = K.Img("Aurora_" + i, amb.transform, colors[i]);
            g.GetComponent<UnityEngine.UI.Image>().sprite = K.Redesign("Aurora_" + i);
            // 좌우로 흔들려도 가장자리가 안 보이게 양옆을 60px씩 넓힌다
            K.Stretch(g, -60, 0, -60, 0);
            glows[i] = g.GetComponent<UnityEngine.UI.Image>();
        }

        TitleAmbience comp = amb.AddComponent<TitleAmbience>();
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty arr = so.FindProperty("glows");
        arr.arraySize = glows.Length;
        for (int i = 0; i < glows.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = glows[i];
        so.FindProperty("fireflySprite").objectReferenceValue = K.Redesign("Firefly");
        // 반딧불 색을 아래 오로라와 같은 푸른 계열로 (오로라보다 밝게)
        so.FindProperty("fireflyColor").colorValue = new Color(0.74f, 0.80f, 0.98f, 1f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>글자만 있는 메뉴 항목. 평소엔 회색, 마우스를 올리거나 고르면 흰색.</summary>
    private static GameObject MenuText(string name, Transform parent, string label)
    {
        GameObject go = K.Text(name, parent, label, 44, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
            Color.white);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (K.TitleFont != null) text.font = K.TitleFont;
        text.raycastTarget = true;
        text.characterSpacing = 6f;

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = text;
        btn.colors = MenuColors(MenuIdle);
        return go;
    }

    /// <summary>우측 상단 아이콘 버튼.</summary>
    private static GameObject IconButton(string name, Transform parent, string sprite)
    {
        GameObject go = K.Img(name, parent, Color.white, true);
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
        image.sprite = K.Redesign(sprite);
        image.preserveAspect = true;

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = image;
        btn.colors = MenuColors(new Color(0.85f, 0.87f, 0.92f, 1f));
        return go;
    }

    private static ColorBlock MenuColors(Color idle)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = idle;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.80f, 0.85f, 0.95f, 1f);
        colors.fadeDuration = 0.08f;
        return colors;
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
        // 이미 목록에 있으면 순서를 건드리지 않는다.
        // 팀원이 테스트용으로 다른 씬을 0번에 둘 수 있어서다.
        if (existing >= 0) return "빌드 씬 목록 변경 없음 (Title이 이미 들어 있음)";
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
