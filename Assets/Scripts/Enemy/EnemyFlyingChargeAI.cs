using UnityEngine;

// Hovers, notices the player, then commits to the position seen on first detection.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyAI), typeof(EnemyHealth))]
[RequireComponent(typeof(Enemy), typeof(EnemyStats), typeof(EnemyContactAttack))]
public class EnemyFlyingChargeAI : MonoBehaviour
{
    private enum State { Hover, Detecting, Windup, Charge, Recovery }

    [Header("Hover")]
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.5f;
    [SerializeField, Min(0.1f)] private float hoverPeriod = 2f;
    [SerializeField, Min(0f)] private float hoverSpeed = 3f;

    [Header("Player Detection")]
    [Tooltip("Maximum distance required to notice the player.")]
    [SerializeField, Min(0f)] private float detectionRadius = 6f;
    [Tooltip("Terrain layers that block detection. Nothing disables the line-of-sight check.")]
    [SerializeField] private LayerMask sightObstacleLayer;
    [Tooltip("Continuous time visible and in range before Prepare starts. Losing sight during this delay resets detection.")]
    [SerializeField, Min(0f)] private float detectionDelay = 0.6f;

    [Header("Charge")]
    [Tooltip("Prepare duration. Once Prepare starts, charge at the first detected position even if the player leaves.")]
    [SerializeField, Min(0f)] private float windupTime = 1f;
    [SerializeField, Min(0f)] private float chargeSpeed = 12f;
    [Tooltip("Pause after charging before hovering and detecting the player again.")]
    [SerializeField, Min(0f)] private float recoveryTime = 1.5f;

    private Rigidbody2D rb;
    private EnemyAI ai;
    private EnemyHealth health;
    private EnemyKnockback knockback;
    private SpriteRenderer spriteRenderer;
    private EnemyAnimation enemyAnimation;
    private State state;
    private Vector2 hoverCenter;
    private Vector2 chargeTarget;
    private float hoverTime;
    private float stateTime;
    private bool needsSpawnReset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
        knockback = GetComponent<EnemyKnockback>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        // Poolers may position an instance after OnEnable. Capture it on its first tick.
        needsSpawnReset = true;
        state = State.Hover;
        stateTime = 0f;
        hoverTime = 0f;
        chargeTarget = Vector2.zero;
    }

    private void OnDisable()
    {
        rb.linearVelocity = Vector2.zero;
        enemyAnimation?.StopCharge();
    }

    private void FixedUpdate()
    {
        if (needsSpawnReset)
        {
            hoverCenter = rb.position;
            needsSpawnReset = false;
        }

        if (!ai.BeginBehaviourTick())
        {
            if (!health.IsDead && knockback != null && knockback.IsKnockbackActive)
            {
                // Let knockback own velocity; restart recovery at its final position.
                state = State.Recovery;
                stateTime = recoveryTime;
                enemyAnimation?.StopCharge();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        float dt = Time.fixedDeltaTime;
        switch (state)
        {
            case State.Hover:
                UpdateHover(dt);
                if (CanSeePlayer())
                {
                    chargeTarget = ai.Player.position;
                    state = State.Detecting;
                    stateTime = detectionDelay;
                    if (stateTime <= 0f) BeginPrepare();
                }
                break;

            case State.Detecting:
                UpdateHover(dt);
                if (!CanSeePlayer())
                {
                    state = State.Hover;
                    break;
                }
                stateTime -= dt;
                if (stateTime <= 0f) BeginPrepare();
                break;

            case State.Windup:
                UpdateHover(dt);
                FaceDirection(chargeTarget - rb.position);
                stateTime -= dt;
                if (stateTime <= 0f) BeginCharge();
                break;

            case State.Charge:
                if (stateTime <= 0f || (chargeTarget - rb.position).sqrMagnitude <= 0.0001f)
                {
                    BeginRecovery();
                    break;
                }
                // Clamp the last step to the remembered point instead of overshooting it.
                Vector2 next = Vector2.MoveTowards(rb.position, chargeTarget, chargeSpeed * dt);
                rb.linearVelocity = (next - rb.position) / dt;
                stateTime -= dt;
                break;

            case State.Recovery:
                rb.linearVelocity = Vector2.zero;
                stateTime -= dt;
                if (stateTime <= 0f)
                {
                    hoverCenter = rb.position;
                    hoverTime = 0f;
                    state = State.Hover;
                }
                break;
        }
    }

    private void UpdateHover(float dt)
    {
        float period = Mathf.Max(0.1f, hoverPeriod);
        hoverTime = Mathf.Repeat(hoverTime + dt, period);
        Vector2 target = hoverCenter + Vector2.up *
            (Mathf.Sin(hoverTime / period * Mathf.PI * 2f) * hoverAmplitude);
        Vector2 next = Vector2.MoveTowards(rb.position, target, hoverSpeed * dt);
        rb.linearVelocity = (next - rb.position) / dt;
    }

    private bool CanSeePlayer()
    {
        if (ai.Player == null || !ai.Player.gameObject.activeInHierarchy) return false;
        Vector2 target = ai.Player.position;
        if ((target - rb.position).sqrMagnitude > detectionRadius * detectionRadius) return false;

        return sightObstacleLayer.value == 0 ||
            Physics2D.Linecast(rb.position, target, sightObstacleLayer).collider == null;
    }

    private void BeginPrepare()
    {
        state = State.Windup;
        stateTime = windupTime;
        FaceDirection(chargeTarget - rb.position);
        enemyAnimation?.PlayPrepare();
    }

    private void BeginCharge()
    {
        Vector2 difference = chargeTarget - rb.position;
        if (difference.sqrMagnitude <= 0.0001f || chargeSpeed <= 0f)
        {
            BeginRecovery();
            return;
        }
        FaceDirection(difference);
        state = State.Charge;
        // Allow the expected travel time plus a little tolerance, so a wall cannot trap us in Charge.
        stateTime = difference.magnitude / chargeSpeed + 0.1f;
        enemyAnimation?.PlayDash();
        rb.linearVelocity = Vector2.zero;
    }

    private void BeginRecovery()
    {
        rb.linearVelocity = Vector2.zero;
        state = State.Recovery;
        stateTime = recoveryTime;
        enemyAnimation?.StopCharge();
    }

    private void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.001f) return;
        ai.TrySetDirection(direction.x > 0f ? 1 : -1);
        if (enemyAnimation != null)
            enemyAnimation.SetDirection(ai.Direction);
        else if (spriteRenderer != null)
            spriteRenderer.flipX = ai.Direction > 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Vector2 center = Application.isPlaying && !needsSpawnReset
            ? hoverCenter : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + Vector2.up * hoverAmplitude, center - Vector2.up * hoverAmplitude);
    }
}
