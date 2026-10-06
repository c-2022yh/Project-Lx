using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class EnemyEliteAttackPattern
{
    public string attackName;

    [Header("Timing")]
    public float startupTime = 0.3f;    //선딜
    public float activeTime = 0.15f;    //공격 판정 시간
    public float recoveryTime = 0.5f;   //후딜

    [Header("Combat")]
    public AttackDamageSpec damageSpec;

    [Header("Hitbox")]
    public GameObject hitboxPrefab;
    public Vector2 hitboxOffset = new Vector2(0.8f, 0f);
    public Vector3 hitboxScale = Vector3.one;
    public float hitboxRotationZ = 0f;

    [Header("Cooldown")]
    public float cooldown = 1.5f;
}


//엘리트 몹의 공격 실행을 담당
public class EnemyEliteAttack : MonoBehaviour
{
    [Header("Melee Attack")]
    [SerializeField]
    private EnemyEliteAttackPattern meleePattern;

    private EnemyStats enemyStats;
    private EnemyAI ai;
    private Rigidbody2D rb;

    private bool isAttacking;
    private bool isCharging;
    private float nextMeleeAttackTime;
    private float nextChargeAttackTime;

    [Header("Charge Test")]
    [Tooltip("돌진 전에 멈춰 있는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeStartupTime = 0.3f;

    [Tooltip("돌진 속도입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeSpeed = 12f;

    [Tooltip("돌진이 유지되는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeDuration = 0.35f;

    [Tooltip("돌진 후 멈춰 있는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeRecoveryTime = 0.5f;

    [Tooltip("다음 돌진까지의 쿨타임입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeCooldown = 1.5f;

    public bool IsAttacking => isAttacking;
    public bool IsCharging => isCharging;


    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        ai = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody2D>();
    }

    //근거리 공격 가능 여부
    public bool CanUseMeleeAttack()
    {
        if (isAttacking) return false;
        if (meleePattern == null) return false;
        if (Time.time < nextMeleeAttackTime) return false;

        return true;
    }


    //근거리 공격 시작
    public void StartMeleeAttack(Action onFinished)
    {
        if (!CanUseMeleeAttack()) return;

        StartCoroutine(MeleeAttackRoutine(onFinished));
    }


    //근거리 공격 루틴
    private IEnumerator MeleeAttackRoutine(Action onFinished)
    {
        isAttacking = true;

        //공격 시작 순간 바라보는 방향 저장
        float dir = ai != null ? ai.Direction : 1f;

        //선딜
        yield return new WaitForSeconds(meleePattern.startupTime);
        
        //공격 판정 생성
        SpawnAttackHitbox(meleePattern, dir);

        //공격 판정 유지 시간
        yield return new WaitForSeconds(meleePattern.activeTime);

        //후딜
        yield return new WaitForSeconds(meleePattern.recoveryTime);

        //공격 종료
        nextMeleeAttackTime = Time.time + meleePattern.cooldown;

        isAttacking = false;

        onFinished?.Invoke();
    }


    //공격 히트박스 생성
    private void SpawnAttackHitbox(EnemyEliteAttackPattern pattern, float dir)
    {
        if (pattern == null) return;
        if (pattern.hitboxPrefab == null) return;
        if (enemyStats == null) return;

        //현재 적 위치 기준으로 바라보는 방향 앞쪽에 생성
        Vector3 spawnPosition = transform.position + new Vector3(pattern.hitboxOffset.x * dir, pattern.hitboxOffset.y, 0f);

        //방향에 맞게 회전
        Quaternion rotation = Quaternion.Euler(0f, 0f, pattern.hitboxRotationZ * dir);

        //공격 데미지 정보 생성
        DamageInfo damageInfo =
            DamageInfo.Create(
                enemyStats.Offense,
                pattern.damageSpec,
                gameObject
            );


        //공격 히트박스 생성
        GameObject hitboxObj =
            Instantiate(
                pattern.hitboxPrefab,
                spawnPosition,
                rotation
            );


        //방향에 맞게 크기 설정
        Vector3 scale = pattern.hitboxScale;

        scale.x = Mathf.Abs(scale.x) * dir;

        hitboxObj.transform.localScale = scale;


        //공격 히트박스 스크립트에 정보 전달
        EnemyAttackHitbox hitbox = hitboxObj.GetComponentInChildren<EnemyAttackHitbox>();

        if (hitbox == null)
        {
            Destroy(hitboxObj);
            return;
        }
        hitbox.Init(damageInfo,new Vector2(dir, 0f), pattern.activeTime);

    }


    // Charge attack check
    public bool CanUseChargeAttack()
    {
        if (isAttacking) return false;
        if (Time.time < nextChargeAttackTime) return false;

        return true;
    }


    // Charge attack
    public void StartChargeAttack(
        int direction,
        Action onFinished)
    {
        if (!CanUseChargeAttack()) return;

        StartCoroutine(
            ChargeAttackRoutine(
                direction,
                onFinished
            )
        );
    }


    // Charge attack routine
    private IEnumerator ChargeAttackRoutine(
        int direction,
        Action onFinished)
    {
        isAttacking = true;
        isCharging = false;

        float chargeDirection =
            direction >= 0 ? 1f : -1f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(chargeStartupTime);

        isCharging = true;

        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(
                    chargeDirection * chargeSpeed,
                    rb.linearVelocity.y
                );
        }

        yield return new WaitForSeconds(chargeDuration);

        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
        }

        isCharging = false;

        yield return new WaitForSeconds(chargeRecoveryTime);

        nextChargeAttackTime =
            Time.time + chargeCooldown;

        isAttacking = false;

        onFinished?.Invoke();
    }


    private void OnDisable()
    {
        StopAllCoroutines();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isCharging = false;
        isAttacking = false;
    }
}