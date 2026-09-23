using System.Collections.Generic;
using UnityEngine;

//적의 공격 패턴 선택 스크립트
public class EnemyAttackSelector : MonoBehaviour
{
    public bool CanUse(EnemyAttackPattern pattern, float distance)
    {
        if (pattern == null) return false;

        if (distance < pattern.minRange) return false;

        if (distance > pattern.maxRange) return false;

        //if (Time.time < pattern.nextAvailableTime)  return false;

        return true;
    }

    public EnemyAttackPattern Select(List<EnemyAttackPattern> patterns, float distance)
    {
        EnemyAttackPattern selected = null;

        foreach (EnemyAttackPattern pattern in patterns)
        {
            if (!CanUse(pattern, distance)) continue;

            if (selected == null || pattern.priority > selected.priority)
            {
                selected = pattern;
            }
        }

        return selected;
    }
}