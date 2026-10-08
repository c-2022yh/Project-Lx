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

    // "Quit" 버튼이 눌렸을 때 실행
    public void OnQuitButton()
    {
        // 일단 로그만 (나중에 메인 메뉴로 가는 코드로 교체)
        Debug.Log("[PausePanel] 나가기 버튼 눌림 - 메인 메뉴로 이동 예정");
    }
}