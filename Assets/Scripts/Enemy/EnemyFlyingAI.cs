using UnityEngine;

// 공중 몬스터의 패트롤과 플레이어 추적을 담당하는 AI
// EnemyAI는 플레이어 감지, 방향, 사망/넉백 상태를 재사용하고 실제 이동은 이 스크립트가 처리한다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyKnockback))]
[RequireComponent(typeof(EnemyAnimation))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyContactAttack))]
public class EnemyFlyingAI : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Chase,
        LostTarget,
        ReturnToPatrol
    }

    private AIState currentState = AIState.Patrol;

    [Header("Mode")]

    // 켜져 있으면 EnemyAI의 감지/해제 거리로 플레이어를 추적한다.
    // 끄면 플레이어를 무시하는 공중 패트롤 몬스터가 된다.
    [SerializeField] private bool chaseEnabled = true;

    [Header("Patrol Area")]

    // 월드 기준 사각형 영역. BoxCollider2D의 Bounds 안에서 랜덤 지점을 선택한다.
    [SerializeField] private BoxCollider2D patrolArea;

    // 영역 가장자리에서 너무 붙지 않도록 남겨두는 여백
    [Min(0f)]
    [SerializeField] private float patrolAreaPadding = 0.2f;

    [Min(0f)]
    [SerializeField] private float patrolSpeed = 2f;

    [Min(0f)]
    [SerializeField] private float patrolArrivalDistance = 0.15f;

    [Min(0f)]
    [SerializeField] private float minPatrolWaitTime = 0.4f;

    [Min(0f)]
    [SerializeField] private float maxPatrolWaitTime = 1.2f;

    [Header("Chase")]

    [Min(0f)]
    [SerializeField] private float chaseSpeed = 3f;

    // 0에 가까울수록 플레이어에게 더 가까이 붙는다.
    [Min(0f)]
    [SerializeField] private float chaseStopDistance = 0.1f;

    [Header("Direction")]

    // 플레이어 추적이나 패트롤 지점 변경 시 좌우 방향을 너무 자주 바꾸지 않도록 하는 내부 쿨타임
    [Min(0f)]
    [SerializeField] private float directionChangeCooldown = 0.25f;

    [Header("Lost Target")]

    // 플레이어를 놓친 직후 기존 이동 방향을 유지하는 시간
    [Min(0f)]
    [SerializeField] private float continueDirectionTime = 0.75f;

    [Min(0f)]
    [SerializeField] private float lostTargetSpeed = 2.5f;

    [Min(0f)]
    [SerializeField] private float returnSpeed = 2.5f;

    [Min(0f)]
    [SerializeField] private float returnArrivalDistance = 0.15f;

    private Rigidbody2D rb;
    private EnemyAI ai;

    private Vector2 patrolTarget;
    private bool hasPatrolTarget;
    private float patrolWaitTimer;

    private Vector2 lostMoveDirection;
    private float lostTargetTimer;

    private Vector2 returnTarget;
    private bool hasReturnTarget;

    private float nextDirectionChangeTime;
    private bool missingPatrolAreaWarningShown;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ai = GetComponent<EnemyAI>();

        // 공중 몬스터는 중력으로 떨어지지 않고 AI가 모든 이동을 제어한다.
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        currentState = AIState.Patrol;
        hasPatrolTarget = false;
        hasReturnTarget = false;
        patrolWaitTimer = 0f;
        lostTargetTimer = 0f;
        lostMoveDirection = Vector2.zero;
        nextDirectionChangeTime = -999f;
        missingPatrolAreaWarningShown = false;
    }

    private void FixedUpdate()
    {
        if (ai == null || !ai.BeginBehaviourTick())
        {
            return;
        }

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrolState();
                break;

            case AIState.Chase:
                UpdateChaseState();
                break;

            case AIState.LostTarget:
                UpdateLostTargetState();
                break;

            case AIState.ReturnToPatrol:
                UpdateReturnState();
                break;
        }
    }

    private void UpdatePatrolState()
    {
        if (chaseEnabled && ai.CanDetectPlayer())
        {
            EnterChase();
            UpdateChaseState();
            return;
        }

        if (patrolArea == null)
        {
            WarnMissingPatrolArea();
            StopMovement();
            return;
        }

        if (!hasPatrolTarget)
        {
            SetRandomPatrolTarget();
        }

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.fixedDeltaTime;
            StopMovement();
            return;
        }

        if (MoveTowards(patrolTarget, patrolSpeed, patrolArrivalDistance))
        {
            patrolWaitTimer = GetRandomPatrolWaitTime();
            hasPatrolTarget = false;
            StopMovement();
        }
    }

    private void UpdateChaseState()
    {
        if (!chaseEnabled || ai.Player == null)
        {
            EnterReturnToPatrol();
            return;
        }

        if (ai.ShouldStopChasing())
        {
            EnterLostTarget();
            UpdateLostTargetState();
            return;
        }

        Vector2 toPlayer = (Vector2)ai.Player.position - rb.position;

        if (toPlayer.magnitude <= chaseStopDistance)
        {
            StopMovement();
            return;
        }

        MoveWithVelocity(toPlayer, chaseSpeed);
    }

    private void UpdateLostTargetState()
    {
        if (chaseEnabled && ai.CanDetectPlayer())
        {
            EnterChase();
            UpdateChaseState();
            return;
        }

        if (lostTargetTimer > 0f)
        {
            lostTargetTimer -= Time.fixedDeltaTime;

            if (lostMoveDirection.sqrMagnitude > 0.0001f)
            {
                MoveWithVelocity(lostMoveDirection, lostTargetSpeed);
            }
            else
            {
                StopMovement();
            }

            return;
        }

        EnterReturnToPatrol();
        UpdateReturnState();
    }

    private void UpdateReturnState()
    {
        if (chaseEnabled && ai.CanDetectPlayer())
        {
            EnterChase();
            UpdateChaseState();
            return;
        }

        if (patrolArea == null)
        {
            WarnMissingPatrolArea();
            EnterPatrol();
            return;
        }

        if (!hasReturnTarget)
        {
            returnTarget = GetClosestPointInsidePatrolArea(transform.position);
            hasReturnTarget = true;
        }

        if (MoveTowards(returnTarget, returnSpeed, returnArrivalDistance))
        {
            hasReturnTarget = false;
            EnterPatrol();
        }
    }

    private void EnterChase()
    {
        currentState = AIState.Chase;
        hasPatrolTarget = false;
        hasReturnTarget = false;
    }

    private void EnterLostTarget()
    {
        currentState = AIState.LostTarget;
        lostTargetTimer = continueDirectionTime;
        lostMoveDirection = rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : new Vector2(ai.Direction, 0f);
    }

    private void EnterReturnToPatrol()
    {
        currentState = AIState.ReturnToPatrol;
        hasPatrolTarget = false;
        hasReturnTarget = false;
        StopMovement();
    }

    private void EnterPatrol()
    {
        currentState = AIState.Patrol;
        hasPatrolTarget = false;
        patrolWaitTimer = 0f;
        StopMovement();
    }

    private void SetRandomPatrolTarget()
    {
        Bounds bounds = patrolArea.bounds;

        float paddingX = Mathf.Min(patrolAreaPadding, bounds.extents.x);
        float paddingY = Mathf.Min(patrolAreaPadding, bounds.extents.y);

        patrolTarget = new Vector2(
            Random.Range(bounds.min.x + paddingX, bounds.max.x - paddingX),
            Random.Range(bounds.min.y + paddingY, bounds.max.y - paddingY)
        );

        hasPatrolTarget = true;
    }

    private Vector2 GetClosestPointInsidePatrolArea(Vector2 position)
    {
        Bounds bounds = patrolArea.bounds;

        float paddingX = Mathf.Min(patrolAreaPadding, bounds.extents.x);
        float paddingY = Mathf.Min(patrolAreaPadding, bounds.extents.y);

        return new Vector2(
            Mathf.Clamp(position.x, bounds.min.x + paddingX, bounds.max.x - paddingX),
            Mathf.Clamp(position.y, bounds.min.y + paddingY, bounds.max.y - paddingY)
        );
    }

    private float GetRandomPatrolWaitTime()
    {
        float min = Mathf.Max(0f, minPatrolWaitTime);
        float max = Mathf.Max(min, maxPatrolWaitTime);

        return Random.Range(min, max);
    }

    private bool MoveTowards(Vector2 target, float speed, float arrivalDistance)
    {
        Vector2 toTarget = target - rb.position;

        if (toTarget.magnitude <= arrivalDistance)
        {
            return true;
        }

        MoveWithVelocity(toTarget, speed);
        return false;
    }

    private void MoveWithVelocity(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude <= 0.0001f || speed <= 0f)
        {
            StopMovement();
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        rb.linearVelocity = normalizedDirection * speed;
        UpdateFacing(normalizedDirection.x);
    }

    private void StopMovement()
    {
        if (rb == null) return;

        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= 0.01f) return;
        if (Time.time < nextDirectionChangeTime) return;

        int newDirection = horizontalDirection > 0f ? 1 : -1;

        if (ai.Direction == newDirection) return;

        if (ai.TrySetDirection(newDirection))
        {
            nextDirectionChangeTime = Time.time + directionChangeCooldown;
        }
    }

    private void WarnMissingPatrolArea()
    {
        if (missingPatrolAreaWarningShown) return;

        Debug.LogWarning($"{name}: EnemyFlyingAI에 Patrol Area(BoxCollider2D)가 지정되지 않았습니다.", this);
        missingPatrolAreaWarningShown = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolArea != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(patrolArea.bounds.center, patrolArea.bounds.size);
        }

        if (hasPatrolTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(patrolTarget, 0.12f);
        }

        if (hasReturnTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(returnTarget, 0.12f);
        }
    }
}
