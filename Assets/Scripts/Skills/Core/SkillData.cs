using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

//프로젝트에서 스킬데이터 SO를 만들수 있도록 설정
public enum SkillAnimationType
{
    None = 0,
    Thrust = 1,
    TripleSlash = 2,
    FireBall = 3,
    Guard = 4,
    ShadowSwap = 5,
    VitalConversion = 6,
    PiercingDash = 7,
    ScorchedEarth = 8
}

public abstract class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public Sprite icon;

    [TextArea(2, 4)]
    public string description;


    [Header("Animation")]
    [Tooltip("Motion played when this skill is used. None skips skill animation. Keep existing enum values unchanged when extending.")]
    public SkillAnimationType animationType = SkillAnimationType.None;

    [Header("Cost")]
    public float cooldownTime = 1f;
    public int soulCost = 0;

    [Header("Timing")]
    [FormerlySerializedAs("startupTime"), FormerlySerializedAs("windup")]
    [Min(0f), Tooltip("Delay in seconds from animation start until the skill effect begins.")]
    public float windupTime = 0f;

    [Tooltip("Duration used by the skill effect itself. Windup and recovery are added separately.")]
    public float activeTime = 0.2f;

    [FormerlySerializedAs("recovery")]
    [Min(0f), Tooltip("Delay in seconds after the skill effect finishes. The skill state is kept until this ends.")]
    public float recoveryTime = 0f;

    // Shared timing for both normal skills and prepared awakening skills.
    public IEnumerator ExecuteWithTiming(Player p, IEnumerator effect)
    {
        if (p == null) yield break;
        yield return WaitForWindup(p);
        if (p == null) yield break;
        yield return effect;
        if (p == null) yield break;
        yield return WaitForRecovery(p);
    }

    protected virtual IEnumerator WaitForWindup(Player p)
    {
        if (windupTime > 0f) yield return new WaitForSeconds(windupTime);
    }

    protected virtual IEnumerator WaitForRecovery(Player p)
    {
        if (recoveryTime > 0f) yield return new WaitForSeconds(recoveryTime);
    }

    //현재 상태에서 스킬을 사용할 수 있는지 확인
    public virtual bool CanUse(Player p) => p != null;
    
    //스킬 실행하는 가상 함수
    public abstract IEnumerator ProcessSkill(Player p);
}