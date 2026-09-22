
//보스나 엘리트 몹의 패턴 변수 정리
[System.Serializable]
public class EnemyAttackPattern
{
    //패턴 이름
    public string patternName;

    //사거리
    public float minRange;
    public float maxRange;

    //쿨타임
    public float cooldown;

    //우선도
    public int priority;

    //선딜, 후딜
    public float startupTime;
    public float recoveryTime;

    //데미지 배율
    public AttackDamageSpec damageSpec;
}