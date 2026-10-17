using UnityEngine;

// Hovers in place, then charges in a fixed direction using contact damage.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyAI), typeof(EnemyHealth))]
[RequireComponent(typeof(Enemy), typeof(EnemyStats), typeof(EnemyContactAttack))]
public class EnemyFlyingChargeAI : MonoBehaviour
{
    private enum State { Hover, Windup, Charge, Recovery }

    [Header("Hover")]
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.5f;
    [SerializeField, Min(0.1f)] private float hoverPeriod = 2f;
    [SerializeField, Min(0f)] private float hoverSpeed = 3f;

    [Header("Player Detection")]
    [Tooltip("Distance to the player required to begin and maintain the windup.")]
    [SerializeField, Min(0f)] private float detectionRadius = 6f;

    [Header("Charge")]
    [Tooltip("Time spent hovering in range before charging. Leaving range cancels it.")]
    [SerializeField, Min(0f)] private float windupTime = 1f;
    [SerializeField, Min(0f)] private float chargeSpeed = 12f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.5f;
    [Tooltip("Pause after charging before hovering and detecting the player again.")]
    [SerializeField, Min(0f)] private float recoveryTime = 1.5f;

    private Rigidbody2D rb;
    private EnemyAI ai;
    private EnemyHealth health;
    private EnemyKnockback knockback;
    private SpriteRenderer spriteRenderer;
    private State state;
    private Vector2 hoverCenter;
    private Vector2 chargeDirection;
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
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        // Poolers may position an instance after OnEnable. Capture it on its first tick.
        needsSpawnReset = true;
        state = State.Hover;
        stateTime = 0f;
        hoverTime = 0f;
        chargeDirection = Vector2.zero;
    }

    private void OnDisable()
    {
        rb.linearVelocity = Vector2.zero;
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
                if (IsPlayerInRange())
                {
                    state = State.Windup;
                    stateTime = windupTime;
                }
                break;

            case State.Windup:
                UpdateHover(dt);
                if (!IsPlayerInRange())
                {
                    state = State.Hover;
                    break;
                }
                FaceDirection((Vector2)ai.Player.position - rb.position);
                stateTime -= dt;
                if (stateTime <= 0f) BeginCharge();
                break;

            case State.Charge:
                if (stateTime <= 0f)
                {
                    rb.linearVelocity = Vector2.zero;
                    state = State.Recovery;
                    stateTime = recoveryTime;
                    break;
                }
                // The last physics step covers only the remaining charge duration.
                rb.linearVelocity = chargeDirection * chargeSpeed * (Mathf.Min(dt, stateTime) / dt);
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

    private bool IsPlayerInRange()
    {
        return ai.Player != null &&
            ((Vector2)ai.Player.position - rb.position).sqrMagnitude <= detectionRadius * detectionRadius;
    }

    private void BeginCharge()
    {
        // Aim once at launch, allowing the player to dodge sideways during the charge.
        Vector2 difference = (Vector2)ai.Player.position - rb.position;
        chargeDirection = difference.sqrMagnitude > 0.0001f
            ? difference.normalized
            : Vector2.right * ai.Direction;
        FaceDirection(chargeDirection);
        state = State.Charge;
        stateTime = chargeDuration;
        rb.linearVelocity = Vector2.zero;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.001f) return;
        ai.TrySetDirection(direction.x > 0f ? 1 : -1);
        // EnemyAnimation is optional. Existing enemy art faces left by default.
        if (spriteRenderer != null) spriteRenderer.flipX = ai.Direction > 0;
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
