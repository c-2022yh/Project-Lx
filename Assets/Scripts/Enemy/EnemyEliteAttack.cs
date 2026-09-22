using System;
using System.Collections;
using UnityEngine;

public class EnemyEliteAttack : MonoBehaviour
{
    [Header("Melee Attack")]
    [SerializeField] private AttackDamageSpec meleeDamageSpec;

    [SerializeField] private float meleeStartupTime = 0.3f;
    [SerializeField] private float meleeActiveTime = 0.15f;
    [SerializeField] private float meleeRecoveryTime = 0.5f;

    [SerializeField] private float meleeCooldown = 1.5f;

    [Header("Melee Hitbox")]
    [SerializeField] private Collider2D meleeHitbox;

    private EnemyStats enemyStats;

    private float nextMeleeAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();

        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = false;
        }
    }

    //근거리 공격 가능
    public bool CanUseMeleeAttack()
    {
        if (isAttacking) return false;
        if (Time.time < nextMeleeAttackTime) return false;

        return true;
    }

    //근거리 공격 실행
    public void StartMeleeAttack(Action onFinished)
    {
        if (!CanUseMeleeAttack()) return;

        StartCoroutine(MeleeAttackRoutine(onFinished));
    }

    //근거리 공격 코루틴
    private IEnumerator MeleeAttackRoutine(Action onFinished)
    {
        isAttacking = true;

        //선딜
        yield return new WaitForSeconds(meleeStartupTime);

        //공격 판정 시작
        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = true;
        }

        yield return new WaitForSeconds(meleeActiveTime);

        //공격 판정 종료
        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = false;
        }

        //후딜
        yield return new WaitForSeconds(meleeRecoveryTime);

        //공격 종료
        nextMeleeAttackTime = Time.time + meleeCooldown;

        isAttacking = false;

        onFinished?.Invoke();
    }


    //돌진은 다음에 구현
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


    private void OnDisable()
    {
        StopAllCoroutines();

        if (meleeHitbox != null)
        {
            meleeHitbox.enabled = false;
        }

        isAttacking = false;
    }
}