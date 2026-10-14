using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// 튜토리얼 안내 한 줄 (키 이름 + 설명).
/// 언제 띄우고 닫을지는 TutorialDirector가 정한다. 여기는 보여주고 숨기는 일만 한다.
/// </summary>
public class TutorialTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private float displayTime = 3f; // 몇 초 보여줄지

    public bool IsShowing => gameObject.activeSelf;

    // 툴팁 띄우기 (키, 설명을 받아서). displayTime 뒤에 저절로 사라진다.
    public void Show(string key, string desc)
    {
        Show(key, desc, displayTime);
    }

    /// <summary>seconds가 0 이하면 Hide를 부를 때까지 떠 있다.</summary>
    public void Show(string key, string desc, float seconds)
    {
        if (keyText != null) keyText.text = key;
        if (descText != null) descText.text = desc;

        gameObject.SetActive(true);
        StopAllCoroutines();          // 이전 타이머 취소

        if (seconds > 0f) StartCoroutine(HideAfterDelay(seconds));
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    // 일정 시간 후 자동으로 숨기기
    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}