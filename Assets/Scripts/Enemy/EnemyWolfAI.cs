using UnityEngine;

// Selection only. Attack owns movement and facing until recovery completes.
[RequireComponent(typeof(EnemyAI), typeof(EnemyWolfAttack))]
public class EnemyWolfAI : MonoBehaviour
{
    [Header("Attack Selection (world units)")]
    [SerializeField, Min(0f)] private float biteRange = 1.8f;
    [SerializeField, Min(0f)] private float chargeMinRange = 3f;
    [SerializeField, Min(0f)] private float chargeMaxRange = 6f;
    [SerializeField, Min(0f)] private float maxAttackHeightDifference = 0.8f;
    private EnemyAI ai;
    private EnemyWolfAttack attack;
    private bool chasing;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        attack = GetComponent<EnemyWolfAttack>();
    }

    private void OnEnable() => chasing = false;

    private void OnValidate()
    {
        chargeMinRange = Mathf.Max(biteRange, chargeMinRange);
        chargeMaxRange = Mathf.Max(chargeMinRange, chargeMaxRange);
    }

    private void FixedUpdate()
    {
        attack.Tick(Time.fixedDeltaTime);
        if (attack.IsAttacking || !ai.BeginBehaviourTick()) return;
        if (!chasing)
        {
            if (!ai.CanDetectPlayer()) { ai.UpdatePatrol(); return; }
            chasing = true;
        }
        if (ai.ShouldStopChasing())
        {
            chasing = false;
            ai.StartPatrolIdle();
            return;
        }

        Vector2 delta = ai.Player.position - transform.position;
        int direction = delta.x >= 0f ? 1 : -1;
        if (!ai.TrySetDirection(direction)) { ai.StopMovement(); return; }
        float distance = Mathf.Abs(delta.x);
        bool sameHeight = Mathf.Abs(delta.y) <= maxAttackHeightDifference;
        if (sameHeight && distance <= biteRange)
        {
            ai.StopMovement();
            attack.TryStart(false);
            return;
        }
        if (sameHeight && distance >= chargeMinRange && distance <= chargeMaxRange &&
            attack.TryStart(true)) return;
        // Wait at the edge of this platform instead of repeatedly turning.
        if (ai.CanMoveForward()) ai.Move();
        else ai.StopMovement();
    }
}
