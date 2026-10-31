using System;
using UnityEngine;

// Shared by the visible warning and damage query. Side is screen/world left (-1),
// right (+1), or the central two-hand attack (0), regardless of the boss position.
public static class GolemBossArenaGeometry
{
    public static Rect GroundAttack(float left, float right, float floor, float height,
        int side, float centerCoverage)
    {
        float width = Math.Max(0.1f, right - left);
        float attackLeft;
        float attackWidth;
        if (side != 0)
        {
            attackWidth = width * 0.5f;
            attackLeft = side < 0 ? left : left + attackWidth;
        }
        else
        {
            attackWidth = width * Math.Max(0.1f, Math.Min(1f, centerCoverage));
            attackLeft = left + (width - attackWidth) * 0.5f;
        }
        return new Rect(attackLeft, floor, attackWidth, Math.Max(0.05f, height));
    }
}
