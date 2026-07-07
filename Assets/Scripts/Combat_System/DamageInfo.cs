using UnityEngine;

//이번 공격이 어떤 피해를 주는지에 대한 정보 구조체
public struct DamageInfo
{
    public float damage;            //기본 피해량
    public DamageType damageType;   //데미지 타입

    public float penetration;       //관통력, 방어력 무시 정도
    public bool canCritical;        //치명타 가능 여부
    public float criticalChance;    //치명타 확률
    public float criticalMultiplier;//치명타 배율

    public GameObject attacker;     //공격자 정보

    //생성자
    public DamageInfo(float damage, DamageType damageType, GameObject attacker = null)
    {
        this.damage = damage;
        this.damageType = damageType;

        penetration = 0f;

        canCritical = false;
        criticalChance = 0f;
        criticalMultiplier = 1.5f;

        this.attacker = attacker;
    }
}