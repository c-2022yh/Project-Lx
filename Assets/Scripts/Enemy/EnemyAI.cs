using UnityEngine;

//모든 적이 공통으로 사용하는 AI 기능
//이동, 패트롤, 플레이어 탐지, 방향 전환, 환경 체크 등을 담당
public class EnemyAI : MonoBehaviour
{
    private enum PatrolState
    {
        Idle,
        Walk
    }

    private PatrolState patrolState = PatrolState.Idle;


    [Header("Movement")]

    [SerializeField] private float moveSpeed = 3f;

    //방향전환 내부 쿨다운
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


    [Header("Player Detection")]

    //플레이어 발견 범위
    [SerializeField] private float detectionRangeX = 6f;
    [SerializeField] private float detectionRangeY = 1.5f;

    //추적 해제 범위
    [SerializeField] private float loseRangeX = 9f;
    [SerializeField] private float loseRangeY = 3f;

    private Transform player;

    //플레이어가 씬에 늦게 생성될 수 있으므로 주기적으로 탐색
    [SerializeField] private float playerSearchInterval = 0.5f;

    private float nextPlayerSearchTime;


    [Header("Environment Check")]

    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;

    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float wallCheckDistance = 0.3f;

    [SerializeField] private LayerMask groundLayer;

    //체크 오브젝트의 원래 X 위치
    private float groundCheckX;
    private float wallCheckX;


    //컴포넌트
    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyAnimation enemyAnimation;
    private EnemyKnockback knockback;


    //기본 스프라이트가 왼쪽을 보고 있으므로
    //-1 = 왼쪽, 1 = 오른쪽
    private int direction = -1;

    private bool aiStopped;


    //다른 스크립트에서 사용하는 공통 값
    public int Direction => direction;
    public float MoveSpeed => moveSpeed;
    public Transform Player => player;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        knockback = GetComponent<EnemyKnockback>();

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

        direction = -1;

        patrolState = PatrolState.Idle;
        patrolTimer = Random.Range(minIdleTime, maxIdleTime);

        lastTurnTime = -999f;

        ApplyCheckDirection();
    }


    //각 행동 AI의 FixedUpdate 시작 부분에서 호출
    //현재 AI가 행동 가능한 상태인지 확인
    public bool BeginBehaviourTick()
    {
        if (rb == null || health == null) return false;

        if (aiStopped)
        {
            StopMovement();
            return false;
        }

        if (health.IsDead)
        {
            StopMovement();
            return false;
        }

        //넉백 중에는 AI가 Rigidbody 속도를 덮어쓰지 않는다.
        if (knockback != null && knockback.IsKnockbackActive)
        {
            return false;
        }

        FindPlayerIfNeeded();

        return true;
    }

    //패트롤
    public void UpdatePatrol()
    {
        patrolTimer -= Time.fixedDeltaTime;

        bool hasGroundAhead = IsGroundAhead();
        bool hasWallAhead = IsWallAhead();


        //정지 상태
        if (patrolState == PatrolState.Idle)
        {
            StopMovement();

            if (patrolTimer <= 0f)
            {
                StartPatrolWalk();
            }

            return;
        }


        //이동 상태
        if (hasWallAhead || !hasGroundAhead)
        {
            HandleObstacle();
            return;
        }

        Move();


        if (patrolTimer <= 0f)
        {
            StartPatrolIdle();
        }
    }


    public void StartPatrolIdle()
    {
        patrolState = PatrolState.Idle;

        patrolTimer = Random.Range(minIdleTime, maxIdleTime);

        StopMovement();
    }


    private void StartPatrolWalk()
    {
        patrolState = PatrolState.Walk;

        patrolTimer = Random.Range(minWalkTime, maxWalkTime);

        if (Random.value < patrolTurnChance)
        {
            TryFlipDirection();
        }
    }

    //플레이어 찾기
    private void FindPlayerIfNeeded()
    {
        if (player != null) return;
        if (Time.time < nextPlayerSearchTime) return;

        nextPlayerSearchTime = Time.time + playerSearchInterval;

        Player foundPlayer = FindFirstObjectByType<Player>();

        if (foundPlayer != null)
        {
            player = foundPlayer.transform;
        }
    }


    public bool CanDetectPlayer()
    {
        if (player == null) return false;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        return distanceX <= detectionRangeX && distanceY <= detectionRangeY;
    }


    public bool ShouldStopChasing()
    {
        if (player == null) return true;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);  

        return distanceX >= loseRangeX || distanceY >= loseRangeY;
    }


    //환경 체크 변수
    public bool IsGroundAhead()
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


    public bool IsWallAhead()
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


    //현재 바라보는 방향으로 안전하게 이동 가능한지
    public bool CanMoveForward()
    {
        return IsGroundAhead() && !IsWallAhead();
    }


    public void HandleObstacle()
    {
        //방향전환이 가능한 경우 반전
        if (CanTurn())
        {
            FlipDirection();
            return;
        }

        //쿨다운 중 다시 장애물을 만나면 정지
        StopMovement();
    }

    //방향
    private bool CanTurn()
    {
        return Time.time >= lastTurnTime + turnCooldown;
    }


    public bool TryFlipDirection()
    {
        if (!CanTurn())
            return false;

        FlipDirection();

        return true;
    }


    private void FlipDirection()
    {
        SetDirectionInternal(-direction);
    }


    public bool TrySetDirection(int newDirection)
    {
        newDirection = newDirection > 0 ? 1 : -1;

        //이미 같은 방향
        if (newDirection == direction) return true;

        if (!CanTurn()) return false;

        SetDirectionInternal(newDirection);

        return true;
    }


    private void SetDirectionInternal(int newDirection)
    {
        if (newDirection == 0) return;

        direction = newDirection > 0 ? 1 : -1;

        lastTurnTime = Time.time;

        //스프라이트 방향 변경
        enemyAnimation?.SetDirection(direction);

        //GroundCheck / WallCheck 위치도 변경
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


    //플레이어 방향으로 회전
    public void FacePlayer()
    {
        if (player == null) return;

        float differenceX = player.position.x - transform.position.x;

        if (Mathf.Abs(differenceX) <= 0.01f) return;

        int playerDirection = differenceX > 0f ? 1 : -1;

        TrySetDirection(playerDirection);
    }

    //이동
    public void Move()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }


    public void StopMovement()
    {
        if (rb == null) return;
        
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);   
    }

    //멈춤
    public void StopAI()
    {
        aiStopped = true;

        StopMovement();
    }
}