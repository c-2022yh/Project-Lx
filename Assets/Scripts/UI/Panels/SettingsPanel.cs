using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 화면.
///
/// 편집 중인 값은 draft에만 쌓이고, "적용"을 눌러야 GameSettings.Current에 반영된다.
/// 그래서 "뒤로가기"로 나가면 변경이 버려진다 (시안의 미적용 안내 팝업 자리).
///
/// 오디오/언어 컨트롤은 지금도 값이 저장되지만 아직 아무 데도 반영되지 않는다.
/// 반영 지점은 SettingsApplier의 [AUDIO_HOOK] / [LOCALIZATION_HOOK] 참고.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("비디오")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessValueText;
    [SerializeField] private SettingsArrowSelector vSyncSelector;
    [SerializeField] private SettingsArrowSelector frameLimitSelector;

    [Header("오디오")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiValueText;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterValueText;

    [Header("기타")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private SettingsArrowSelector cameraShakeSelector;
    [SerializeField] private SettingsArrowSelector screenFlashSelector;
    [SerializeField] private SettingsArrowSelector damageNumberSelector;

    [Header("스크롤 / 탭")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform sectionVideo;
    [SerializeField] private RectTransform sectionAudio;
    [SerializeField] private RectTransform sectionControl;
    [SerializeField] private RectTransform sectionEtc;

    [Tooltip("콘텐츠 맨 끝의 빈 공간. 마지막 섹션도 화면 맨 위까지 올라올 수 있게 런타임에 크기를 맞춘다.")]
    [SerializeField] private LayoutElement bottomSpacer;

    private GameSettingsData draft;
    private List<Vector2Int> resolutions;
    private TitleMenuPanel returnToTitle;
    private bool isBinding;

    private void Awake()
    {
        resolutions = GameSettings.GetResolutions();

        // Open()을 거치지 않고 패널이 켜지는 경로가 있어도 draft가 null이 되지 않도록
        // 여기서 먼저 채운다. Open()/기본값 버튼이 BindFrom으로 다시 덮어쓴다.
        draft = GameSettings.Current.Clone();

        BuildDropdownOptions();
        HookControlEvents();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>타이틀에서 열 때. 뒤로가기 목적지를 같이 받는다.</summary>
    public void Open(TitleMenuPanel from)
    {
        returnToTitle = from;
        SetVisible(true);
        BindFrom(GameSettings.Current);
        AdjustBottomSpacer();
    }

    /// <summary>타이틀 외의 곳(일시정지 등)에서 열 때.</summary>
    public void Open()
    {
        returnToTitle = null;
        SetVisible(true);
        BindFrom(GameSettings.Current);
        AdjustBottomSpacer();
    }

    // ── 초기 구성 ──────────────────────────

    private void BuildDropdownOptions()
    {
        if (resolutionDropdown != null)
        {
            List<string> labels = new List<string>();
            foreach (Vector2Int r in resolutions) labels.Add(GameSettings.FormatResolution(r));

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        if (screenModeDropdown != null)
        {
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(new List<string>(GameSettings.ScreenModeLabels));
        }

        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string>(GameSettings.LanguageLabels));
        }
    }

    private void HookControlEvents()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(v => { if (!isBinding) draft.resolutionIndex = v; });

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.AddListener(v => { if (!isBinding) draft.screenModeIndex = v; });

        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(v => { if (!isBinding) draft.languageIndex = v; });

        BindSlider(brightnessSlider, brightnessValueText, v => draft.brightness = v);
        BindSlider(bgmSlider, bgmValueText, v => draft.bgmVolume = v);
        BindSlider(sfxSlider, sfxValueText, v => draft.sfxVolume = v);
        BindSlider(uiSlider, uiValueText, v => draft.uiVolume = v);
        BindSlider(masterSlider, masterValueText, v => draft.masterVolume = v);

        if (vSyncSelector != null)
            vSyncSelector.OnIndexChanged += i => { if (!isBinding) draft.vSync = i == 1; };

        if (frameLimitSelector != null)
            frameLimitSelector.OnIndexChanged += i => { if (!isBinding) draft.frameLimitIndex = i; };

        if (cameraShakeSelector != null)
            cameraShakeSelector.OnIndexChanged += i => { if (!isBinding) draft.cameraShake = i == 1; };

        if (screenFlashSelector != null)
            screenFlashSelector.OnIndexChanged += i => { if (!isBinding) draft.screenFlash = i == 1; };

        if (damageNumberSelector != null)
            damageNumberSelector.OnIndexChanged += i => { if (!isBinding) draft.showDamageNumbers = i == 1; };
    }

    private void BindSlider(Slider slider, TextMeshProUGUI valueText, System.Action<float> setter)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;

        slider.onValueChanged.AddListener(v =>
        {
            if (valueText != null) valueText.text = Mathf.RoundToInt(v).ToString();
            if (!isBinding) setter(v);
        });
    }

    // ── 값 <-> UI ──────────────────────────

    private void BindFrom(GameSettingsData source)
    {
        draft = source.Clone();

        isBinding = true;

        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(ResolveResolutionIndex(draft.resolutionIndex));

        if (screenModeDropdown != null)
            screenModeDropdown.SetValueWithoutNotify(draft.screenModeIndex);

        if (languageDropdown != null)
            languageDropdown.SetValueWithoutNotify(draft.languageIndex);

        SetSlider(brightnessSlider, brightnessValueText, draft.brightness);
        SetSlider(bgmSlider, bgmValueText, draft.bgmVolume);
        SetSlider(sfxSlider, sfxValueText, draft.sfxVolume);
        SetSlider(uiSlider, uiValueText, draft.uiVolume);
        SetSlider(masterSlider, masterValueText, draft.masterVolume);

        vSyncSelector?.Setup(new[] { "OFF", "ON" }, draft.vSync ? 1 : 0);
        frameLimitSelector?.Setup(GameSettings.FrameLimitLabels, draft.frameLimitIndex);
        cameraShakeSelector?.Setup(new[] { "OFF", "ON" }, draft.cameraShake ? 1 : 0);
        screenFlashSelector?.Setup(new[] { "OFF", "ON" }, draft.screenFlash ? 1 : 0);
        damageNumberSelector?.Setup(new[] { "OFF", "ON" }, draft.showDamageNumbers ? 1 : 0);

        isBinding = false;

        // 드롭다운은 SetValueWithoutNotify로 넣었으니 draft에 확정값을 되돌려 놓는다.
        if (resolutionDropdown != null) draft.resolutionIndex = resolutionDropdown.value;
    }

    private void SetSlider(Slider slider, TextMeshProUGUI valueText, float value)
    {
        if (slider == null) return;

        slider.SetValueWithoutNotify(value);
        if (valueText != null) valueText.text = Mathf.RoundToInt(value).ToString();
    }

    private int ResolveResolutionIndex(int savedIndex)
    {
        if (savedIndex >= 0 && savedIndex < resolutions.Count) return savedIndex;

        // 저장된 값이 없으면 현재 화면 크기와 가장 가까운 항목을 고른다.
        for (int i = 0; i < resolutions.Count; i++)
            if (resolutions[i].x == Screen.width && resolutions[i].y == Screen.height)
                return i;

        return Mathf.Max(0, resolutions.Count - 1);
    }

    // ── 하단 버튼 ──────────────────────────

    /// <summary>"적용"</summary>
    public void OnApplyButton()
    {
        GameSettingsData target = GameSettings.Current;

        target.resolutionIndex = draft.resolutionIndex;
        target.screenModeIndex = draft.screenModeIndex;
        target.brightness = draft.brightness;
        target.vSync = draft.vSync;
        target.frameLimitIndex = draft.frameLimitIndex;

        target.masterVolume = draft.masterVolume;
        target.bgmVolume = draft.bgmVolume;
        target.sfxVolume = draft.sfxVolume;
        target.uiVolume = draft.uiVolume;

        target.languageIndex = draft.languageIndex;
        target.cameraShake = draft.cameraShake;
        target.screenFlash = draft.screenFlash;
        target.showDamageNumbers = draft.showDamageNumbers;

        GameSettings.Apply();

        Debug.Log("[SettingsPanel] 설정 적용 및 저장 완료");
    }

    /// <summary>"기본값"</summary>
    public void OnDefaultButton()
    {
        BindFrom(new GameSettingsData());
    }

    /// <summary>"뒤로가기" — 적용하지 않은 변경은 버려진다.</summary>
    public void OnBackButton()
    {
        // [TODO] 시안의 "변경 사항 미적용 시 안내 팝업"을 여기에 붙이면 된다.
        SetVisible(false);
        returnToTitle?.SetVisible(true);
    }

    /// <summary>"단축키 설정"</summary>
    public void OnRebindKeysButton()
    {
        // [REBIND_HOOK] Input System의 PerformInteractiveRebinding으로 구현.
        // UIInputActions / InputSystem_Actions 양쪽을 다뤄야 해서 별도 화면이 필요하다.
        Debug.Log("[SettingsPanel] 단축키 설정 화면 (미구현)");
    }

    // ── 탭 (해당 섹션으로 스크롤) ────────────

    public void OnTabVideo() => ScrollTo(sectionVideo);
    public void OnTabAudio() => ScrollTo(sectionAudio);
    public void OnTabControl() => ScrollTo(sectionControl);
    public void OnTabEtc() => ScrollTo(sectionEtc);

    private void ScrollTo(RectTransform section)
    {
        if (scrollRect == null || section == null) return;

        RectTransform content = scrollRect.content;
        if (content == null) return;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0f;
        float scrollable = content.rect.height - viewportHeight;

        if (scrollable <= 0f)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(TopOf(section) / scrollable);
    }

    /// <summary>
    /// 섹션의 위쪽 모서리가 content 상단에서 얼마나 떨어져 있는지.
    ///
    /// LayoutGroup은 자식을 anchor (0,1)에 놓고
    ///   anchoredPosition.y = -(위에서부터의 거리 + 높이 * pivot.y)
    /// 로 넣는다. pivot이 기본값 0.5라 -anchoredPosition.y를 그대로 쓰면
    /// 섹션의 '중앙' 위치가 나온다. pivot 몫을 빼줘야 위쪽 모서리가 된다.
    /// </summary>
    private static float TopOf(RectTransform section)
    {
        return -section.anchoredPosition.y - section.rect.height * section.pivot.y;
    }

    /// <summary>
    /// 마지막 섹션도 화면 맨 위까지 올라올 수 있도록 콘텐츠 끝에 빈 공간을 둔다.
    /// 이게 없으면 뒤쪽 탭들이 전부 '맨 아래'로만 가서 눌러도 안 움직이는 것처럼 보인다.
    /// </summary>
    private void AdjustBottomSpacer()
    {
        if (bottomSpacer == null || scrollRect == null || scrollRect.viewport == null) return;

        RectTransform content = scrollRect.content;
        if (content == null) return;

        // 여백을 0으로 되돌린 뒤 실제 높이를 재야 누적되지 않는다.
        bottomSpacer.preferredHeight = 0f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float viewportHeight = scrollRect.viewport.rect.height;
        float lastSectionHeight = sectionEtc != null ? sectionEtc.rect.height : 0f;

        bottomSpacer.preferredHeight = Mathf.Max(0f, viewportHeight - lastSectionHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}
