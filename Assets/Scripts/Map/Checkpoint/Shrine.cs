
using UnityEngine;

//리스폰 제단 스크립트
public class Shrine : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.white;

    private Player player;
    private PlayerRespawn playerRespawn;
    private PlayerHealth playerHealth;


    public bool IsPlayerInRange { get; private set; }

    public Vector2 RespawnPosition
    {
        get
        {
            if (respawnPoint != null) return respawnPoint.position;
            return transform.position;
        }
    }

    private void Awake()
    {
        if (spriteRenderer != null) spriteRenderer.color = inactiveColor;
        
    }

    //제단 활성화
    public void Activate()
    {
        if (!IsPlayerInRange) return;
        if (playerRespawn == null) return;

        playerRespawn.SetRespawnPoint(RespawnPosition);

        // 제단 사용 시 전체 회복
        playerHealth?.FullHeal();

        if (spriteRenderer != null) spriteRenderer.color = activeColor;
        

        Debug.Log("Shrine Activated");
    }

    //플레이어가 제단 범위에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null) return;

        player = foundPlayer;
        playerRespawn = foundPlayer.GetComponent<PlayerRespawn>();
        playerHealth = foundPlayer.GetComponent<PlayerHealth>();

        IsPlayerInRange = true;

        player.SetCurrentShrine(this);
    }

    //플레이어가 제단 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        Player foundPlayer = other.GetComponentInParent<Player>();

        if (foundPlayer == null || foundPlayer != player) return;

        player.ClearCurrentShrine(this);

        player = null;
        playerRespawn = null;
        playerHealth = null;

        IsPlayerInRange = false;
    }
}