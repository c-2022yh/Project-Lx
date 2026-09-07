
using UnityEngine;

//리스폰 지점을 설정하는 제단 스크립트
public class Shrine : MonoBehaviour, IInteractable
{
    //현재 활성화된 제단
    private static Shrine activeShrine;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.white;

    public string InteractionText => "활성화";

    private Player player;
    private PlayerRespawn playerRespawn;
    private PlayerHealth playerHealth;

    public bool IsPlayerInRange { get; private set; }
    public bool IsActive { get; private set; }

    public Vector2 RespawnPosition
    {
        get
        {
            if (respawnPoint != null)
            {
                return respawnPoint.position;
            }

            return transform.position;
        }
    }

    //플레이어가 제단과 상호작용 시 호출되는 함수
    public void Interact(Player player)
    {
        Activate();
    }

    private void Awake()
    {
        SetInactiveVisual();
    }

    //제단 활성화
    public void Activate()
    {
        if (!IsPlayerInRange) return;
        if (playerRespawn == null) return;

        //이미 활성화된 제단이면 회복만
        if (activeShrine == this)
        {
            playerHealth?.FullHeal();
            return;
        }

        //기존 활성 제단 비활성화
        if (activeShrine != null)
        {
            activeShrine.Deactivate();
        }

        activeShrine = this;
        IsActive = true;

        //리스폰 지점 변경
        playerRespawn.SetRespawnPoint(RespawnPosition);

        //체력 전체 회복
        playerHealth?.FullHeal();

        SetActiveVisual();

        Debug.Log("Shrine Activated : " + gameObject.name);
    }

    //제단 비활성화
    private void Deactivate()
    {
        IsActive = false;

        SetInactiveVisual();

        Debug.Log("Shrine Deactivated : " + gameObject.name);
    }

    private void SetActiveVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = activeColor;
        }
    }

    private void SetInactiveVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = inactiveColor;
        }
    }

    //플레이어가 제단 범위에 들어옴
    private void OnTriggerEnter2D(Collider2D other)
    {
        Player foundPlayer =  other.GetComponentInParent<Player>();

        if (foundPlayer == null) return;

        player = foundPlayer;

        playerRespawn = foundPlayer.GetComponent<PlayerRespawn>();

        playerHealth = foundPlayer.GetComponent<PlayerHealth>();

        IsPlayerInRange = true;

        player.SetInteractable(this);
    }

    //플레이어가 제단 범위에서 나감
    private void OnTriggerExit2D(Collider2D other)
    {
        Player foundPlayer =  other.GetComponentInParent<Player>();

        if (foundPlayer == null || foundPlayer != player)  return;

        player.ClearInteractable(this);

        player = null;
        playerRespawn = null;
        playerHealth = null;

        IsPlayerInRange = false;
    }
}