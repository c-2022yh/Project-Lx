using System.Collections;
using UnityEngine;

//플레이어 피격 경직, 넉백, 무적, 대쉬 중 접촉 무시 등을 처리하는 스크립트
public class PlayerHitReaction : MonoBehaviour
{
    [Header("Invincible")]
    [SerializeField] private float invincibleTime = 1.0f;
    private bool isInvincible;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunTime = 0.25f;

    [Header("Knockback")]
    [SerializeField] private float knockbackX = 8f;
    [SerializeField] private float knockbackY = 5f;

    [Header("Enemy Contact Ignore")]
    [SerializeField] private float defaultEnemyContactIgnoreTime = 0.2f;

    //대쉬 중 적 접촉 무시 여부
    public bool IsIgnoringEnemyContact { get; private set; }

    private Coroutine enemyContactIgnoreRoutine;

    private Rigidbody2D rb;
    private PlayerActionState playerActionState;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerActionState = GetComponent<PlayerActionState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //현재 데미지를 받을 수 있는지 확인하기
    public bool CanTakeDamage()
    {
        if (isInvincible) return false;
        if (playerActionState != null && !playerActionState.CanTakeHit()) return false;

        return true;
    }

    //피격 반응 시작
    public void PlayHitReaction(Vector2 damageSourcePosition)
    {
        if (isInvincible) return;

        StartCoroutine(HitRoutine(damageSourcePosition));
    }

    //피격 반응 코루틴
    private IEnumerator HitRoutine(Vector2 damageSourcePosition)
    {
        isInvincible = true;
        if (playerActionState != null)
        {
            playerActionState.EnterHitStun();
        }

        ApplyKnockback(damageSourcePosition);
        yield return new WaitForSeconds(hitStunTime);

        if (playerActionState != null && playerActionState.isHitStunned)
        {
            playerActionState.EnterNormal();
        }

        yield return StartCoroutine(InvincibleBlinkRoutine());

        isInvincible = false;
    }

    //넉백 적용
    private void ApplyKnockback(Vector2 damageSourcePosition)
    {
        if (rb == null) return;

        float dirX = transform.position.x >= damageSourcePosition.x ? 1f : -1f;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(dirX * knockbackX, knockbackY);

    }

    //무적 상태에서 깜빡임 효과
    private IEnumerator InvincibleBlinkRoutine()
    {
        float timer = 0f;
        float blinkInterval = 0.1f;

        while (timer < invincibleTime)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            timer += blinkInterval;

            yield return new WaitForSeconds(blinkInterval);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    //대쉬 시작 시 적 접촉 무시
    public void BeginEnemyContactIgnore()
    {
        if (enemyContactIgnoreRoutine != null)
        {
            StopCoroutine(enemyContactIgnoreRoutine);
            enemyContactIgnoreRoutine = null;
        }

        IsIgnoringEnemyContact = true;
    }

    //대쉬 종료 후 적 접촉 무시 해제
    public void EndEnemyContactIgnoreAfterDelay()
    {
        EndEnemyContactIgnoreAfterDelay(defaultEnemyContactIgnoreTime);
    }

    //대쉬 종료 후 적 접촉 무시 해제 (지연 시간 지정)
    public void EndEnemyContactIgnoreAfterDelay(float delay)
    {
        if (enemyContactIgnoreRoutine != null)
        {
            StopCoroutine(enemyContactIgnoreRoutine);
        }

        enemyContactIgnoreRoutine = StartCoroutine(EnemyContactIgnoreDelayRoutine(delay));
    }

    //적 접촉 무시 해제 지연 코루틴
    private IEnumerator EnemyContactIgnoreDelayRoutine(float delay)
    {
        IsIgnoringEnemyContact = true;

        yield return new WaitForSeconds(delay);

        IsIgnoringEnemyContact = false;
        enemyContactIgnoreRoutine = null;
    }

    //리스폰 직후 무적
    public void StartRespawnInvincible()
    {
        StartCoroutine(RespawnInvincibleRoutine());
    }

    //리스폰 직후 무적 코루틴
    private IEnumerator RespawnInvincibleRoutine()
    {
        isInvincible = true;
        yield return StartCoroutine(InvincibleBlinkRoutine());
        isInvincible = false;
    }
}