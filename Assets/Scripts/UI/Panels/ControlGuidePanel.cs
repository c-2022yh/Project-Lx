using UnityEngine;

public class ControlGuidePanel : MonoBehaviour
{
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void OnCloseButton()
    {
        SetVisible(false);  // 조작법 끄기
        UIManager.Instance.ShowPause();  // 일시정지 다시 켜기
    }
}

