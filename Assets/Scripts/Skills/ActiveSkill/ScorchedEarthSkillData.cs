using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SKL_ScorchedEarth", menuName = "Skills/Awakening/Scorched Earth")]

//폭주 유물이 제공하는 각성 전용 광역 공격 스킬
public class ScorchedEarthSkillData : SkillData
{
    [Header("Enemy Detection")]
    public LayerMask enemyLayer;

    [System.Serializable]
    public sealed class BarrageSettings
    {
        [Header("Targeting")]
        [Min(0.1f)] public float targetSearchRadius = 12f;
        [Min(0.1f)] public float retargetSearchRadius = 8f;
        public LayerMask sightObstacleLayer;
        public bool requireInitialTarget = true;
        public bool passThroughTerrain = true;

        [Header("Formation and Launch")]
        [Min(1)] public int projectileCount = 12;
        public Vector2 formationOffset = new Vector2(-0.4f, 0.6f);
        [Min(0f)] public float formationRadius = 1.2f;
        [Range(0f, 360f)] public float formationArc = 150f;
        [Min(0f)] public float preparationDuration = 0.15f;
        [Tooltip("Seconds between projectile launches. 0 launches the volley simultaneously.")]
        [Min(0f)] public float launchInterval = 0.025f;

        [Header("Flight")]
        [Min(0f)] public float outwardDuration = 0.04f;
        [Min(0.1f)] public float projectileSpeed = 30f;
        [Tooltip("Maximum homing turn in degrees per second.")]
        [Min(0f)] public float turnSpeed = 2160f;
        [Min(0.1f)] public float projectileLifetime = 2f;
        [Min(0.01f)] public float hitRadius = 0.15f;

        [Header("Damage per Projectile")]
        public AttackDamageSpec regularDamage = new AttackDamageSpec
        {
            damageType = DamageType.Magical,
            damageMultiplier = 4.25f / 12f
        };

        [Header("Visual Prefabs")]
        public GameObject preparationEffectPrefab;
        public GameObject regularProjectilePrefab;
        public GameObject regularHitEffectPrefab;
        [Header("Projectile Audio")]
        public SoundCue flightLoop = new SoundCue();
        [Min(0f)] public float hitEffectLifetime = 1f;
    }

    [Header("Barrage Configuration")]
    [SerializeField] private BarrageSettings barrage = new BarrageSettings();
    public BarrageSettings Barrage => barrage;

    public struct PreparedDamage
    {
        public DamageInfo regular;
    }

    //각성 상태와 스킬 시전 가능 상태를 함께 확인한다.
    public override bool CanUse(Player p)
    {
        return base.CanUse(p) && p.Awakening != null && p.Awakening.IsAwakened
            && p.ActionState != null && p.ActionState.CanSkill()
            && barrage != null && barrage.regularProjectilePrefab != null
            && (!barrage.requireInitialTarget || ScorchedEarthTargeting.FindTargets(
                p.transform.position, barrage.targetSearchRadius, enemyLayer,
                barrage.sightObstacleLayer).Count > 0);
    }

    //버프가 해제되기 전에 기존 공격 스탯과 AttackDamageSpec으로 피해 정보를 만든다.
    public PreparedDamage PrepareDamage(Player p)
    {
        DamageInfo regular = DamageInfo.Create(p.Stats.Offense, barrage.regularDamage, p.gameObject);
        // Small repeated hits must not push the target out of the barrage.
        regular.suppressKnockback = true;
        return new PreparedDamage
        {
            regular = regular
        };
    }

    //일반 스킬 호출 경로에서도 기존 SkillData 계약을 충족한다.
    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        PreparedDamage preparedDamage = PrepareDamage(p);
        yield return ProcessPreparedSkill(p, preparedDamage);
    }

    public ScorchedEarthBarrage CreateBarrage(Player p, PreparedDamage damage)
    {
        var runner = new GameObject("ScorchedEarth_Barrage").AddComponent<ScorchedEarthBarrage>();
        damage.regular = AttachImpactAudio(p, damage.regular);
        runner.Initialize(p, barrage, enemyLayer, damage,
            p.Skill != null ? p.Skill.GetAudioSession(this) : null, activeTime);
        return runner;
    }

    public IEnumerator ProcessPreparedSkill(Player p, PreparedDamage damage)
    {
        if (p == null) yield break;
        ScorchedEarthBarrage runner = CreateBarrage(p, damage);
        try { yield return runner.Process(); }
        finally { if (runner != null) runner.Cancel(); }
    }

    public override void OnCastCancelled(Player p)
    {
        base.OnCastCancelled(p);
        if (p != null) p.Awakening?.EndAwakening();
    }
}
