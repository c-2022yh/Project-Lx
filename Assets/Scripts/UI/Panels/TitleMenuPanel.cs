using UnityEngine;

/// <summary>
/// 타이틀 화면의 메인 메뉴.
/// 게임하기 → 저장 슬롯 선택, 가이드 → 조작법, 설정 → 설정 화면.
/// </summary>
public class TitleMenuPanel : MonoBehaviour
{
    [Header("이동할 화면")]
    [SerializeField] private SaveSlotPanel saveSlotPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private ControlGuidePanel controlGuidePanel;

    private void Start()
    {
        // 저장된 설정을 읽어 반영. 타이틀이 게임의 첫 화면이므로 여기가 적절한 자리.
        GameSettings.LoadAndApply();

        // 인트로·아무 키나 누르세요·여우불 메뉴 연출 (10.29 시안). 씬을 고치지 않고 여기서 붙인다.
        if (GetComponent<TitleIntro>() == null) gameObject.AddComponent<TitleIntro>();

        SetVisible(true);
        saveSlotPanel?.SetVisible(false);
        settingsPanel?.SetVisible(false);
        controlGuidePanel?.SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // ── 버튼 핸들러 ────────────────────────

    /// <summary>"게임하기"</summary>
    public void OnPlayButton()
    {
        SetVisible(false);
        saveSlotPanel?.Open(this);
    }

    /// <summary>"가이드"</summary>
    public void OnGuideButton()
    {
        // 10.31: 조작법 화면은 코드로 그리는 ControlGuideView를 쓴다 (일시정지 "조작법"과 같은 화면).
        SetVisible(false);
        ControlGuideView.Open(() => SetVisible(true));
    }

    /// <summary>"게임 종료"</summary>
    public void OnQuitButton()
    {
        GameFlow.QuitGame();
    }

    /// <summary>우측 상단 톱니 아이콘</summary>
    public void OnSettingsButton()
    {
        SetVisible(false);
        settingsPanel?.Open(this);
    }

    /// <summary>우측 상단 나가기 아이콘 (종료/로그아웃)</summary>
    public void OnExitIconButton()
    {
        GameFlow.QuitGame();
    }
}
