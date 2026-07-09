using System;
using UnityEngine;

//이번 공격이 어떤 피해를 들고 왔는가? 에 대한 정보 구조체
public struct DamageInfo
{
    public float damage;            //기본 피해량
    public DamageType damageType;   //데미지 타입

    public float penetration;       //관통력, 방어력 무시 정도

    public bool isCritical;         //치명타 여부

    public float damageAmplification; //피해 증폭

    public GameObject attacker;     //공격자 정보
    
    //생성자
    public DamageInfo(float damage, DamageType damageType, 
        float penetration, bool isCritical, 
        float damageAmplification, GameObject attacker)
    {
        this.damage = damage;
        this.damageType = damageType;

        this.penetration = penetration;

        this.isCritical = isCritical;

        this.damageAmplification = damageAmplification;

        this.attacker = attacker;
    }


    //공격자의 OffensiveStats를 기반으로 이번 공격의 DamageInfo 생성
    //매개변수 : 공격자의 스탯, 공격의 추가보정값, 공격자
    public static DamageInfo Create(
        OffensiveStats offense,
        AttackDamageSpec attack,
        GameObject attacker)
    {
        float damage;
        float penetration;

        //물리/마법 피해 설정
        switch (attack.damageType)
        {
            case DamageType.Physical: //물리 피해
                damage = offense.physicalAttack * attack.damageMultiplier;
                penetration = offense.physicalPenetration;
                break;


            case DamageType.Magical: //마법 피해
                damage = offense.magicalAttack * attack.damageMultiplier;
                penetration = offense.magicalPenetration;
                break;

            default:
                damage = 0f;
                penetration = 0f;
                break;
        }

        //이번 공격에만 추가 관통력 적용
        penetration += attack.bonusPenetration;



        //치명타 판정
        bool isCritical = attack.canCritical && UnityEngine.Random.value <= offense.criticalChance;
        if (isCritical) damage *= offense.criticalMultiplier;
        

        //기본 피증 + 이번 공격 전용 피증
        float damageAmplification = offense.damageAmplification + attack.bonusDamageAmplification;


        //반환값
        return new DamageInfo(damage, attack.damageType, penetration,
            isCritical, damageAmplification, attacker);
        
    }
}