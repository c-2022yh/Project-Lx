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

    [Header("Audio")]
    public SkillSoundSettings audio = new SkillSoundSettings();

    [Header("Cost")]
    [Min(0f)] public float cooldownTime = 1f;
    [FormerlySerializedAs("soulCost")]
    [Min(0f), Tooltip("Energy spent once when casting starts. 0 is free; a positive cost requires an owned orb and enough energy.")]
    public float energyCost = 0f;

    // Multi-stage skills can price each activation without changing shared asset values.
    public virtual float GetEnergyCost(Player p) => energyCost;
    public virtual float GetCooldownAfterCast(Player p) => cooldownTime;

    public bool CanPayCost(Player p)
    {
        if (p == null) return false;
        float cost = GetEnergyCost(p);
        return cost <= 0f || (p.Energy != null && p.Energy.CanSpendEnergy(cost));
    }

    public bool TryPayCost(Player p)
    {
        if (p == null) return false;
        float cost = GetEnergyCost(p);
        if (cost <= 0f) return true;
        return p.Energy != null && p.Energy.TrySpendEnergy(cost);
    }

    [Header("Use Conditions")]
    [Tooltip("When disabled, casting requires the player to be grounded. Checked before energy payment or cooldown.")]
    public bool allowUseInAir = true;

    [Header("Movement")]
    [Tooltip("Stop existing horizontal velocity when a normal skill starts. Keeps vertical velocity and held movement input. Does not affect the movable awakening Q cast.")]
    public bool stopHorizontalMovementOnStart = true;

    public void ApplyStartMovement(Player p)
    {
        if (!stopHorizontalMovementOnStart || p == null || p.rb == null) return;
        p.rb.linearVelocity = new Vector2(0f, p.rb.linearVelocity.y);
    }

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
        SkillAudioSession session = p.Skill != null ? p.Skill.GetAudioSession(this) : null;
        session?.BeginCast();
        yield return WaitForWindup(p);
        if (p == null) yield break;
        session?.EndWindup();
        yield return effect;
        session?.EndActive();
        if (p == null) yield break;
        yield return WaitForRecovery(p);
    }

    // Playback state stays on the player; SOs only supply cue settings.
    protected void BeginEffectAudio(Player p)
    {
        if (p != null && p.Skill != null) p.Skill.GetAudioSession(this)?.BeginActive();
    }

    protected void PlayReleaseSound(Player p, Vector3 position)
    {
        if (p != null && p.Skill != null) p.Skill.GetAudioSession(this)?.Release(position);
    }

    protected DamageInfo AttachImpactAudio(Player p, DamageInfo damage)
    {
        SkillAudioSession session = p != null && p.Skill != null ? p.Skill.GetAudioSession(this) : null;
        damage.impactAudio = session != null ? session.Impact
            : new AttackAudioContext(audio?.impactSound, audio != null ? audio.impactMode : ImpactSoundMode.EveryHit, p != null ? p.transform : null);
        return damage;
    }

    // Called before a replacement action starts, so a cancelled cast cannot retain its physics freeze.
    public virtual void OnCastCancelled(Player p)
    {
        if (p != null) p.SetPhysicsFreeze(false);
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
    public virtual bool CanUse(Player p) => p != null && (allowUseInAir || p.isGrounded) && CanPayCost(p);
    
    //스킬 실행하는 가상 함수
    public abstract IEnumerator ProcessSkill(Player p);
}