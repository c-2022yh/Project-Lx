using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // HUD 체력바 갱신용 이벤트
    public event Action<int, int> OnHealthChanged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    [Header("Invincible")]
    [SerializeField] private float invincibleTime = 1.0f;
    private bool isInvincible;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunTime = 0.25f;

    [Header("Knockback")]
    [SerializeField] private float knockbackX = 8f;
    [SerializeField] private float knockbackY = 5f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.7f;
    private Vector2 respawnPosition;

    //대쉬 중 적과의 충돌 일정시간 무시
    [Header("Enemy Contact Ignore")]
    [SerializeField] private float defaultEnemyContactIgnoreTime = 0.2f;
    public bool IsIgnoringEnemyContact { get; private set; }

    private Coroutine enemyContactIgnoreRoutine;
    private PlayerMove playerMove;

    private Player player;
    private Rigidbody2D rb;
    private PlayerActionState playerActionState;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        playerActionState = GetComponent<PlayerActionState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMove = GetComponent<PlayerMove>();

        currentHealth = maxHealth;

        //시작 위치 기본 리스폰 위치
        respawnPosition = transform.position;
    }

    private void Start()
    {
        // HUD를 현재 체력으로 초기화
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetRespawnPoint(Vector2 newRespawnPosition)
    {
        respawnPosition = newRespawnPosition;
        Debug.Log("Respawn point saved: " + respawnPosition);
    }

    public void TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        if (isInvincible) return;
        if (currentHealth <= 0) return;
        if (playerActionState != null && !playerActionState.CanTakeHit()) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player damaged. HP: " + currentHealth);

        // 체력이 바뀔 때마다 HUD 알림
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HitRoutine(damageSourcePosition));
    }

    private IEnumerator HitRoutine(Vector2 damageSourcePosition)
    {
        isInvincible = true;

        if (playerActionState != null)
        {
            playerActionState.EnterHitStun();
        }

        ApplyKnockback(damageSourcePosition);

        yield return new WaitForSeconds(hitStunTime);

        // 피격 경직 중에만 Normal로 되돌림
        if (playerActionState != null && playerActionState.isHitStunned)
        {
            playerActionState.EnterNormal();
        }

        yield return StartCoroutine(InvincibleBlinkRoutine());

        isInvincible = false;
    }

    private void ApplyKnockback(Vector2 damageSourcePosition)
    {
        if (rb == null) return;

        float dirX = transform.position.x >= damageSourcePosition.x ? 1f : -1f;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(dirX * knockbackX, knockbackY);
    }

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

    private void Die()
    {
        Debug.Log("Player Dead");

        isInvincible = true;

        if (playerActionState != null && playerActionState.CanDie())
        {
            playerActionState.EnterDead();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 사망 중에는 정지
        if (player != null)
        {
            player.SetPhysicsFreeze(true);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // 마지막 저장 위치로 이동
        if (rb != null)
        {
            rb.position = respawnPosition;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = respawnPosition;
        }

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (playerActionState != null)
        {
            playerActionState.RespawnToNormal();
        }

        if (player != null)
        {
            player.SetPhysicsFreeze(false);
        }

        // 리스폰 무적 시간
        yield return StartCoroutine(InvincibleBlinkRoutine());

        isInvincible = false;
    }


    //무적 판정 적용
    public void BeginEnemyContactIgnore()
    {
        if (enemyContactIgnoreRoutine != null)
        {
            StopCoroutine(enemyContactIgnoreRoutine);
            enemyContactIgnoreRoutine = null;
        }

        IsIgnoringEnemyContact = true;
    }

    //딜레이 이후 무적 판정 해제
    public void EndEnemyContactIgnoreAfterDelay()
    {
        EndEnemyContactIgnoreAfterDelay(defaultEnemyContactIgnoreTime);
    }

    //딜레이 이후 무적 판정 해제
    public void EndEnemyContactIgnoreAfterDelay(float delay)
    {
        if (enemyContactIgnoreRoutine != null)
        {
            StopCoroutine(enemyContactIgnoreRoutine);
        }

        enemyContactIgnoreRoutine = StartCoroutine(EnemyContactIgnoreDelayRoutine(delay));
    }

    //무적 판정 해제 코루틴
    private IEnumerator EnemyContactIgnoreDelayRoutine(float delay)
    {
        IsIgnoringEnemyContact = true;

        yield return new WaitForSeconds(delay);

        IsIgnoringEnemyContact = false;
        enemyContactIgnoreRoutine = null;
    }


}