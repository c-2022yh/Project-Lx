
using UnityEngine;

//필드에서 획득하면 유물 선택창을 여는 오브젝트
public class RelicChoicePickup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private RelicSelectionPanelUI relicSelectionPanel;

    //중복 획득 방지
    private bool isPicked;


    private void Awake()
    {
        //선택창이 비활성화 상태여도 찾도록 설정
        if (relicSelectionPanel == null)
        {
            relicSelectionPanel = FindFirstObjectByType<RelicSelectionPanelUI>(FindObjectsInactive.Include);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPicked) return;

        if (!other.CompareTag("Player")) return;

        if (relicSelectionPanel == null)
        {
            return;
        }

        isPicked = true;

        //유물 선택창 열기
        relicSelectionPanel.OpenSelection();

        //획득 오브젝트 숨기기
        gameObject.SetActive(false);
    }
}