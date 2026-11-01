using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WolfAttackSettings
{
    [Min(0.01f)] public float startup = 0.4f;
    [Min(0.01f)] public float active = 0.15f;
    [Min(0.01f)] public float recovery = 0.5f;
    [Min(0f)] public float cooldown = 1.2f;
    [Tooltip("Total forward travel during active phase, in world units.")]
    [Min(0f)] public float travelDistance = 0.35f;
    [Tooltip("Local prefab units. Offset is mirrored with facing.")]
    public Vector2 hitboxOffset = new Vector2(1.6f, -0.6f);
    public Vector2 hitboxSize = new Vector2(0.9f, 0.8f);
    public AttackDamageSpec damage = new AttackDamageSpec { damageMultiplier = 2f };
    public SoundCue attackSound = new SoundCue();
    [Tooltip("Play the attack sound when windup starts instead of when the hit becomes active.")]
    public bool attackSoundOnPrepare;
    public SoundCue impactSound = new SoundCue();
    public SoundCue activeLoop = new SoundCue();
    public Sprite prepareSprite;
    public Sprite[] activeSprites;
    public Sprite recoverySprite;
    [Min(0.01f)] public float frameTime = 0.08f;
}

// Physics phases keep animation, movement and damage synchronized.
[RequireComponent(typeof(EnemyAI), typeof(EnemyHealth), typeof(EnemyStats))]
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyAnimation), typeof(EnemyKnockback))]
public class EnemyWolfAttack : MonoBehaviour
{
    private enum Phase { None, Prepare, Active, Recovery }
    [Header("Bite")]
    [SerializeField] private WolfAttackSettings bite = new WolfAttackSettings();
    [Header("Ground Charge")]
    [SerializeField] private WolfAttackSettings charge = new WolfAttackSettings
    {
        startup = 0.6f, active = 0.4f, recovery = 0.8f, cooldown = 2.5f,
        travelDistance = 4f, hitboxOffset = new Vector2(1f, -1f),
        hitboxSize = new Vector2(1.5f, 0.9f)
    };
    [Header("Layers")]
    [SerializeField] private LayerMask targetLayers = 1 << 7;
    [SerializeField] private LayerMask groundLayers = 1 << 6;
    [Header("Movement Safety (world units)")]
    [SerializeField, Min(0.01f)] private float skin = 0.04f;
    [SerializeField, Min(0.01f)] private float groundProbeDepth = 0.3f;
    [SerializeField, Min(0f)] private float groundedTolerance = 0.15f;

    private EnemyAI ai;
    private EnemyHealth health;
    private EnemyStats stats;
    private EnemyAnimation animationController;
    private EnemyKnockback knockback;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Phase phase;
    private WolfAttackSettings current;
    private bool charging;
    private int direction;
    private float phaseTime;
    private float activeElapsed;
    private float biteReadyTime;
    private float chargeReadyTime;
    private SoundHandle attackLoop;
    private DamageInfo damageInfo;
    private readonly HashSet<PlayerHealth> hitTargets = new HashSet<PlayerHealth>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[16];
    public bool IsAttacking => phase != Phase.None;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
        stats = GetComponent<EnemyStats>();
        animationController = GetComponent<EnemyAnimation>();
        knockback = GetComponent<EnemyKnockback>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        phase = Phase.None;
        biteReadyTime = chargeReadyTime = 0f;
        hitTargets.Clear();
    }

    private void OnDisable() => CancelAttack();

    public bool TryStart(bool useCharge)
    {
        if (!isActiveAndEnabled || health.IsDead || IsAttacking ||
            knockback.IsKnockbackActive || bodyCollider == null ||
            Time.time < (useCharge ? chargeReadyTime : biteReadyTime) || !IsGrounded()) return false;
        charging = useCharge;
        current = useCharge ? charge : bite;
        direction = ai.Direction;
        damageInfo = DamageInfo.Create(stats.Offense, current.damage, gameObject);
        damageInfo.impactAudio = new AttackAudioContext(current.impactSound, ImpactSoundMode.EveryHit, transform);
        hitTargets.Clear();
        knockback.StopKnockback();
        knockback.AttackImmune = true;
        EnterPhase(Phase.Prepare);
        return true;
    }

    public void Tick(float dt)
    {
        if (!IsAttacking) return;
        if (!isActiveAndEnabled || health.IsDead || !body.simulated) { CancelAttack(); return; }
        if (phase == Phase.Active)
        {
            // The previous step has now moved the body. Check its endpoint too.
            DealDamage();
            if (activeElapsed >= Mathf.Max(0.01f, current.active))
            {
                EnterPhase(Phase.Recovery);
                return;
            }
            Sprite[] frames = current.activeSprites;
            if (frames != null && frames.Length > 0)
                animationController.SetActionPose(frames[(int)(activeElapsed / Mathf.Max(0.01f, current.frameTime)) % frames.Length]);
            if (!MoveSafely(dt)) EnterPhase(Phase.Recovery);
            else activeElapsed += Mathf.Min(dt, Mathf.Max(0.01f, current.active) - activeElapsed);
            return;
        }
        ai.StopMovement();
        phaseTime += dt;
        if (phase == Phase.Prepare && phaseTime >= Mathf.Max(0.01f, current.startup))
            EnterPhase(Phase.Active);
        else if (phase == Phase.Recovery && phaseTime >= Mathf.Max(0.01f, current.recovery))
        {
            if (charging) chargeReadyTime = Time.time + Mathf.Max(0f, current.cooldown);
            else biteReadyTime = Time.time + Mathf.Max(0f, current.cooldown);
            CancelAttack();
        }
    }

    private void EnterPhase(Phase next)
    {
        attackLoop.Stop();
        if ((next == Phase.Prepare && current.attackSoundOnPrepare) ||
            (next == Phase.Active && !current.attackSoundOnPrepare))
            GameAudio.Play(current.attackSound, transform.position, transform);
        if (next == Phase.Active)
        {
            attackLoop = GameAudio.PlayLoop(current.activeLoop, transform);
        }
        phase = next;
        phaseTime = 0f;
        ai.StopMovement();
        if (next == Phase.Prepare) animationController.SetActionPose(current.prepareSprite);
        else if (next == Phase.Recovery) animationController.SetActionPose(current.recoverySprite);
        else
        {
            activeElapsed = 0f;
            if (current.activeSprites != null && current.activeSprites.Length > 0)
                animationController.SetActionPose(current.activeSprites[0]);
            DealDamage();
        }
    }

    private bool IsGrounded()
    {
        Bounds bounds = bodyCollider.bounds;
        return Physics2D.Raycast(new Vector2(bounds.center.x, bounds.min.y + skin),
            Vector2.down, skin + groundedTolerance, groundLayers).collider != null;
    }

    private bool MoveSafely(float dt)
    {
        float duration = Mathf.Max(0.01f, current.active);
        float speed = Mathf.Max(0f, current.travelDistance) / duration;
        float step = speed * Mathf.Min(dt, duration - activeElapsed);
        if (step <= 0f) { ai.StopMovement(); return true; }
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        int count = body.Cast(Vector2.right * direction, filter, castHits, step + skin);
        for (int i = 0; i < count; i++)
            if (castHits[i].normal.x * direction < -0.1f) return false;
        Bounds bounds = bodyCollider.bounds;
        Vector2 probe = new Vector2(bounds.center.x + direction * (bounds.extents.x + step + skin), bounds.min.y + skin);
        if (!Physics2D.Raycast(probe, Vector2.down, groundProbeDepth + skin, groundLayers)) return false;
        body.linearVelocity = new Vector2(direction * step / dt, body.linearVelocity.y);
        return true;
    }

    private Vector2 HitboxCenter(WolfAttackSettings settings, int facing)
    {
        return transform.TransformPoint(new Vector3(settings.hitboxOffset.x * facing, settings.hitboxOffset.y));
    }

    private Vector2 HitboxSize(WolfAttackSettings settings)
    {
        Vector3 scale = transform.lossyScale;
        return new Vector2(Mathf.Max(0.01f, Mathf.Abs(settings.hitboxSize.x * scale.x)),
            Mathf.Max(0.01f, Mathf.Abs(settings.hitboxSize.y * scale.y)));
    }

    private void DealDamage()
    {
        if (health.IsDead) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayers);
        filter.useTriggers = true;
        Physics2D.OverlapBox(HitboxCenter(current, direction), HitboxSize(current), 0f, filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            PlayerHealth target = other.GetComponentInParent<PlayerHealth>();
            if (target == null || target.IsDead || hitTargets.Contains(target)) continue;
            PlayerHitReaction reaction = target.GetComponent<PlayerHitReaction>();
            if (reaction != null && !reaction.CanTakeDamage()) continue;
            hitTargets.Add(target);
            target.TakeDamage(damageInfo, Vector2.right * direction);
        }
    }

    private void CancelAttack()
    {
        attackLoop.Stop();
        phase = Phase.None;
        hitTargets.Clear();
        if (knockback != null) knockback.AttackImmune = false;
        if (animationController != null) animationController.ReleaseActionPose();
        if (ai != null) ai.StopMovement();
    }

    private void OnDrawGizmosSelected()
    {
        int facing = Application.isPlaying && ai != null ? ai.Direction : 1;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(HitboxCenter(bite, facing), HitboxSize(bite));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(HitboxCenter(charge, facing), HitboxSize(charge));
    }
}
