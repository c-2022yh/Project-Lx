using UnityEngine;

//모든 적의 공통 공격 처리를 담당
public class EnemyContactAttack : MonoBehaviour
{
    private EnemyStats enemyStats;

    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
    }

    //기본 접촉 공격
    public void ContactAttack(IDamageable target, Vector2 hitDirection)
    {
        if (target == null || enemyStats == null) return;

        OffensiveStats offense = enemyStats.Offense;

        //접촉 공격은 기본 물리 공격력을 그대로 사용
        DamageInfo damageInfo = new DamageInfo(
            offense.physicalAttack,
            DamageType.Physical,
            offense.physicalPenetration,
            false,
            offense.damageAmplification,
            gameObject
        );

        target.TakeDamage(damageInfo, hitDirection);
    }
}