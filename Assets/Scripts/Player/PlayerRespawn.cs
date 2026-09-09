using System.Collections;
using UnityEngine;

//플레이어 리스폰 처리 스크립트
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.7f;

    private Vector2 respawnPosition;

    private Player player;
    private PlayerHealth playerHealth;
    private PlayerHitReaction hitReaction;
    private PlayerActionState playerActionState;

    private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;


    private void Awake()
    {
        player = GetComponent<Player>();
        playerHealth = GetComponent<PlayerHealth>();
        hitReaction = GetComponent<PlayerHitReaction>();
        playerActionState = GetComponent<PlayerActionState>();

        rb = GetComponent<Rigidbody2D>();

        //시작 위치를 기본 리스폰 위치로 사용
        respawnPosition = transform.position;
    }

    //플레이어 사망 이벤트 가져오기
    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDeath;
        }
    }

    //플레이어 사망 이벤트 해제
    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDeath;
        }
    }

    //리스폰 위치 변경
    public void SetRespawnPoint(Vector2 newRespawnPosition)
    {
        respawnPosition = newRespawnPosition;
        Debug.Log("Respawn point saved: " + respawnPosition);
    }

    //플레이어 사망 처리
    private void HandlePlayerDeath()
    {
        if (playerActionState != null && playerActionState.CanDie())
        {
            playerActionState.EnterDead();
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (player != null) player.SetPhysicsFreeze(true);
        
        StartCoroutine(RespawnRoutine());
    }

    //리스폰 처리 코루틴
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // 마지막 체크포인트로 이동
        if (rb != null)
        {
            rb.position = respawnPosition;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = respawnPosition;
        }


        if (playerHealth != null) playerHealth.RestoreHealthOnRespawn();
        if (sr != null) sr.enabled = true;
        if (playerActionState != null) playerActionState.RespawnToNormal();
        if (player != null) player.SetPhysicsFreeze(false);
        if (hitReaction != null) hitReaction.StartRespawnInvincible();
        
    }
}