using UnityEngine;

public class GameOverPanel : MonoBehaviour
{
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // "다시하기" 버튼
    public void OnRetryButton()
    {
        Time.timeScale = 1f; // 멈췄던 시간 되돌리기
        Debug.Log("[GameOverPanel] 다시하기 - 현재 씬 재시작 예정");
        // [TODO] 나중에 씬 재시작 코드 추가
    }

    // "메인으로" 버튼
    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameOverPanel] 메인 메뉴로 이동 예정");
        // [TODO] 나중에 메인 메뉴 씬으로
    }
}