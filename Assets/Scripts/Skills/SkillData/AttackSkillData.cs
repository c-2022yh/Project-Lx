using UnityEngine;
using System.Collections.Generic;

public abstract class AttackSkillData : SkillData
{
    [Header("Attack")]
    public LayerMask enemyLayer;
    public float damageMultiplier = 1.0f;
    public Vector2 knockbackDirection = new Vector2(1f, 0f);

    protected void TryDamageEnemy(Collider2D col, HashSet<Enemy> hitEnemies, float dir)
    {
        if (col == null) return;

        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy == null)
            enemy = col.GetComponentInParent<Enemy>();

        if (enemy == null) return;
        if (hitEnemies.Contains(enemy)) return;

        hitEnemies.Add(enemy);

        enemy.TakeDamage(damageMultiplier, new Vector2(dir, 0f));
    }
}