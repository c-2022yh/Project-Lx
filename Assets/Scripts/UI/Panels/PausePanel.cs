using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    // 일시정지 화면을 켜고 끄는 함수
    // UIManager가 이 함수를 호출해서 패널을 보이거나 숨김
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // "Continue" 버튼이 눌렸을 때 실행
    public void OnResumeButton()
    {
        // UIManager에게 "일시정지 풀기"라고 요청
        UIManager.Instance.TogglePause();
    }

    // "Quit" 버튼이 눌렸을 때 실행
    public void OnQuitButton()
    {
        // 일단 로그만 (나중에 메인 메뉴로 가는 코드로 교체)
        Debug.Log("[PausePanel] 나가기 버튼 눌림 - 메인 메뉴로 이동 예정");
    }
}