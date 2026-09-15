using UnityEngine;

//적의 이동과 행동 판단을 제어하는 스크립트
public class EnemyAI : MonoBehaviour
{

    [Header("Debug Visual")]
    [SerializeField] private SpriteRenderer groundCheckVisual;
    [SerializeField] private SpriteRenderer wallCheckVisual;

    [SerializeField]
    private Color normalColor =
        new Color(1f, 1f, 1f, 0.25f);

    [SerializeField]
    private Color detectedColor =
        new Color(1f, 0f, 0f, 0.6f);


    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private Transform player;

    [Header("Environment Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;

    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float wallCheckDistance = 0.5f;

    [SerializeField] private LayerMask groundLayer;

    private float groundCheckX;
    private float wallCheckX;


    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyAnimation enemyAnimation;

    //현재 바라보는 방향
    //-1 = 왼쪽, 1 = 오른쪽
    private int direction = 1;

    public int Direction => direction;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimation = GetComponent<EnemyAnimation>();

        groundCheckX = Mathf.Abs(groundCheck.localPosition.x);
        wallCheckX = Mathf.Abs(wallCheck.localPosition.x);
    }

    private void Start()
    {
        //플레이어를 직접 연결하지 않았을 경우 자동 탐색
        if (player == null)
        {
            Player foundPlayer = FindFirstObjectByType<Player>();

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }
    }

    private void OnEnable()
    {
        direction = -1;
        SetDirection(direction);
    }

    private void FixedUpdate()
    {
        if (health == null || rb == null) return;

        if (health.IsDead)
        {
            StopMovement();
            return;
        }

        if (health.IsHitStunned)  return;

        UpdateAI();
        Move();
    }

    private void UpdateAI()
    {
        //플레이어가 감지 범위 안이면 추적
        if (IsPlayerInRange())
        {
            ChasePlayer();
            return;
        }

        // 평상시 패트롤
        Patrol();
    }

    //왔다갔다
    private void Patrol()
    {
        // 앞에 벽이 있거나 낭떠러지라면 방향 전환
        if (IsWallAhead() || !IsGroundAhead())
        {
            FlipDirection();
        }
    }

    //플레이어 추적
    private void ChasePlayer()
    {
        /*
        if (player == null) return;

        float horizontalDifference =  player.position.x - transform.position.x;

        int targetDirection = horizontalDifference > 0f ? 1 : -1;

        //플레이어 방향으로 가다가
        //벽 또는 낭떠러지가 있다면 이동하지 않음
        if (targetDirection != direction)
        {
            SetDirection(targetDirection);
        }

        if (IsWallAhead() || !IsGroundAhead())
        {
            StopMovement();
        }
        */
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    //플레이어가 주변에 있는지 확인
    private bool IsPlayerInRange()
    {
        if (player == null) return false;

        float distance = Vector2.Distance(transform.position, player.position);

        return distance <= detectionRange;
    }

    //앞에 이동할 수 있는 땅이 있는지 확인 
    private bool IsGroundAhead()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        bool detected = hit.collider != null;

        if (groundCheckVisual != null)
        {
            groundCheckVisual.color =
                detected ? normalColor : detectedColor;
        }

        return detected;
    }

    //앞에 벽이 있는지 확인
    private bool IsWallAhead()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        bool detected = hit.collider != null;

        if (wallCheckVisual != null)
        {
            wallCheckVisual.color =
                detected ? detectedColor : normalColor;
        }

        return detected;
    }

    //방향전환
    private void FlipDirection()
    {
        SetDirection(-direction);
    }

    //방향 설정
    public void SetDirection(int newDirection)
    {
        if (newDirection == 0) return;

        direction = newDirection > 0 ? 1 : -1;

        enemyAnimation?.SetDirection(direction);

        Vector3 groundPos = groundCheck.localPosition;
        groundPos.x = groundCheckX * direction;
        groundCheck.localPosition = groundPos;

        Vector3 wallPos = wallCheck.localPosition;
        wallPos.x = wallCheckX * direction;
        wallCheck.localPosition = wallPos;
    }

    //움직임 멈추기
    public void StopMovement()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    //사망 시 AI 정지
    public void StopAI()
    {
        StopMovement();

        enabled = false;
    }


    //기즈모 설정
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        if (groundCheck != null)
        {
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position +
                Vector3.down * groundCheckDistance
            );
        }

        if (wallCheck != null)
        {
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position +
                Vector3.right *
                direction *
                wallCheckDistance
            );
        }
    }
}