using UnityEngine;
using System.Collections;
using TMPro;

public class TutorialTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private float displayTime = 3f; // 몇 초 보여줄지

    // 툴팁 띄우기 (키, 설명을 받아서)
    public void Show(string key, string desc)
    {
        keyText.text = key;
        descText.text = desc;
        gameObject.SetActive(true);
        StopAllCoroutines();          // 이전 타이머 취소
        StartCoroutine(HideAfterDelay());
    }

    // 일정 시간 후 자동으로 숨기기
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        gameObject.SetActive(false);
    }
}