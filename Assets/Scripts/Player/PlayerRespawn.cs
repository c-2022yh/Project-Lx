using System.Collections;
using UnityEngine;

//플레이어 리스폰 처리 스크립트
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.7f;

    [Header("Death Fade")]
    [SerializeField] private float deathFadeDuration = 0.4f;

    private Vector2 respawnPosition;

    private Player player;
    private PlayerHealth playerHealth;
    private PlayerHitReaction hitReaction;
    private PlayerActionState playerActionState;
    private PlayerAnimation playerAnimation;

    private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;


    private void Awake()
    {
        player = GetComponent<Player>();
        playerHealth = GetComponent<PlayerHealth>();
        hitReaction = GetComponent<PlayerHitReaction>();
        playerActionState = GetComponent<PlayerActionState>();
        playerAnimation = GetComponent<PlayerAnimation>();

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

        if (hitReaction != null) hitReaction.ResetReaction();
        if (sr != null) sr.enabled = true;
        if (playerAnimation != null) playerAnimation.PlayDeath();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (player != null) player.SetPhysicsFreeze(true);
        
        StartCoroutine(RespawnRoutine());
    }

    //리스폰 처리 코루틴
    //사망 자세 유지 → 페이드아웃 → 리스폰
    private IEnumerator RespawnRoutine()
    {
        //전체 부활 대기 시간 안에서 페이드 시간을 배분
        float totalTime = Mathf.Max(0f, respawnDelay);
        float fadeTime = Mathf.Clamp(deathFadeDuration, 0f, totalTime);
        float holdTime = totalTime - fadeTime;

        //사망 모션은 완전히 보이는 상태에서 시작
        SetSpriteAlpha(1f);

        //사망 자세를 잠깐 유지
        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        //알파값을 서서히 내려서 사라지게 처리
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                1f,
                0f,
                Mathf.Clamp01(timer / fadeTime)
            );

            SetSpriteAlpha(alpha);

            yield return null;
        }

        SetSpriteAlpha(0f);

        //마지막 체크포인트로 이동
        if (rb != null)
        {
            rb.position = respawnPosition;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = respawnPosition;
        }

        //체력과 행동 상태 복구
        if (playerHealth != null)
        {
            playerHealth.RestoreHealthOnRespawn();
        }

        if (playerActionState != null)
        {
            playerActionState.RespawnToNormal();
        }

        if (playerAnimation != null)
        {
            playerAnimation.ResetOnRespawn();
        }

        //부활할 때 다시 보이도록 투명도 복구
        SetSpriteAlpha(1f);

        if (sr != null) sr.enabled = true;
        if (player != null) player.SetPhysicsFreeze(false);
        if (hitReaction != null) hitReaction.StartRespawnInvincible();
    }

    //기존 RGB 색상은 유지하고 투명도만 변경
    private void SetSpriteAlpha(float alpha)
    {
        if (sr == null) return;

        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}