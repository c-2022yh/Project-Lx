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

    private bool isAttacking;
    private float nextMeleeAttackTime;

    public bool IsAttacking => isAttacking;


    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        ai = GetComponent<EnemyAI>();
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


    // =========================
    // 돌진 공격
    // 나중에 구현

    public bool CanUseChargeAttack()
    {
        return false;
    }


    public void StartChargeAttack(
        int direction,
        Action onFinished)
    {
        onFinished?.Invoke();
    }


    //비활성화 시 초기화

    private void OnDisable()
    {
        StopAllCoroutines();

        isAttacking = false;
    }
}