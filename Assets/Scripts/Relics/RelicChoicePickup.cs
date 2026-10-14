using UnityEngine;

//상호작용하면 유물 선택창을 여는 보물상자
public class RelicChest : MonoBehaviour, IInteractable
{
    [Header("UI")]
    [SerializeField] private RelicSelectionPanelUI relicSelectionPanel;

    [Header("Relic Pool")]
    [SerializeField] private RelicData[] relicPool;

    [Header("Chest Sprites")]
    [Tooltip("미지정 시 상자 또는 자식의 SpriteRenderer를 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("상자를 열기 전 스프라이트. 미지정 시 기존 스프라이트를 유지합니다.")]
    [SerializeField] private Sprite closedSprite;
    [Tooltip("유물 선택창이 열려 있는 동안 표시할 스프라이트.")]
    [SerializeField] private Sprite selectingSprite;
    [Tooltip("선택창이 닫힌 후 필드에 남는 열린 상자 스프라이트.")]
    [SerializeField] private Sprite openedSprite;

    private bool isOpened;
    private Player nearbyPlayer;

    public string InteractionText => "열기";

    private void Awake()
    {
        if (relicSelectionPanel == null)
        {
            relicSelectionPanel = FindFirstObjectByType<RelicSelectionPanelUI>(FindObjectsInactive.Include);
        }

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (closedSprite == null && spriteRenderer != null) closedSprite = spriteRenderer.sprite;
        SetSprite(closedSprite);
    }

    public void Interact(Player player)
    {
        if (!isActiveAndEnabled || isOpened) return;
        if (relicSelectionPanel == null) return;

        bool opened = relicSelectionPanel.OpenSelection(relicPool);
        if (!opened) return;

        isOpened = true;
        relicSelectionPanel.OnSelectionClosed += HandleSelectionClosed;
        SetSprite(selectingSprite != null ? selectingSprite : closedSprite);

        //열린 상자는 범위 안에 남아 있어도 다시 상호작용하지 못하게 정리.
        if (player != null) player.ClearInteractable(this);
        ClearNearbyPlayer();

        Debug.Log("Relic Chest Opened : " + gameObject.name);
    }

    private void HandleSelectionClosed()
    {
        relicSelectionPanel.OnSelectionClosed -= HandleSelectionClosed;
        SetSprite(openedSprite != null ? openedSprite : selectingSprite);
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || isOpened || !isActiveAndEnabled) return;

        nearbyPlayer = player;
        player.SetInteractable(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        player.ClearInteractable(this);
        if (player == nearbyPlayer) nearbyPlayer = null;
    }

    private void OnDisable()
    {
        ClearNearbyPlayer();
    }

    private void OnDestroy()
    {
        if (relicSelectionPanel != null)
            relicSelectionPanel.OnSelectionClosed -= HandleSelectionClosed;
    }

    private void ClearNearbyPlayer()
    {
        if (nearbyPlayer != null) nearbyPlayer.ClearInteractable(this);
        nearbyPlayer = null;
    }
}
