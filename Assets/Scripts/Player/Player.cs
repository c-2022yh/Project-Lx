using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerTransformation;

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
    public Vector3 initialScale;

    private PlayerState currentState;

    public bool dashInputPressed;
    public bool isDashing;


    public FollowingOrb orb;

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


    public void OnDash(InputValue value)
    {
        // 버튼을 누른 순간 true, 떼면 false가 됨
        dashInputPressed = value.isPressed;
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

    public void ExecuteDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;


        if (moveInput.x == 0)
        {
            Debug.Log("<color=yellow>[Dash]</color> 방향키 입력이 없어 대쉬를 취소합니다.");
            return;
        }
        
        float dashDir = moveInput.x > 0 ? 1f : -1f;

        // 2. 물리 적용
        // Y축 속도를 0으로 만들면 공중 대쉬 시 아래로 처지지 않고 일직선으로 쇄도함
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0f);

        lastDashTime = Time.time;

        // 3. (선택 사항) 대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(dashDir));
    }

    private IEnumerator DashRoutine(float dir)
    {
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // 텔레포트 느낌: 폭발적인 속도 주입
        // dashForce를 인스펙터에서 50~100 정도로 높게 잡아보세요.
        rb.linearVelocity = new Vector2(dir * dashForce, 0f);

        Debug.Log("<color=yellow>[Dash]</color> " + dir);
        // 0.1초만 아주 짧게 '촥' 이동
        yield return new WaitForSeconds(0.025f);

        // 핵심: 여기서 속도를 0으로 빡 잡아줘야 '스르륵' 안 가고 텔레포트처럼 멈춤
        rb.linearVelocity = Vector2.zero;

        rb.gravityScale = originalGravity;

        
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

/*


using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    //상태 정의 (일반, 변신1, 변신2)
    public enum PlayerState { Normal, Transform1, Transform2 }
    public PlayerState currentState = PlayerState.Normal;

    private Vector3 initialScale;

    [Header("변신 비주얼 설정")]
    public Color normalColor = Color.green;

    public Color colorT1 = Color.red;
    public Color colorT2 = Color.blue;
    public Vector3 scaleT2 = new Vector3(1.4f, 0.7f, 1f); //변신2 크기 설정

    private bool isTransformed = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 시작 시 기본 색상 적용
        spriteRenderer.color = normalColor;
        initialScale = transform.localScale;
    }

    //변신1 -> E
    public void OnTransform1(InputValue value)
    {
        if (value.isPressed)
        {
            //이미 변신1이면 일반으로, 아니면 변신1로
            SetState(currentState == PlayerState.Transform1 ? PlayerState.Normal : PlayerState.Transform1);
        }
    }

    //변신1 -> R
    public void OnTransform2(InputValue value)
    {
        if (value.isPressed)
        {
            //이미 변신2면 일반으로, 아니면 변신2로
            SetState(currentState == PlayerState.Transform2 ? PlayerState.Normal : PlayerState.Transform2);
        }
    }




    //상태 변경 함수
    private void SetState(PlayerState newState)
    {
        currentState = newState;

        //초기화
        spriteRenderer.color = Color.green;
        transform.localScale = initialScale;

        switch (currentState)
        {
            case PlayerState.Normal:
                UnityEngine.Debug.Log("일반 모드 복귀");
                break;

            case PlayerState.Transform1:
                spriteRenderer.color = colorT1;
                UnityEngine.Debug.Log("변신 1 완료!");
                break;

            case PlayerState.Transform2:
                spriteRenderer.color = colorT2;
                transform.localScale = scaleT2;
                UnityEngine.Debug.Log("변신 2 완료!");
                break;
        }
    }


    public bool CanIDoubleJump()
    {
        return currentState == 1; // 1번 모드일 때만 true 반환
    }

}using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f; //이동속도
    public float jumpForce = 15f; //점프력

    [Range(0, 1)] public float airControlMin = 0.8f; //공중에서 이동속도 보정값 (이 값이 1이면 비행기처럼 움직임)

    [Header("가감속")]
    [Range(0f, 0.3f)] public float groundDecel = 0.01f; //바닥 마찰계수
    [Range(0f, 0.5f)] public float airDecel = 0.1f; //공중 마찰계수

    [Header("바닥 판정 설정")]
    public Transform groundCheck;
    public float checkRadius = 0.2f; //체크할 범위
    public LayerMask groundLayer; //Ground 확인

    private Rigidbody2D rb;
    private Vector2 moveInput; //xy인풋

    private bool isGrounded;
    private bool isJumpPressed = false; //점프키가 눌려있는지?
    private bool canDoubleJump; //더블 점프 가능 여부

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }




    void Update()
    {
        //땅에 닿아 있는지 확인하는 변수
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

    }

    void FixedUpdate()
    {
        //이동 관련
        float targetSpeedX = moveInput.x * moveSpeed;
        if (!isGrounded) targetSpeedX *= airControlMin;

        //플레이어가 부드럽게 움직이도록 하는 과정
        //그냥 += moveSpeed하면 딱딱하게 움직임
        float decelVar = isGrounded ? groundDecel : airDecel;
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, 1f - decelVar);

        //일정량의 작은 미끄러짐은 0으로 보정
        if (moveInput.x == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;
        //최종 이동속도 적용
        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);




        //점프 관련
        if (isGrounded)
        {
            canDoubleJump = true; // 땅에 닿으면 더블 점프 충전

            // 1. 꾹 누르고 있으면 지면에서 무한 점프
            if (isJumpPressed && rb.linearVelocity.y <= 0.1f)
            {
                ExecuteJump();
            }
        }


    }

    public void OnMove(InputValue value) //이동 (Move 액션과 연결)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value) //점프 (Jump 액션과 연결)
    {
        isJumpPressed = value.isPressed;

        //추가로 점프버튼을 누르는 순간+ 각성상태 + 공중 + 더블점프 가능이면 더블점프
        if (value.isPressed && currentState == PlayerState.Transform1 && !isGrounded && canDoubleJump)
        {
            ExecuteJump();
            canDoubleJump = false;
            isJumpPressed = false;
        }


    }

    void ExecuteJump() //점프 실행
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }





    //바닥 체크 범위를 확인하는 기즈모
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.blue : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }

*/