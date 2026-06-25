using UnityEngine;

public enum RelicCategory
{
    Sword,  // 검 유물
    Orb,    // 보주 유물
    Body    // 신체 유물
}


/*public enum RelicRarity //레어도가 필요할까
{
    Common,
    Rare,
    Epic,
    Legendary
}
*/


public enum RelicEffectType
{
    IncreaseBasicAttackDamage,
    IncreaseSkillDamage,
    IncreaseMaxSoul,
    IncreaseSoulGain,
    DisableSoulGain,
    IncreaseAwakeningDuration,
    HealOnKill,
    ExtraJump,
    IncreaseDashDistance,
    IncreaseGuardWindow
}

[CreateAssetMenu(
    fileName = "RLC_NewRelic",
    menuName = "Relics/Relic Data"
)]
public class RelicData : ScriptableObject
{
    [Header("Display")]
    public string relicName;
    public Sprite icon;

    [TextArea(2, 5)]
    public string description;

    [Header("Info")]
    public RelicCategory category;
    //public RelicRarity rarity;

    [Header("Effect")]
    public RelicEffectType effectType;
    public float value = 1f;
}