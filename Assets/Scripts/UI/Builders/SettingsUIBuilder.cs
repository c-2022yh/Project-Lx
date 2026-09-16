#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using K = UIBuildKit;

/// <summary>
/// 설정 화면을 만든다. 보통은 TitleSceneUIBuilder가 호출하지만,
/// 다른 씬(일시정지 등)에 붙이고 싶으면 Tools/UI → Build Settings Panel 로 따로 실행할 수 있다.
///
/// 구조는 시안 그대로:
///   좌측 탭 레일(비디오/오디오/조작/기타) + 우측 스크롤 영역 + 하단 적용/기본값/뒤로가기
/// 탭은 페이지를 바꾸는 게 아니라 해당 섹션으로 스크롤한다.
/// </summary>
public static class SettingsUIBuilder
{
    private const float RowHeight = 48f;
    private const float LabelWidth = 260f;
    private const float ControlWidth = 300f;

    [MenuItem("Tools/UI/Build Settings Panel")]
    public static void BuildStandalone()
    {
        if (!K.EnsureKoreanFont()) return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음",
                "현재 씬에 Canvas가 없습니다.\n먼저 Canvas를 하나 만들어주세요.", "확인");
            return;
        }

        SettingsPanel panel = Build(canvas.transform);
        Selection.activeGameObject = panel.gameObject;

        EditorUtility.DisplayDialog("완료!",
            $"'{canvas.name}' 아래에 SettingsPanel을 만들었습니다.\n\n" +
            "여는 쪽에서 settingsPanel.Open() 을 호출하면 됩니다.", "확인");
    }

    public static SettingsPanel Build(Transform canvas)
    {
        GameObject panel = K.Obj("SettingsPanel", canvas);
        K.Stretch(panel, 0, 0, 0, 0);

        GameObject dim = K.Img("Dim", panel.transform, new Color(0f, 0f, 0f, 0.7f), true);
        K.Stretch(dim, 0, 0, 0, 0);

        // 창
        GameObject window = K.Img("Window", panel.transform, K.PanelBg, true);
        K.Place(window, K.Anchor.Center, 0, 0, 1180, 780);

        // 좌측 탭 레일
        GameObject tabRail = K.Img("TabRail", window.transform, K.SectionBg, true);
        K.StretchLeft(tabRail, 16, 16, 150, 100);

        GameObject tabVideo = TabButton(tabRail.transform, "Tab_Video", "비디오", 0);
        GameObject tabAudio = TabButton(tabRail.transform, "Tab_Audio", "오디오", 1);
        GameObject tabControl = TabButton(tabRail.transform, "Tab_Control", "조작", 2);
        GameObject tabEtc = TabButton(tabRail.transform, "Tab_Etc", "기타", 3);

        // 우측 스크롤 영역
        K.ScrollParts scroll = K.ScrollView("SettingsScroll", window.transform, 26f,
            new RectOffset(28, 28, 24, 24));
        RectTransform scrollRt = scroll.root.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.offsetMin = new Vector2(182, 100);
        scrollRt.offsetMax = new Vector2(-16, -16);

        Transform content = scroll.content;

        // ── 섹션들 ─────────────────────────
        GameObject secVideo = Section(content, "비디오");
        TMP_Dropdown resolutionDd = DropdownRow(secVideo.transform, "해상도");
        TMP_Dropdown screenModeDd = DropdownRow(secVideo.transform, "화면 모드");
        (Slider brightness, TextMeshProUGUI brightnessVal) = SliderRow(secVideo.transform, "밝기");
        SettingsArrowSelector vSync = ArrowRow(secVideo.transform, "VSync");
        SettingsArrowSelector frameLimit = ArrowRow(secVideo.transform, "프레임 제한");

        GameObject secAudio = Section(content, "오디오");
        (Slider bgm, TextMeshProUGUI bgmVal) = SliderRow(secAudio.transform, "BGM");
        (Slider sfx, TextMeshProUGUI sfxVal) = SliderRow(secAudio.transform, "효과음");
        (Slider uiVol, TextMeshProUGUI uiVal) = SliderRow(secAudio.transform, "UI 사운드");
        (Slider master, TextMeshProUGUI masterVal) = SliderRow(secAudio.transform, "마스터 볼륨");

        GameObject secControl = Section(content, "조작");
        GameObject rebindBtn = ButtonRow(secControl.transform, "단축키 설정", "설정하기");

        GameObject secEtc = Section(content, "기타");
        TMP_Dropdown languageDd = DropdownRow(secEtc.transform, "언어");
        SettingsArrowSelector cameraShake = ArrowRow(secEtc.transform, "카메라 흔들림");
        SettingsArrowSelector screenFlash = ArrowRow(secEtc.transform, "화면 플래시 효과");
        SettingsArrowSelector damageNumbers = ArrowRow(secEtc.transform, "데미지 숫자 표시");

        // 마지막 섹션도 맨 위까지 스크롤되도록 하는 빈 공간.
        // 실제 높이는 SettingsPanel이 뷰포트 크기에 맞춰 런타임에 정한다.
        GameObject spacer = K.Obj("BottomSpacer", content);
        LayoutElement spacerLayout = K.FixedHeight(spacer, 0f);

        // ── 하단 버튼 ──────────────────────
        GameObject applyBtn = K.Button("Button_Apply", window.transform, "적용", 24);
        K.Place(applyBtn, K.Anchor.BottomLeft, 182, 24, 300, 56);

        GameObject defaultBtn = K.Button("Button_Default", window.transform, "기본값", 24);
        K.Place(defaultBtn, K.Anchor.BottomLeft, 500, 24, 300, 56);

        GameObject backBtn = K.Button("Button_Back", window.transform, "뒤로가기", 24);
        K.Place(backBtn, K.Anchor.BottomLeft, 818, 24, 300, 56);

        // ── 컴포넌트 + 참조 연결 ────────────
        SettingsPanel comp = panel.AddComponent<SettingsPanel>();
        SerializedObject so = new SerializedObject(comp);

        K.SetRef(so, "resolutionDropdown", resolutionDd);
        K.SetRef(so, "screenModeDropdown", screenModeDd);
        K.SetRef(so, "brightnessSlider", brightness);
        K.SetRef(so, "brightnessValueText", brightnessVal);
        K.SetRef(so, "vSyncSelector", vSync);
        K.SetRef(so, "frameLimitSelector", frameLimit);

        K.SetRef(so, "bgmSlider", bgm);
        K.SetRef(so, "bgmValueText", bgmVal);
        K.SetRef(so, "sfxSlider", sfx);
        K.SetRef(so, "sfxValueText", sfxVal);
        K.SetRef(so, "uiSlider", uiVol);
        K.SetRef(so, "uiValueText", uiVal);
        K.SetRef(so, "masterSlider", master);
        K.SetRef(so, "masterValueText", masterVal);

        K.SetRef(so, "languageDropdown", languageDd);
        K.SetRef(so, "cameraShakeSelector", cameraShake);
        K.SetRef(so, "screenFlashSelector", screenFlash);
        K.SetRef(so, "damageNumberSelector", damageNumbers);

        K.SetRef(so, "scrollRect", scroll.scrollRect);
        K.SetRef(so, "sectionVideo", secVideo.GetComponent<RectTransform>());
        K.SetRef(so, "sectionAudio", secAudio.GetComponent<RectTransform>());
        K.SetRef(so, "sectionControl", secControl.GetComponent<RectTransform>());
        K.SetRef(so, "sectionEtc", secEtc.GetComponent<RectTransform>());
        K.SetRef(so, "bottomSpacer", spacerLayout);

        so.ApplyModifiedPropertiesWithoutUndo();

        Wire(applyBtn, comp.OnApplyButton);
        Wire(defaultBtn, comp.OnDefaultButton);
        Wire(backBtn, comp.OnBackButton);
        Wire(rebindBtn, comp.OnRebindKeysButton);

        Wire(tabVideo, comp.OnTabVideo);
        Wire(tabAudio, comp.OnTabAudio);
        Wire(tabControl, comp.OnTabControl);
        Wire(tabEtc, comp.OnTabEtc);

        return comp;
    }

    // ── 조각들 ─────────────────────────────

    /// <summary>
    /// 좌측 탭 버튼. 시안에는 아이콘이 있지만 한글 폰트(TerrarumSans)에 이모지 글리프가
    /// 없어 네모로 나오므로 글자만 쓴다. 아이콘 스프라이트가 준비되면
    /// 여기에 Image 자식을 하나 추가하면 된다.
    /// </summary>
    private static GameObject TabButton(Transform rail, string name, string label, int index)
    {
        GameObject go = K.Obj(name, rail);
        K.Place(go, K.Anchor.TopCenter, 0, -(12 + index * 76), 130, 68);

        UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
        bg.color = K.SlotBg;

        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = bg;

        GameObject labelGO = K.Text("Label", go.transform, label, 20,
            FontStyles.Normal, TextAlignmentOptions.Center);
        K.Stretch(labelGO, 0, 0, 0, 0);

        return go;
    }

    /// <summary>섹션 = 헤더 + 행들. 높이는 부모 Content의 VLG가 preferredHeight로 읽어간다.</summary>
    private static GameObject Section(Transform content, string title)
    {
        GameObject section = K.Obj("Section_" + title, content);

        VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        // true여야 자식의 LayoutElement(FixedHeight) 높이를 읽는다.
        // false면 sizeDelta를 보는데, 헤더/행은 sizeDelta를 세팅하지 않으므로 전부 높이 0이 된다.
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter는 쓰지 않는다.
        // 부모(ScrollView Content)의 VLG가 childControlHeight=true라
        // 이 섹션의 preferredHeight를 직접 읽어간다. 한 프레임 지연도 없다.

        GameObject header = K.Text("Header", section.transform, title, 30, FontStyles.Bold);
        K.FixedHeight(header, 44);

        GameObject divider = K.Img("Divider", section.transform, K.Divider);
        K.FixedHeight(divider, 2);

        return section;
    }

    /// <summary>행 = 왼쪽 라벨 + 오른쪽 컨트롤 자리. 컨트롤을 담을 Transform을 돌려준다.</summary>
    private static Transform Row(Transform section, string label)
    {
        GameObject row = K.Obj("Row_" + label, section);
        K.FixedHeight(row, RowHeight);

        GameObject labelGO = K.Text("Label", row.transform, label, 22);
        K.Place(labelGO, K.Anchor.MiddleLeft, 8, 0, LabelWidth, RowHeight);

        GameObject control = K.Obj("Control", row.transform);
        K.Place(control, K.Anchor.MiddleRight, -8, 0, ControlWidth, RowHeight);

        return control.transform;
    }

    private static TMP_Dropdown DropdownRow(Transform section, string label)
    {
        Transform control = Row(section, label);

        TMP_Dropdown dropdown = K.Dropdown("Dropdown", control);
        K.Stretch(dropdown.gameObject, 0, 6, 0, 6);

        return dropdown;
    }

    private static (Slider, TextMeshProUGUI) SliderRow(Transform section, string label)
    {
        Transform control = Row(section, label);

        Slider slider = K.Slider("Slider", control);
        RectTransform sliderRt = slider.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0.5f);
        sliderRt.anchorMax = new Vector2(1, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(-26, 0);
        sliderRt.sizeDelta = new Vector2(-52, 20);

        GameObject valueGO = K.Text("Value", control, "0", 20,
            FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        K.Place(valueGO, K.Anchor.MiddleRight, 0, 0, 44, RowHeight);

        return (slider, valueGO.GetComponent<TextMeshProUGUI>());
    }

    private static SettingsArrowSelector ArrowRow(Transform section, string label)
    {
        Transform control = Row(section, label);

        GameObject bg = K.Img("Box", control, K.SlotBg, true);
        K.Stretch(bg, 0, 6, 0, 6);

        GameObject left = K.Button("Button_Left", bg.transform, "<", 20, new Color(0, 0, 0, 0));
        K.Place(left, K.Anchor.MiddleLeft, 4, 0, 40, 34);

        GameObject right = K.Button("Button_Right", bg.transform, ">", 20, new Color(0, 0, 0, 0));
        K.Place(right, K.Anchor.MiddleRight, -4, 0, 40, 34);

        GameObject value = K.Text("Value", bg.transform, "ON", 20,
            FontStyles.Normal, TextAlignmentOptions.Center);
        K.Stretch(value, 48, 0, 48, 0);

        SettingsArrowSelector selector = bg.AddComponent<SettingsArrowSelector>();

        SerializedObject so = new SerializedObject(selector);
        K.SetRef(so, "valueText", value.GetComponent<TextMeshProUGUI>());
        K.SetRef(so, "leftButton", left.GetComponent<UnityEngine.UI.Button>());
        K.SetRef(so, "rightButton", right.GetComponent<UnityEngine.UI.Button>());
        so.ApplyModifiedPropertiesWithoutUndo();

        return selector;
    }

    private static GameObject ButtonRow(Transform section, string label, string buttonText)
    {
        Transform control = Row(section, label);

        GameObject btn = K.Button("Button", control, buttonText, 20);
        K.Stretch(btn, 0, 6, 0, 6);

        return btn;
    }

    private static void Wire(GameObject buttonGO, UnityEngine.Events.UnityAction action)
    {
        UnityEngine.UI.Button btn = buttonGO.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;
        UnityEventTools.AddPersistentListener(btn.onClick, action);
    }
}
#endif
