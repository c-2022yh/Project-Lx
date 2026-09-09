using UnityEngine;

//상호작용하면 유물 선택창을 여는 보물상자
public class RelicChest : MonoBehaviour, IInteractable
{
    [Header("UI")]
    [SerializeField] private RelicSelectionPanelUI relicSelectionPanel;

    [Header("Relic Pool")]
    [SerializeField] private RelicData[] relicPool;

    private bool isOpened;

    public string InteractionText => "열기";

    private void Awake()
    {
        if (relicSelectionPanel == null)
        {
            relicSelectionPanel = FindFirstObjectByType<RelicSelectionPanelUI>(FindObjectsInactive.Include);
        }
    }

    public void Interact(Player player)
    {
        if (isOpened) return;
        if (relicSelectionPanel == null) return;

        bool opened = relicSelectionPanel.OpenSelection(relicPool);

        if (!opened) return;

        isOpened = true;

        Debug.Log("Relic Chest Opened : " + gameObject.name);

        //일단 테스트용
        //나중에 상자 열린 스프라이트나 애니메이션으로 변경
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null) return;
        if (isOpened) return;

        player.SetInteractable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null) return;

        player.ClearInteractable(this);
    }
}