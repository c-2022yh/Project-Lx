using UnityEngine;
using System.Collections.Generic;
public abstract class AttackSkillData : SkillData
{
    [Header("Attack")]
    public LayerMask enemyLayer; //적을 판정할 레이어 
    
    //모든 공격 스킬이 공통으로 가지는 피해 설정
    [SerializeField] protected AttackDamageSpec damageSpec;

    //넉백 방향 설정
    public Vector2 knockbackDirection = new Vector2(1f, 0f);

    //현재 시전자의 스탯 + 스킬 고유 설정으로 DamageInfo 생성
    protected DamageInfo CreateDamageInfo(Player p)
    {
        return DamageInfo.Create(p.Stats.Offense, damageSpec, p.gameObject);
    }


    protected void TryDamageEnemy(Collider2D col, HashSet<Enemy> hitEnemies, float dir, DamageInfo damageInfo)
    {
        if (col == null) return;

        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy == null)
            enemy = col.GetComponentInParent<Enemy>();

        if (enemy == null) return;
        if (hitEnemies.Contains(enemy)) return;

        hitEnemies.Add(enemy);

        enemy.TakeDamage(damageInfo, new Vector2(dir, 0f));
    }
}