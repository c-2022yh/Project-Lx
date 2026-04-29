using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 15f;

    [Header("Transformation Settings")]
    public float superSpeedMultiplier = 1.5f;
    public float superJumpMultiplier = 1.1f;
    public float animalSpeedMultiplier = 1.8f;
    public float animalJumpMultiplier = 1f;

    [Header("Dash Settings")]
    public float dashForce = 50f;
    public float dashCooldown = 0.5f;
    public float waitSecond = 0.01f;

    public float lastDashTime; //쿨타임

    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    Vector2 boxSize = new Vector2(0.7f, 0.1f); //캐릭터 너비에 맞춘 납작한 박스
    public LayerMask groundLayer;

    [Header("State Data")]
    public bool isGrounded;
    public bool isJumpPressed;
    public bool canDoubleJump;
    public Vector2 moveInput;
    private bool isFacingRight = true;

    public Vector3 initialScale;
    public Vector3 scaleAnimal = new Vector3(1.4f, 1f, 1f); //환수폼 크기 설정

    private PlayerState currentState;



    public bool dashInputPressed;
    public bool isDashing;


    public FollowingOrb orb;

    [SerializeField] private GameObject ghostPrefab; //잔상 프리펩

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        rb.freezeRotation = true;
    }

    void Start()
    {
        ChangeState(new NormalState(this));
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);
        
        if (isGrounded)
        {
            canDoubleJump = true;
        }

        //현재 상태의 Update()실행
        currentState?.DoUpdate();
    }


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        //이동 입력에 따라 방향 반전 체크
        if (moveInput.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;// 상태 변경
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            currentState?.DoJump();
    }


    public void OnDash(InputValue value)
    {
        dashInputPressed = value.isPressed;
    }



    public void OnTransformSuper(InputValue value) //E
    {
        if (value.isPressed) currentState?.OnTransformSuper();
    }

    public void OnTransformAnimal(InputValue value) //R
    {
        if (value.isPressed) currentState?.OnTransformAnimal();
    }



    public void ChangeState(PlayerState newState)
    {
        currentState = newState;
        currentState.EnterTransform();
    }




    public void DoMove(float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        //목표 속도 계산 (보정값 적용)
        float targetSpeedX = moveInput.x * (moveSpeed * speedMultiplier);

        //공중 제어 보정
        if (!isGrounded) targetSpeedX *= airControlMin;

        //가속/감속 비율 계산
        float decelVar = isGrounded ? groundDecel : airDecel;
        //가속도에도 보정값이 필요하다면 적용 (1f - decelVar가 클수록 빠릿하게 반응)
        float lerpFactor = (1f - decelVar) * accelMultiplier;
        //플레이어가 부드럽게 움직이도록 하는 과정
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, lerpFactor);
        //일정량의 작은 미끄러짐은 0으로 보정
        if (moveInput.x == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;

        //최종 이동속도 적용
        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
    }

    public void ExecuteJump(float jumpMultiplier = 1f)
    {
        //실제 물리적인 점프 실행
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpMultiplier);
    }

    public void ExecuteDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;


        if (moveInput.x == 0)
        {
            Debug.Log("<color=yellow>[Dash]</color> 방향키 입력이 없어 대쉬를 취소합니다.");
            return;
        }
        
        float dashDir = moveInput.x > 0 ? 1f : -1f;

        //Y축 속도를 0으로 만들면 공중 대쉬 시 아래로 처지지 않고 일직선으로 쇄도함
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0f);

        lastDashTime = Time.time;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(dashDir));
    }

    private IEnumerator DashRoutine(float dir)
    {
        isDashing = true;

        float originalGravity = rb.gravityScale; //중력 값
        float originalDrag = rb.linearDamping; //공기 저항(마찰)

        rb.gravityScale = 0f; //중력 0
        rb.linearDamping = 0f; //공기저항 0
        //대쉬 처음부터 끝까지 같은 속도로 날아가게 함


        // --- [수정] 대쉬 시작 지점에 잔상 딱 하나 생성 ---
        GameObject ghost = GhostPooler.Instance.GetGhost();
        if (ghost != null)
        {
            ghost.SetActive(true);
            ghost.GetComponent<GhostEffect>().Init(
                GetComponent<SpriteRenderer>().sprite,
                transform.position,
                transform.rotation,
                transform.localScale
            );
        }

        //고정 이동 루프
        float timer = 0f;
        while (timer < waitSecond)
        {
            rb.linearVelocity = new Vector2(dir * dashForce, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
        rb.linearDamping = originalDrag; 
        //변수 원래대로 정상화


        isDashing = false;

    }




    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.blue : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, boxSize);
        }
    }



}
