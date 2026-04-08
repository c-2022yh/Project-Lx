using System.Diagnostics;
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

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashCooldown = 0.5f;
    public float lastDashTime; //쿨타임

    //더블 탭(따닥) 판정용
    public float lastTapTime;
    public Vector2 lastInput;
    public Vector2 lastDirection;

    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    Vector2 boxSize = new Vector2(0.7f, 0.1f); // 캐릭터 너비에 맞춘 납작한 박스
    public LayerMask groundLayer;

    [Header("State Data")]
    public bool isGrounded;
    public bool isJumpPressed;
    public bool canDoubleJump;
    public Vector2 moveInput;
    public Vector3 initialScale;

   private PlayerState currentState;


    void awake()
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
        // 공통 데이터 갱신
        isGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);
        
        if (isGrounded)
        {
            canDoubleJump = true;
        }


        // 현재 상태의 Update 로직 실행 (이동 가감속 등)
        currentState?.DoUpdate();
    }


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        // 누를 때만 상태에게 점프 명령 전달
        if (value.isPressed)
            currentState?.DoJump();
    }

    public void OnTransformSuper(InputValue value) // E 키
    {
        if (value.isPressed) currentState?.OnTransformSuper();
    }

    public void OnTransformAnimal(InputValue value) // R 키
    {
        if (value.isPressed) currentState?.OnTransformAnimal();
    }



    public void ChangeState(PlayerState newState)
    {
        // 새로운 상태 주입 후 시작 로직(색상 변경 등) 실행
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
        // 실제 물리적인 점프 실행
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpMultiplier);
    }

    public void ExecuteDash(Vector2 direction)
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        // 대쉬 시 순간적으로 속도 고정 (Y축은 유지하거나 0으로)
        rb.linearVelocity = new Vector2(direction.x * dashForce, rb.linearVelocity.y);
        lastDashTime = Time.time;
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
