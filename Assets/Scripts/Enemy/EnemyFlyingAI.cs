using UnityEngine;

// 공중 몬스터는 좌우 패트롤에 상하 물결 움직임을 더한다.
// 생명/넉백 상태와 스프라이트 방향은 기존 EnemyAI를 재사용한다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyKnockback))]
[RequireComponent(typeof(EnemyAnimation))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(EnemyContactAttack))]
public class EnemyFlyingAI : MonoBehaviour
{
    [Header("Patrol Area")]

    // 스폰 위치 기준 좌우 이동 범위와 상하 이동 최대 범위
    [Tooltip("X는 좌우 패트롤 거리, Y는 상하 움직임의 최대 거리입니다.")]
    [SerializeField] private Vector2 patrolRange = new Vector2(4f, 2f);

    [Min(0f)]
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Vertical Wave")]

    [Tooltip("스폰 높이를 기준으로 위아래로 움직이는 거리입니다. Patrol Range Y 이하로 제한됩니다.")]
    [Min(0f)]
    [SerializeField] private float verticalAmplitude = 0.5f;

    [Tooltip("위아래 움직임 한 바퀴에 걸리는 시간입니다. 짧을수록 자주 오르내립니다.")]
    [Min(0.1f)]
    [SerializeField] private float verticalPeriod = 2f;

    [Tooltip("상하 이동 속도 제한입니다. 넉백 후에도 원래 높이로 순간 이동하지 않습니다.")]
    [Min(0f)]
    [SerializeField] private float maxVerticalSpeed = 3f;

    [Header("Turn")]

    // 좌우 끝에 도착하면 잠시 기다렸다가 방향을 바꾼다. 대기 중에도 상하로 떠다닌다.
    [Min(0f)]
    [SerializeField] private float minPatrolWaitTime = 0.2f;

    [Min(0f)]
    [SerializeField] private float maxPatrolWaitTime = 0.5f;

    [Min(0f)]
    [SerializeField] private float directionChangeCooldown = 0.5f;

    private Rigidbody2D rb;
    private EnemyAI ai;
    private EnemyKnockback knockback;

    private Vector2 spawnPosition;
    private bool hasSpawnPosition;
    private float waveTime;
    private float patrolWaitTimer;
    private int pendingDirection;
    private float nextDirectionChangeTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ai = GetComponent<EnemyAI>();
        knockback = GetComponent<EnemyKnockback>();

        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        waveTime = 0f;
        patrolWaitTimer = 0f;
        pendingDirection = 0;
        nextDirectionChangeTime = -999f;
    }

    // 최초 배치 위치를 고정 패트롤 중심으로 저장한다.
    private void Start()
    {
        spawnPosition = rb.position;
        hasSpawnPosition = true;
    }

    private void FixedUpdate()
    {
        if (!hasSpawnPosition || ai == null) return;

        if (!ai.BeginBehaviourTick())
        {
            // 넉백 중에는 넉백 컴포넌트가 속도를 제어한다. 사망/AI 정지 시에는 상하 이동도 멈춘다.
            if (knockback == null || !knockback.IsKnockbackActive)
            {
                rb.linearVelocity = Vector2.zero;
            }

            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        waveTime = Mathf.Repeat(waveTime + deltaTime, Mathf.Max(0.1f, verticalPeriod));

        float horizontalVelocity = UpdateHorizontalPatrol(deltaTime);
        float verticalVelocity = GetVerticalVelocity(deltaTime);
        rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
    }

    // 좌우 경계에서 대기 후 반전한다. 넉백으로 영역을 벗어나면 안쪽을 향해 복귀한다.
    private float UpdateHorizontalPatrol(float deltaTime)
    {
        float rangeX = Mathf.Max(0f, patrolRange.x);
        float minX = spawnPosition.x - rangeX;
        float maxX = spawnPosition.x + rangeX;
        float positionX = rb.position.x;

        if (rangeX <= 0.001f)
        {
            pendingDirection = 0;
            return GetHorizontalVelocity(spawnPosition.x, deltaTime);
        }

        if (positionX < minX - 0.01f || positionX > maxX + 0.01f)
        {
            int inwardDirection = positionX < minX ? 1 : -1;
            pendingDirection = 0;
            patrolWaitTimer = 0f;

            if (!TrySetDirection(inwardDirection)) return 0f;

            return GetHorizontalVelocity(inwardDirection > 0 ? maxX : minX, deltaTime);
        }

        if (pendingDirection != 0)
        {
            patrolWaitTimer = Mathf.Max(0f, patrolWaitTimer - deltaTime);

            if (patrolWaitTimer > 0f || !TrySetDirection(pendingDirection))
            {
                return 0f;
            }

            pendingDirection = 0;
        }

        float targetX = ai.Direction > 0 ? maxX : minX;

        if (Mathf.Abs(targetX - positionX) <= 0.01f)
        {
            pendingDirection = -ai.Direction;
            float minWait = Mathf.Max(0f, minPatrolWaitTime);
            patrolWaitTimer = Random.Range(minWait, Mathf.Max(minWait, maxPatrolWaitTime));
            return 0f;
        }

        return GetHorizontalVelocity(targetX, deltaTime);
    }

    // 한 틱에 목표 경계를 지나치지 않도록 속도를 제한한다.
    private float GetHorizontalVelocity(float targetX, float deltaTime)
    {
        float nextX = Mathf.MoveTowards(rb.position.x, targetX, Mathf.Max(0f, patrolSpeed) * deltaTime);
        return (nextX - rb.position.x) / deltaTime;
    }

    // 위/아래 전환이 부드러운 사인파를 사용하며 좌우 반전 시에도 파동은 이어진다.
    private float GetVerticalVelocity(float deltaTime)
    {
        float amplitude = Mathf.Min(Mathf.Max(0f, verticalAmplitude), Mathf.Max(0f, patrolRange.y));
        float phase = waveTime / Mathf.Max(0.1f, verticalPeriod) * Mathf.PI * 2f;
        float targetY = spawnPosition.y + Mathf.Sin(phase) * amplitude;
        float nextY = Mathf.MoveTowards(rb.position.y, targetY, Mathf.Max(0f, maxVerticalSpeed) * deltaTime);

        return (nextY - rb.position.y) / deltaTime;
    }

    // 기존 AI의 방향 및 애니메이션 동기화를 사용한다.
    private bool TrySetDirection(int direction)
    {
        if (ai.Direction == direction) return true;
        if (Time.time < nextDirectionChangeTime) return false;
        if (!ai.TrySetDirection(direction)) return false;

        nextDirectionChangeTime = Time.time + Mathf.Max(0f, directionChangeCooldown);
        return true;
    }

    private void OnValidate()
    {
        patrolRange = new Vector2(Mathf.Max(0f, patrolRange.x), Mathf.Max(0f, patrolRange.y));
        verticalAmplitude = Mathf.Max(0f, verticalAmplitude);
        verticalPeriod = Mathf.Max(0.1f, verticalPeriod);
        maxPatrolWaitTime = Mathf.Max(minPatrolWaitTime, maxPatrolWaitTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying && hasSpawnPosition
            ? spawnPosition
            : (Vector2)transform.position;
        Vector2 range = new Vector2(Mathf.Max(0f, patrolRange.x), Mathf.Max(0f, patrolRange.y));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, range * 2f);

        // 실제 상하 파동 폭도 표시한다.
        float amplitude = Mathf.Min(Mathf.Max(0f, verticalAmplitude), range.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(range.x * 2f, amplitude * 2f, 0f));
    }
}
