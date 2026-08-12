using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float decisionTime = 1f;

    [Header("Animation")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float walkFrameTime = 0.12f;

    [SerializeField] private Sprite[] deathSprites;
    [SerializeField] private float deathFrameTime = 0.12f;
    [SerializeField] private float deathEndHoldTime = 0.08f;

    [Header("Health")]
    public float maxHp = 3f;
    private float currentHp;

    [Header("Health UI")]
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Damage Popup")]
    [SerializeField] private DamagePopup damagePopupPrefab;

    [SerializeField]
    private Vector3 damagePopupOffset =
        new Vector3(0f, 1.4f, 0f);

    [Header("Hit Effect")]
    [SerializeField] private EnemyHitEffect hitEffectPrefab;

    [SerializeField]
    private Vector3 hitEffectOffset =
        new Vector3(0f, 0.3f, 0f);

    [Header("Hit Feedback")]
    [SerializeField] private float hitStunTime = 0.12f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackUpForce = 1f;

    [SerializeField, Min(1)]
    private int hitBlinkCount = 2;

    [SerializeField, Min(0.01f)]
    private float hitBlinkInterval = 0.03f;

    [Header("Reward")]
    public PlayerEnergy playerEnergy;
    public float energyReward = 20f;

    private EnemyStats enemyStats;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Collider2D[] enemyColliders;
    private bool[] defaultColliderStates;

    private int direction = 1;

    private bool isDead;
    private bool isHitStunned;

    private Coroutine decisionCoroutine;
    private Coroutine walkAnimationCoroutine;
    private Coroutine hitFeedbackCoroutine;
    private Coroutine deathCoroutine;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        enemyStats = GetComponent<EnemyStats>();

        //사망 시 충돌을 제거하고,
        //다시 소환될 때 원래 상태로 돌리기 위해 저장
        enemyColliders =
            GetComponentsInChildren<Collider2D>(true);

        defaultColliderStates =
            new bool[enemyColliders.Length];

        for (int i = 0; i < enemyColliders.Length; i++)
        {
            defaultColliderStates[i] =
                enemyColliders[i].enabled;
        }
    }


    //오브젝트 풀링에서 다시 소환될 때마다 호출
    private void OnEnable()
    {
        isDead = false;
        isHitStunned = false;
        direction = 1;

        currentHp = maxHp;

        //스프라이트 초기화
        if (sr != null)
        {
            sr.enabled = true;
            sr.color = Color.white;
            sr.flipX = false;

            if (walkSprites != null &&
                walkSprites.Length > 0)
            {
                sr.sprite = walkSprites[0];
            }
        }

        //물리 판정 복구
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        RestoreColliders();

        if (playerEnergy == null)
        {
            playerEnergy =
                FindAnyObjectByType<PlayerEnergy>();
        }

        //체력바 초기화 후 숨김
        if (healthBar != null)
        {
            healthBar.Initialize(
                currentHp,
                maxHp
            );
        }

        decisionCoroutine =
            StartCoroutine(DecideAction());

        walkAnimationCoroutine =
            StartCoroutine(WalkAnimationRoutine());
    }


    private void OnDisable()
    {
        decisionCoroutine = null;
        walkAnimationCoroutine = null;
        hitFeedbackCoroutine = null;
        deathCoroutine = null;
    }


    //이동 방향 랜덤 결정
    private IEnumerator DecideAction()
    {
        while (!isDead)
        {
            direction =
                UnityEngine.Random.Range(-1, 2);

            if (direction != 0 && sr != null)
            {
                sr.flipX = direction > 0;
            }

            yield return new WaitForSeconds(
                decisionTime
            );
        }
    }


    //1~4번 걷기 스프라이트 반복
    private IEnumerator WalkAnimationRoutine()
    {
        if (walkSprites == null ||
            walkSprites.Length == 0)
        {
            yield break;
        }

        int frameIndex = 0;

        while (!isDead)
        {
            if (sr == null)
                yield break;

            //실제로 이동 중일 때만 걷기 프레임 진행
            if (direction != 0 && !isHitStunned)
            {
                sr.sprite =
                    walkSprites[frameIndex];

                frameIndex++;

                if (frameIndex >= walkSprites.Length)
                {
                    frameIndex = 0;
                }
            }
            else if (direction == 0)
            {
                //정지 중에는 첫 번째 프레임 유지
                sr.sprite = walkSprites[0];
                frameIndex = 0;
            }

            yield return new WaitForSeconds(
                Mathf.Max(
                    0.01f,
                    walkFrameTime
                )
            );
        }
    }


    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (isHitStunned)
            return;

        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(
            direction * moveSpeed,
            rb.linearVelocity.y
        );
    }


    //적이 피해를 받음
    public void TakeDamage(
        DamageInfo damageInfo,
        Vector2 hitDirection
    )
    {
        if (isDead)
            return;

        float defense =
            damageInfo.damageType switch
            {
                DamageType.Physical =>
                    enemyStats.Defense.physicalDefense,

                DamageType.Magical =>
                    enemyStats.Defense.magicalDefense,

                _ => 0f
            };

        float finalDamage =
            DamageCalculator.Calculate(
                damageInfo,
                defense,
                enemyStats.Defense.durability
            );

        currentHp = Mathf.Max(
            0f,
            currentHp - finalDamage
        );

        Debug.Log(
            $"Enemy Hit! Damage: {finalDamage:F2}, " +
            $"HP: {currentHp:F2}"
        );

        //체력바
        if (healthBar != null)
        {
            healthBar.ShowHealth(
                currentHp,
                maxHp
            );
        }

        //데미지 숫자
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

        //피격 이펙트
        if (hitEffectPrefab != null)
        {
            Instantiate(
                hitEffectPrefab,
                transform.position +
                hitEffectOffset,
                Quaternion.identity
            );
        }

        //사망 판정
        if (currentHp <= 0f)
        {
            Die(damageInfo);
            return;
        }

        //기존 피격 연출 중단 후 재시작
        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(
                hitFeedbackCoroutine
            );
        }

        hitFeedbackCoroutine =
            StartCoroutine(
                HitFeedbackRoutine(
                    hitDirection
                )
            );
    }


    //피격 넉백 + 깜빡임
    private IEnumerator HitFeedbackRoutine(
        Vector2 hitDirection
    )
    {
        isHitStunned = true;

        //넉백
        if (rb != null)
        {
            float hitDir =
                hitDirection.x >= 0f
                    ? 1f
                    : -1f;

            rb.linearVelocity = new Vector2(
                hitDir * knockbackForce,
                knockbackUpForce
            );
        }

        float blinkDuration = 0f;

        //색 변경 대신 스프라이트를 껐다 켬
        for (int i = 0; i < hitBlinkCount; i++)
        {
            if (sr == null)
                break;

            sr.enabled = false;

            yield return new WaitForSeconds(
                hitBlinkInterval
            );

            sr.enabled = true;

            yield return new WaitForSeconds(
                hitBlinkInterval
            );

            blinkDuration +=
                hitBlinkInterval * 2f;
        }

        //깜빡임 시간보다 피격 경직이 더 길 경우
        float remainingStun =
            hitStunTime - blinkDuration;

        if (remainingStun > 0f)
        {
            yield return new WaitForSeconds(
                remainingStun
            );
        }

        if (sr != null)
        {
            sr.enabled = true;
        }

        isHitStunned = false;
        hitFeedbackCoroutine = null;
    }


    //사망 처리
    private void Die(
        DamageInfo lastDamageInfo
    )
    {
        if (isDead)
            return;

        isDead = true;

        //처치 알림
        if (lastDamageInfo.attacker != null)
        {
            PlayerKillTracker killTracker =
                lastDamageInfo.attacker
                    .GetComponentInParent<
                        PlayerKillTracker>();

            if (killTracker != null)
            {
                killTracker.NotifyEnemyKilled(
                    gameObject
                );
            }
        }

        //기력 보상
        if (playerEnergy != null)
        {
            playerEnergy.GainEnergy(
                energyReward
            );
        }

        //이동과 걷기 애니메이션 중단
        if (decisionCoroutine != null)
        {
            StopCoroutine(decisionCoroutine);
            decisionCoroutine = null;
        }

        if (walkAnimationCoroutine != null)
        {
            StopCoroutine(walkAnimationCoroutine);
            walkAnimationCoroutine = null;
        }

        //피격 깜빡임 중이었다면 중단
        if (hitFeedbackCoroutine != null)
        {
            StopCoroutine(hitFeedbackCoroutine);
            hitFeedbackCoroutine = null;
        }

        if (sr != null)
        {
            sr.enabled = true;
        }

        //체력바 즉시 숨기기
        if (healthBar != null)
        {
            healthBar.Initialize(
                0f,
                maxHp
            );
        }

        //물리적으로는 즉시 사라진 상태로 만듦
        DisablePhysicalPresence();

        //스프라이트만 남겨 죽음 애니메이션 재생
        deathCoroutine =
            StartCoroutine(
                DeathAnimationRoutine()
            );
    }


    //5~8번 죽음 스프라이트 순차 재생
    private IEnumerator DeathAnimationRoutine()
    {
        if (deathSprites == null ||
            deathSprites.Length == 0)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float frameTime =
            Mathf.Max(
                0.01f,
                deathFrameTime
            );

        foreach (Sprite deathSprite in deathSprites)
        {
            if (sr == null)
                break;

            sr.sprite = deathSprite;

            yield return new WaitForSeconds(
                frameTime
            );
        }

        //마지막 프레임 잠깐 유지
        if (deathEndHoldTime > 0f)
        {
            yield return new WaitForSeconds(
                deathEndHoldTime
            );
        }

        //애니메이션이 끝난 뒤 풀로 반환
        gameObject.SetActive(false);
    }


    //충돌과 물리 판정을 즉시 제거
    private void DisablePhysicalPresence()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (enemyColliders == null)
            return;

        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
        }
    }


    //다시 소환될 때 충돌 상태 복구
    private void RestoreColliders()
    {
        if (enemyColliders == null ||
            defaultColliderStates == null)
        {
            return;
        }

        for (int i = 0;
             i < enemyColliders.Length;
             i++)
        {
            if (enemyColliders[i] != null)
            {
                enemyColliders[i].enabled =
                    defaultColliderStates[i];
            }
        }
    }
}