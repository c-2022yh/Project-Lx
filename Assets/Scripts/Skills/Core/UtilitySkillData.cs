using UnityEngine;

public abstract class UtilitySkillData : SkillData
{
    [Header("Utility")]
    public bool lockInputDuringSkill = true;
    public bool invincibleDuringSkill = false;
}