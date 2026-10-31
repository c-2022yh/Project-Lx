using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Tooltip("일시정지에서 여는 설정창. 같은 System_Canvas 안에 있다. Tools/UI/일시정지에 설정 버튼 넣기 가 연결한다.")]
    [SerializeField] private SettingsPanel settingsPanel;

    /// <summary>일시정지에서 연 설정창이 떠 있는지. Esc를 누르면 설정창부터 닫는다.</summary>
    public bool IsSettingsOpen => settingsPanel != null && settingsPanel.gameObject.activeSelf;

    // 일시정지 화면을 켜고 끄는 함수
    // UIManager가 이 함수를 호출해서 패널을 보이거나 숨김
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        // 10.31 UI 통일: 어두운 판 + 글자 메뉴 + 먹 붓줄 (프리팹은 그대로, 실행 중에 입힌다)
        if (visible) UnifiedPanels.StylePause(this);
    }

    // "Continue" 버튼이 눌렸을 때 실행
    public void OnResumeButton()
    {
        // UIManager에게 "일시정지 풀기"라고 요청
        UIManager.Instance.TogglePause();
    }

    // "조작법" 버튼이 눌렸을 때 실행
    //
    // UIManager.ShowControlGuide를 버튼에 직접 걸 수는 없다.
    // UIManager는 UI_Root에, 이 버튼은 System_Canvas에 있어서 서로 다른 프리팹이고,
    // 프리팹 A의 버튼이 프리팹 B의 컴포넌트를 가리키는 참조는 저장되지 않는다.
    // (실제로 그렇게 걸려 있어서 눌러도 아무 일도 일어나지 않았다.)
    // 그래서 같은 프리팹 안에 있는 이 함수가 받아서 넘긴다. OnResumeButton과 같은 방식이다.
    public void OnControlGuideButton()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowControlGuide();
    }

    // "설정" 버튼이 눌렸을 때 실행. 설정창을 닫으면 일시정지 창으로 돌아온다.
    public void OnSettingsButton()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("[PausePanel] 설정창이 연결되지 않았습니다. Tools/UI/일시정지에 설정 버튼 넣기 를 실행해주세요.", this);
            return;
        }

        SetVisible(false);
        settingsPanel.OnClosed = () => SetVisible(true);
        settingsPanel.Open();
    }

    /// <summary>Esc로 설정창 닫기 (적용 안 한 변경은 버린다. 뒤로가기 버튼과 같다).</summary>
    public void CloseSettings()
    {
        if (IsSettingsOpen) settingsPanel.OnBackButton();
    }

    // "Quit" 버튼이 눌렸을 때 실행
    public void OnQuitButton()
    {
        // 진행도를 저장하고 타이틀로. 다음에 이어하면 마지막 제단에서 시작한다.
        GameSession.SaveAndGoToTitle();
    }
}