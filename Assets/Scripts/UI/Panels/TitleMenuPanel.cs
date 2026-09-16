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
        if (controlGuidePanel == null)
        {
            Debug.LogWarning("[TitleMenuPanel] ControlGuidePanel이 연결되지 않았습니다.");
            return;
        }

        // 조작법 패널은 인게임에서는 일시정지로, 여기서는 타이틀로 돌아가야 한다.
        controlGuidePanel.OnClosed = () => SetVisible(true);

        SetVisible(false);
        controlGuidePanel.SetVisible(true);
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
