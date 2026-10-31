using UnityEngine;

//상호작용 시 랜덤 선택 또는 지정 유물을 지급하는 보물상자
public class RelicChest : MonoBehaviour, IInteractable
{
    public enum RewardMode
    {
        RandomChoice = 0,
        FixedRelic = 1
    }

    [Header("Reward")]
    [Tooltip("RandomChoice: 풀에서 최대 3개 중 선택. FixedRelic: 지정한 유물 하나를 바로 획득.")]
    [SerializeField] private RewardMode rewardMode = RewardMode.RandomChoice;
    [Tooltip("FixedRelic 방식에서 지급할 유물 SO. 자동 장착하지 않고 보유 목록에 추가합니다.")]
    [SerializeField] private RelicData fixedRelic;

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
        if (rewardMode == RewardMode.RandomChoice && relicSelectionPanel == null)
        {
            relicSelectionPanel = FindFirstObjectByType<RelicSelectionPanelUI>(FindObjectsInactive.Include);
        }

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (closedSprite == null && spriteRenderer != null) closedSprite = spriteRenderer.sprite;
        SetSprite(closedSprite);

        //이미 연 상자면 열린 모습으로 두고 다시 못 열게
        if (FieldObjectState.IsDone(this))
        {
            isOpened = true;
            SetSprite(openedSprite != null ? openedSprite : selectingSprite);
        }
    }

    public void Interact(Player player)
    {
        if (!isActiveAndEnabled || isOpened || player == null) return;
        if (rewardMode == RewardMode.FixedRelic)
        {
            AcquireFixedRelic(player);
            return;
        }
        if (relicSelectionPanel == null) return;

        bool opened = relicSelectionPanel.OpenSelection(relicPool);
        if (!opened) return;

        isOpened = true;
        FieldObjectState.MarkDone(this);
        relicSelectionPanel.OnSelectionClosed += HandleSelectionClosed;
        SetSprite(selectingSprite != null ? selectingSprite : closedSprite);

        //열린 상자는 범위 안에 남아 있어도 다시 상호작용하지 못하게 정리.
        if (player != null) player.ClearInteractable(this);
        ClearNearbyPlayer();

        Debug.Log("Relic Chest Opened : " + gameObject.name);
    }

    private void AcquireFixedRelic(Player player)
    {
        if (fixedRelic == null)
        {
            Debug.LogWarning("[RelicChest] Fixed Relic에 지급할 유물 SO를 지정해 주세요.", this);
            return;
        }

        PlayerRelicManager manager = player.GetComponent<PlayerRelicManager>();
        if (manager == null) return;
        if (manager.HasRelic(fixedRelic))
        {
            Debug.LogWarning("[RelicChest] 이미 보유한 유물이므로 상자를 열지 않습니다.", this);
            return;
        }

        //획득 이벤트에서 다시 상호작용하더라도 중복 지급하지 않도록 먼저 잠금.
        isOpened = true;
        if (!manager.AcquireRelic(fixedRelic))
        {
            isOpened = false;
            Debug.LogWarning("[RelicChest] 유물을 획득하지 못했습니다. 다시 상호작용할 수 있습니다.", this);
            return;
        }

        FieldObjectState.MarkDone(this);
        SetSprite(openedSprite != null ? openedSprite : selectingSprite);
        player.ClearInteractable(this);
        ClearNearbyPlayer();
        Debug.Log("Relic Chest Reward Acquired : " + fixedRelic.RelicName, this);
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
