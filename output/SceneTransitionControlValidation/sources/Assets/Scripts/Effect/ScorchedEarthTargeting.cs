using System.Collections.Generic;
using UnityEngine;

public static class ScorchedEarthTargeting
{
    public static bool IsValid(Enemy enemy)
    {
        return enemy != null && enemy.isActiveAndEnabled
            && enemy.Health != null && !enemy.IsDead;
    }

    public static Vector2 AimPoint(Enemy enemy)
    {
        return enemy.Sr != null ? (Vector2)enemy.Sr.bounds.center : (Vector2)enemy.transform.position;
    }

    // Deduplicate child colliders before distributing shots. Distance order also
    // makes invalid-target reacquisition choose the nearest visible enemy.
    public static List<Enemy> FindTargets(Vector2 origin, float radius,
        LayerMask enemyLayer, LayerMask obstacles)
    {
        var enemies = new List<Enemy>();
        var seen = new HashSet<Enemy>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, Mathf.Max(0f, radius), enemyLayer))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (!IsValid(enemy) || !seen.Add(enemy)) continue;
            Vector2 aim = AimPoint(enemy);
            if (obstacles.value != 0 && Physics2D.Linecast(origin, aim, obstacles).collider != null) continue;
            enemies.Add(enemy);
        }
        enemies.Sort((a, b) => (AimPoint(a) - origin).sqrMagnitude
            .CompareTo((AimPoint(b) - origin).sqrMagnitude));
        return enemies;
    }
}
