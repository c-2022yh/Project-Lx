using System.Collections;
using UnityEngine;

//DamageInfo로 들어온 피해가 이 피격자에게 실제로 몇 들어가는가?
public static class DamageCalculator
{
    public static float Calculate(DamageInfo info, float defense, float durability)
    {
        float finalDamage = info.damage;
        
        /*
        방어력, 관통력 적용 방식
        방어력 공식: 최종 피해량 = 기본 피해량 * (100 / (100 + 방어력)) 
        관통력 공식: 최종 방어력 = 방어력 - 관통력 (0보다 낮아질 수 없음)
        */

        //관통 적용
        float finalDefense = Mathf.Max(0f, defense - info.penetration);

        //방어력 적용
        finalDamage *= 100f / (100f + finalDefense);

        


        //피해 증폭, 내구력 계산 후 최종 피해량 반환
        //피해 증폭 10%, 내구력 20%
        //1 + 0.1 - 0.2 = 0.9배
        float finalMultiplier = Mathf.Max(0f, 1f + info.damageAmplification - durability);

        finalDamage *= finalMultiplier;

        return finalDamage;
    }
}