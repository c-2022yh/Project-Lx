using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//적 머리 위 체력바 관리
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visibility")]
    [SerializeField, Min(0f)]
    private float visibleDuration = 1.5f;

    private Coroutine hideCoroutine;


    private void Awake()
    {
        HideImmediate();
    }

    //오브젝트 풀링에서 소환될때마다 호출
    private void OnDisable()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        HideImmediate();
    }

    //적이 새로 소환될 때 초기화
    public void Initialize(float currentHp, float maxHp)
    {
        UpdateFill(currentHp, maxHp);
        HideImmediate();
    }

    //피격 시 체력 갱신 및 표시
    public void ShowHealth(float currentHp, float maxHp)
    {
        UpdateFill(currentHp, maxHp);
        SetVisible(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        hideCoroutine = StartCoroutine(HideAfterDelayRoutine());
    }


    //체력바 비율 갱신
    private void UpdateFill(float currentHp, float maxHp)
    {
        if (fillRect == null) return;

        float healthRatio =  maxHp <= 0f ? 0f : currentHp / maxHp;

        fillRect.localScale = new Vector3(Mathf.Clamp01(healthRatio), 1f, 1f);
        Debug.Log($"[EnemyHealthBar] HP: {currentHp}/{maxHp}, " +  $"Ratio: {healthRatio}");

    }

    //일정 시간 후 체력바 숨기기
    private IEnumerator HideAfterDelayRoutine()
    {
        yield return new WaitForSeconds(visibleDuration);

        SetVisible(false);
        hideCoroutine = null;
    }

    //체력바 표시 상태 설정
    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }


    private void HideImmediate()
    {
        SetVisible(false);
    }
}