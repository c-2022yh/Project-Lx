using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_Shadow",
    menuName = "Relics/Effects/Shadow"
)]

public class ShadowRelicEffect : SkillGrantRelicEffectBase
{
    [Header("Shadow Skill")]
    [Tooltip("그림자 유물이 지급할 스킬")]
    [SerializeField]
    private SkillData shadowSkill;

    protected override SkillData Skill => shadowSkill;
    protected override string LogTag => "Shadow";
}
