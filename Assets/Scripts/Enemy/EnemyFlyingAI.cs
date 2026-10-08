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

    // 최초 스폰 위치에서 좌우/상하로 이동할 수 있는 거리 (전체 너비/높이는 값의 두 배)
    [Tooltip("스폰 위치 기준 좌우/상하 패트롤 거리입니다. X=4, Y=2이면 전체 영역은 8 x 4입니다.")]
    [SerializeField] private Vector2 patrolRange = new Vector2(4f, 2f);

    [Min(0f)]
    [SerializeField] private float patrolSpeed = 2f;

    [Min(0f)]
    [SerializeField] private float patrolArrivalDistance = 0.15f;

    [Min(0f)]
    [SerializeField] private float minPatrolWaitTime = 0.4f;

    [Min(0f)]
    [SerializeField] private float maxPatrolWaitTime = 1.2f;

    [Header("Chase")]

    // 패트롤 영역 밖도 추적하지만 이 범위를 넘으면 감지를 무시하고 복귀한다.
    [Tooltip("스폰 위치 기준 최대 추적 거리입니다. 패트롤 범위 이상으로 설정하세요.")]
    [SerializeField] private Vector2 maxChaseRange = new Vector2(10f, 5f);

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
    private Vector2 spawnPosition;
    private bool hasSpawnPosition;

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
    }

    // 스폰 코드가 배치한 최초 위치를 저장한다. 컴포넌트를 다시 켜도 중심은 바뀌지 않는다.
    private void Start()
    {
        spawnPosition = rb.position;
        hasSpawnPosition = true;
    }

    private void FixedUpdate()
    {
        if (ai == null || !ai.BeginBehaviourTick())
        {
            return;
        }

        // 추적과 이탈 후 직진 모두 최대 추적 범위를 벗어나면 즉시 복귀한다.
        if (currentState != AIState.ReturnToPatrol && IsOutsideRange(GetMaxChaseRange()))
        {
            EnterReturnToPatrol();
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
        // 넉백 등으로 패트롤 영역을 벗어난 경우에도 먼저 복귀한다.
        if (IsOutsideRange(GetPatrolRange()))
        {
            EnterReturnToPatrol();
            UpdateReturnState();
            return;
        }

        if (chaseEnabled && ai.CanDetectPlayer())
        {
            EnterChase();
            UpdateChaseState();
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
        if (!chaseEnabled)
        {
            EnterReturnToPatrol();
            UpdateReturnState();
            return;
        }

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
        // 복귀 중에는 감지를 무시하고 중심까지 돌아온 뒤 패트롤을 재개한다.
        if (!hasReturnTarget)
        {
            returnTarget = spawnPosition;
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
        Vector2 range = GetPatrolRange();

        patrolTarget = spawnPosition + new Vector2(
            Random.Range(-range.x, range.x),
            Random.Range(-range.y, range.y)
        );

        hasPatrolTarget = true;
    }

    // 인스펙터를 플레이 중 변경해도 음수 범위나 패트롤보다 작은 추적 범위를 사용하지 않는다.
    private Vector2 GetPatrolRange()
    {
        return new Vector2(Mathf.Max(0f, patrolRange.x), Mathf.Max(0f, patrolRange.y));
    }

    private Vector2 GetMaxChaseRange()
    {
        Vector2 range = GetPatrolRange();
        return new Vector2(
            Mathf.Max(range.x, maxChaseRange.x),
            Mathf.Max(range.y, maxChaseRange.y)
        );
    }

    private bool IsOutsideRange(Vector2 range)
    {
        Vector2 offset = rb.position - spawnPosition;
        return Mathf.Abs(offset.x) > range.x || Mathf.Abs(offset.y) > range.y;
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

        // 높은 속도나 작은 도착 거리에서도 목표점을 지나쳐 왕복하지 않도록 제한한다.
        MoveWithVelocity(toTarget, Mathf.Min(speed, toTarget.magnitude / Time.fixedDeltaTime));
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

    private void OnValidate()
    {
        patrolRange = GetPatrolRange();
        maxChaseRange = GetMaxChaseRange();
        hasPatrolTarget = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying && hasSpawnPosition
            ? spawnPosition
            : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, GetPatrolRange() * 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, GetMaxChaseRange() * 2f);

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
