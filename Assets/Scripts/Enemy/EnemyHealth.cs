using System;
using System.Collections;
using UnityEngine;

//적의 체력, 피격, 넉백, 사망 처리를 담당하는 스크립트
public class EnemyHealth : MonoBehaviour
{
    //인스펙터에서 변수 설정
    [Header("Health")]
    [SerializeField] private float maxHp = 10f;

    private float currentHp;

    [Header("Health UI")]
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Damage Popup")]
    [SerializeField] private DamagePopup damagePopupPrefab;

    [SerializeField]
    private Vector3 damagePopupOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Hit Effect")]
    [SerializeField] private EnemyHitEffect hitEffectPrefab;

    [SerializeField]
    private Vector3 hitEffectOffset = new Vector3(0f, 0.3f, 0f);

    [Header("Hit Feedback")]
    [SerializeField] private float hitStunTime = 0.12f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackUpForce = 1f;

    [SerializeField, Min(1)]
    private int hitBlinkCount = 2;

    [SerializeField, Min(0.01f)]
    private float hitBlinkInterval = 0.03f;

    [Header("Reward")]
    [SerializeField] private PlayerEnergy playerEnergy;
    [SerializeField] private float energyReward = 20f;

    private EnemyStats enemyStats;
    private EnemyAI ai;
    private EnemyAnimation enemyAnimation;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Collider2D[] enemyColliders;
    private bool[] defaultColliderStates;

    private bool isDead;
    private bool isHitStunned;

    private Coroutine hitFeedbackCoroutine;

    public bool IsDead => isDead;
    public bool IsHitStunned => isHitStunned;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private void Awake()
    {
        //컴포넌트 연결
        enemyStats = GetComponent<EnemyStats>();
        ai = GetComponent<EnemyAI>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        enemyColliders = GetComponentsInChildren<Collider2D>(true);

        defaultColliderStates = new bool[enemyColliders.Length];

        for (int i = 0; i < enemyColliders.Length; i++)
        {
            defaultColliderStates[i] = enemyColliders[i].enabled;
        }
    }

    private void OnEnable()
    {
        ResetHealth();
    }

    private void OnDisable()
    {
        hitFeedbackCoroutine = null;
    }

    //체력 초기화, 리젠 등에서 사용
    private void ResetHealth()
    {
        isDead = false;
        isHitStunned = false;

        currentHp = maxHp;

        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white;
        }

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        RestoreColliders();

        if (playerEnergy == null)
        {
            playerEnergy = FindAnyObjectByType<PlayerEnergy>();
        }

        if (healthBar != null)
        {
            healthBar.Initialize(currentHp, maxHp);
        }
    }

    //적이 피해를 입음
    public void TakeDamage(DamageInfo damageInfo,Vector2 hitDirection)
    {
        if (isDead) return;

        //피해 타입에 맞는 방어스탯 계산
        float defense = 
            damageInfo.damageType switch
            {
                DamageType.Physical => enemyStats.Defense.physicalDefense,
                DamageType.Magical => enemyStats.Defense.magicalDefense,
                _ => 0f
            };

        float finalDamage =
            DamageCalculator.Calculate(
                damageInfo,
                defense,
                enemyStats.Defense.durability
            );

        currentHp = Mathf.Max(0f, currentHp - finalDamage);


        ShowDamageFeedback(finalDamage);

        if (currentHp <= 0f)
        {
            Die(damageInfo);
            return;
        }

        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
        }

        hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine(hitDirection));
    }

    //피격에 따른 hp감소 체력바 보여주기
    private void ShowDamageFeedback(float finalDamage)
    {
        if (healthBar != null)
        {
            healthBar.ShowHealth(currentHp, maxHp);
        }

        if (damagePopupPrefab != null)
        {
            DamagePopup popup = Instantiate(
                damagePopupPrefab,
                transform.position +
                damagePopupOffset,
                Quaternion.identity
            );

            popup.Show(finalDamage);
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(
                hitEffectPrefab,
                transform.position +
                hitEffectOffset,
                Quaternion.identity
            );
        }
    }

    //피격 루틴: 넉백
    private IEnumerator HitFeedbackRoutine(Vector2 hitDirection)
    {
        isHitStunned = true;

        if (rb != null)
        {
            float hitDir =  hitDirection.x >= 0f ? 1f : -1f;

            rb.linearVelocity = new Vector2(hitDir * knockbackForce, knockbackUpForce);
        }

        float blinkDuration = 0f;

        for (int i = 0; i < hitBlinkCount; i++)
        {
            if (sr == null) break;

            sr.enabled = false;

            yield return new WaitForSeconds(hitBlinkInterval);

            sr.enabled = true;

            yield return new WaitForSeconds(hitBlinkInterval);

            blinkDuration += hitBlinkInterval * 2f;
        }

        float remainingStun = hitStunTime - blinkDuration;

        if (remainingStun > 0f)
        {
            yield return new WaitForSeconds(remainingStun);
        }

        if (sr != null)
        {
            sr.enabled = true;
        }

        isHitStunned = false;
        hitFeedbackCoroutine = null;
    }

    private void Die(DamageInfo lastDamageInfo)
    {
        if (isDead) return;

        isDead = true;

        NotifyKill(lastDamageInfo);
        GiveReward();

        ai?.StopAI();

        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = null;
        }

        isHitStunned = false;

        if (sr != null)
        {
            sr.enabled = true;
        }

        if (healthBar != null)
        {
            healthBar.Initialize(0f, maxHp);
        }

        DisablePhysicalPresence();

        if (enemyAnimation != null)
        {
            enemyAnimation.PlayDeath(FinishDeath);
        }
        else
        {
            FinishDeath();
        }
    }

    //적이 처치되었다고 알려줌
    private void NotifyKill(DamageInfo lastDamageInfo)
    {
        if (lastDamageInfo.attacker == null) return;

        PlayerKillTracker killTracker = 
            lastDamageInfo.attacker.GetComponentInParent<PlayerKillTracker>();

        if (killTracker != null)
        {
            killTracker.NotifyEnemyKilled(gameObject);
        }
    }

    //보상 제공 -> 플레이어 기력 수급
    private void GiveReward()
    {
        if (playerEnergy != null)
        {
            playerEnergy.GainEnergy(energyReward);
        }
    }

    //실제 사망한 시점에서 실행
    private void FinishDeath()
    {
        gameObject.SetActive(false);
    }

    //사망 모션 중에서 물리계산 제외
    private void DisablePhysicalPresence()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (enemyColliders == null) return;

        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
        }
    }

    //콜라이더 복귀
    private void RestoreColliders()
    {
        if (enemyColliders == null ||
            defaultColliderStates == null)
        {
            return;
        }

        for (int i = 0; i < enemyColliders.Length; i++)
        {
            if (enemyColliders[i] != null)
            {
                enemyColliders[i].enabled = defaultColliderStates[i];
            }
        }
    }
}