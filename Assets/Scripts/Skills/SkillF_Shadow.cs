using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillF_Shadow", menuName = "SkillF_Shadow")]

public class SkillF_Shadow : SkillData
{
    public override IEnumerator ProcessSkill(
       Player p, 
        SkillRangeIndicator indicator, 
        GameObject weaponHandle, 
        Collider2D swordCollider, 
        float defaultAngle)
    {
        // 실제 로직은 PlayerSkill.cs에서 처리하므로 여기선 할 게 없습니다.
        yield break; 
    }
}
