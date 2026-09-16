using System;
using UnityEngine;

public class ControlGuidePanel : MonoBehaviour
{
    /// <summary>
    /// 설정되어 있으면 닫기 버튼이 이걸 호출한다.
    /// 타이틀 화면처럼 UIManager가 없는 씬에서 쓰라고 열어둔 자리.
    /// </summary>
    public Action OnClosed;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void OnCloseButton()
    {
        SetVisible(false);  // 조작법 끄기

        if (OnClosed != null)
        {
            OnClosed.Invoke();
            return;
        }

        // 인게임: 일시정지 다시 켜기
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPause();
    }
}
