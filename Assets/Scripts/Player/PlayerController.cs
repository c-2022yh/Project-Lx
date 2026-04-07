using UnityEngine;
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
    public LayerMask groundLayer; //인스펙터에서 레이어 지정해야함


    // 내부 변수
    private Rigidbody2D rb;
    private Vector2 moveInput; //xInput, yInput을 하나로 관리
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }


    public void OnMove(InputValue value) //이동 (Move 액션과 연결)
    {
        moveInput = value.Get<Vector2>();
    }
    
    public void OnJump(InputValue value) //점프 (Jump 액션과 연결)
    {
        //버튼을 눌렀을 때(isPressed) + 바닥일 때 + 낙하 중이 아닐 때
        if (value.isPressed && isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void Update()
    {
        //땅에 닿아 있는지 확인하는 변수
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
       
    }

    void FixedUpdate()
    {
        float targetSpeedX = moveInput.x * moveSpeed;
        if (!isGrounded) targetSpeedX *= airControlMin;


        //플레이어가 부드럽게 움직이도록 하는 과정
        //그냥 += moveSpeed하면 딱딱하게 움직임
        float decelVar = isGrounded ? groundDecel : airDecel;
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, 1f - decelVar);

        //일정량의 작은 미끄러짐은 0으로 보정
        if (moveInput.x == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;

        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
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

    
}
