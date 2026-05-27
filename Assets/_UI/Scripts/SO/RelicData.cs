using UnityEngine;

[CreateAssetMenu(fileName = "New Relic", menuName = "Game Data/Relic")]
public class RelicData : ScriptableObject
{
    [Header("Identity")]
    public string relicId;
    public string relicName;
    public Sprite icon;
    [TextArea(3, 6)]
    public string description;

    [Header("Linked Skill")]
    public SkillUIData linkedSkill;   // ← 여기 SkillData → SkillUIData 로 변경!
}