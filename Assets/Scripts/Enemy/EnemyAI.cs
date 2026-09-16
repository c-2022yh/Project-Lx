using UnityEngine;
using System.Collections;

//적의 이동과 행동 판단을 제어하는 스크립트
public class EnemyAI : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Chase
    }

    private enum PatrolState
    {
        Idle,
        Walk
    }

    private AIState currentState = AIState.Patrol;
    private PatrolState patrolState = PatrolState.Idle;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    //방향전환 내부쿨(부르르 떠는거 방지)
    [SerializeField] private float turnCooldown = 0.5f;

    private float lastTurnTime = -999f;

    [Header("Patrol")]

    //정지 시간
    [SerializeField] private float minIdleTime = 0f;
    [SerializeField] private float maxIdleTime = 2f;

    //이동 시간
    [SerializeField] private float minWalkTime = 1.5f;
    [SerializeField] private float maxWalkTime = 3f;

    //이동 시작 시 방향을 반전할 확률
    [Range(0f, 1f)]
    [SerializeField] private float patrolTurnChance = 0.5f;

    private float patrolTimer;


    [Header("Chase")]

    //플레이어를 발견하는 좌우 범위
    [SerializeField] private float detectionRangeX = 6f;

    //플레이어를 발견하는 위아래 범위
    [SerializeField] private float detectionRangeY = 1.5f;

    //이 거리 이상 멀어지면 다시 플레이어 방향으로 방향 전환
    //예: 1이면 플레이어 X를 중심으로 대략 ±1 범위를 왕복
    [SerializeField] private float chaseTurnDistance = 1f;

    //추적 해제 범위
    //감지 범위보다 크게 설정하여 경계에서 상태가 떨리는 것을 방지
    [SerializeField] private float loseRangeX = 9f;
    [SerializeField] private float loseRangeY = 3f;

    private Transform player;

    //플레이어가 씬에 늦게 생성될 수도 있으므로
    //일정 시간마다 다시 찾는다.
    [SerializeField] private float playerSearchInterval = 0.5f;
    private float nextPlayerSearchTime;

    [Header("Environment Check")]

    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;

    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float wallCheckDistance = 0.3f;

    [SerializeField] private LayerMask groundLayer;

    //GroundCheck / WallCheck의 원래 X 위치
    private float groundCheckX;
    private float wallCheckX;

    //컴포넌트
    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyAnimation enemyAnimation;
    private EnemyKnockback knockback;

    //기본 스프라이트가 왼쪽을 보고 있으므로
    //-1 = 왼쪽,1 = 오른쪽
    private int direction = -1;

    private bool aiStopped;

    public int Direction => direction;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        knockback = GetComponent<EnemyKnockback>();

        //체크
        if (groundCheck != null)
        {
            groundCheckX = Mathf.Abs(groundCheck.localPosition.x);
        }
        if (wallCheck != null)
        {
            wallCheckX = Mathf.Abs(wallCheck.localPosition.x);
        }
        

    }

    private void OnEnable()
    {
        aiStopped = false;

        //풀에서 다시 활성화될 때 기본 상태로 초기화
        direction = -1;

        currentState = AIState.Patrol;
        patrolState = PatrolState.Idle;

        patrolTimer = Random.Range(minIdleTime, maxIdleTime);

        lastTurnTime = -999f;

        ApplyCheckDirection();
    }

    private void FixedUpdate()
    {
        if (rb == null || health == null) return;

        if (aiStopped)
        {
            StopMovement();
            return;
        }

        if (health.IsDead)
        {
            StopMovement();
            return;
        }

        //피격 넉백 중에는 AI가 속도를 덮어쓰지 않는다.
        if (knockback != null && knockback.IsKnockbackActive)
        {
            return;
        }

        //플레이어가 존재하지 않는 경우 주기적으로 다시 탐색
        FindPlayerIfNeeded();

        //환경 체크는 상태와 상관없이 계속 실행
        bool hasGroundAhead = IsGroundAhead();
        bool hasWallAhead = IsWallAhead();

        //상태 구분하기
        if (currentState == AIState.Patrol)
        {
            if (CanDetectPlayer())
            {
                EnterChase();
            }
        }
        else if (currentState == AIState.Chase)
        {
            if (ShouldStopChasing())
            {
                EnterPatrol();
            }
        }

        //현재 상태 실행
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol(hasGroundAhead,hasWallAhead);
                break;

            case AIState.Chase:
                UpdateChase(hasGroundAhead, hasWallAhead);
                break;
        }
    }

    //패트롤
    private void UpdatePatrol(bool hasGroundAhead, bool hasWallAhead)
    {
        patrolTimer -= Time.fixedDeltaTime;

        //정지
        if (patrolState == PatrolState.Idle)
        {
            StopMovement();

            if (patrolTimer <= 0f) StartPatrolWalk();
            
            return;
        }

        //이동
        if (hasWallAhead || !hasGroundAhead)
        {
            HandleObstacle();
            return;
        }

        Move();

        //이동 시간이 끝나면 다시 정지
        if (patrolTimer <= 0f) StartPatrolIdle();
        
    }

    private void StartPatrolIdle()
    {
        patrolState = PatrolState.Idle;

        patrolTimer = Random.Range(minIdleTime, maxIdleTime);

        StopMovement();
    }

    private void StartPatrolWalk()
    {
        patrolState = PatrolState.Walk;

        patrolTimer = Random.Range(minWalkTime,  maxWalkTime);


        //50% 확률로 방향 변경
        if (Random.value < patrolTurnChance)
        {
            TryFlipDirection();
        }
    }

    //추적
    private void UpdateChase(bool hasGroundAhead, bool hasWallAhead)
    {
        if (player == null)
        {
            EnterPatrol();
            return;
        }

        //벽이나 낭떠러지 만나면?
        if (hasWallAhead || !hasGroundAhead)
        {
            HandleObstacle();
            return;
        }

        //플레이어와의 x거리 계산
        float differenceX = player.position.x - transform.position.x;
        float distanceX = Mathf.Abs(differenceX);

        //플레이어와 일정 거리 이상 떨어졌을 때, 플레이어 방향으로 방향을 보정한다.
        if (distanceX >= chaseTurnDistance)
        {
            int playerDirection = differenceX > 0f ? 1 : -1;
            TrySetDirection(playerDirection);
        }

        //플레이어를 지나가더라도 계속 이동
        Move();
    }

    //플레이어 추적
    private void EnterChase()
    {
        currentState = AIState.Chase;

        if (player == null) return;

        //초기방향
        float differenceX = player.position.x - transform.position.x;

        if (Mathf.Abs(differenceX) > 0.01f)
        {
            int playerDirection = differenceX > 0f ? 1 : -1;

            TrySetDirection(playerDirection);
        }
    }

    private void EnterPatrol()
    {
        currentState = AIState.Patrol;

        StartPatrolIdle();
    }


    //플레이어 찾기
    private void FindPlayerIfNeeded()
    {
        if (player != null) return;
        if (Time.time < nextPlayerSearchTime) return;

        nextPlayerSearchTime = Time.time + playerSearchInterval;
        Player foundPlayer =FindFirstObjectByType<Player>();

        if (foundPlayer != null)
        {
            player = foundPlayer.transform;
        }
    }

    private bool CanDetectPlayer()
    {
        if (player == null) return false;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        return distanceX <= detectionRangeX && distanceY <= detectionRangeY;
    }

    private bool ShouldStopChasing()
    {
        if (player == null) return true;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        return distanceX >= loseRangeX || distanceY >= loseRangeY;
    }

    private bool IsGroundAhead()
    {
        if (groundCheck == null) return true;

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool IsWallAhead()
    {
        if (wallCheck == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );


        return hit.collider != null;
    }

    private void HandleObstacle()
    {
        //방향전환이 가능한 상태라면
        //벽 / 낭떠러지에서 즉시 반전
        if (CanTurn())
        {
            FlipDirection();
            return;
        }

        //방향전환 쿨다운 중인데
        //또 벽이나 낭떠러지를 만났다면
        //억지로 진행하지 않고 정지한다.
        //특히 낭떠러지에서 떨어지는 것을 방지.
        StopMovement();
    }


    private bool CanTurn()
    {
        return Time.time >= lastTurnTime + turnCooldown;
    }

    private bool TryFlipDirection()
    {
        if (!CanTurn()) return false;

        FlipDirection();

        return true;
    }

    private void FlipDirection()
    {
        SetDirectionInternal(-direction);
    }

    private bool TrySetDirection(int newDirection)
    {
        newDirection = newDirection > 0 ? 1 : -1;

        // 이미 같은 방향이면 변경할 필요 없음
        if (newDirection == direction) return true;


        if (!CanTurn()) return false;

        SetDirectionInternal(newDirection);

        return true;
    }

    private void SetDirectionInternal(int newDirection)
    {
        if (newDirection == 0) return;
        int oldDirection = direction;
        direction = newDirection > 0 ? 1 : -1;
        lastTurnTime = Time.time;

        //스프라이트 방향 변경
        enemyAnimation?.SetDirection(direction);

        //GroundCheck, WallCheck도 같이 이동
        ApplyCheckDirection();
    }

    private void ApplyCheckDirection()
    {
        if (groundCheck != null)
        {
            Vector3 pos = groundCheck.localPosition;
            pos.x = groundCheckX * direction;
            groundCheck.localPosition = pos;
        }

        if (wallCheck != null)
        {
            Vector3 pos = wallCheck.localPosition;
            pos.x = wallCheckX * direction;
            wallCheck.localPosition = pos;
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        
    }

    public void StopMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    //사망
    public void StopAI()
    {
        aiStopped = true;

        StopMovement();
    }
   
}