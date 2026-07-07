using System.Collections;
using UnityEngine;

//데미지 계산기 , 방어력, 관통, 치명타 계산 담당
public static class DamageCalculator
{
    public static float Calculate(DamageInfo info, float defense)
    {
        float finalDamage = info.damage;

        //관통 적용
        float finalDefense = Mathf.Max(0f, defense - info.penetration);

        //방어력 적용
        finalDamage *= 100f / (100f + finalDefense);

        /*
        방어력, 관통력 적용 방식
        방어력 공식: 최종 피해량 = 기본 피해량 * (100 / (100 + 방어력)) 
        관통력 공식: 최종 방어력 = 방어력 - 관통력 (0보다 낮아질 수 없음)
        */

        //치명타 적용 계산식
        if (info.canCritical && Random.value <= info.criticalChance)
        {
            finalDamage *= info.criticalMultiplier;
        }

        return finalDamage;
    }
}