using UnityEngine;
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

    Rigidbody2D rb;
    float xInput;
    float yInput;
    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; //사각형 모양 고정

    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");    //좌우 입력
        yInput = Input.GetAxisRaw("Vertical");      //상하 입력

        //땅에 닿아 있는지 확인하는 변수
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        //점프 조건 1.바닥에 닿아 있고, 2.위로 올라가는 중이 아닐 때만(y 속도가 0 이하)
        if (Input.GetKey(KeyCode.C) && isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

    }

    void FixedUpdate()
    {
        float targetSpeedX = xInput * moveSpeed;
        if (!isGrounded) targetSpeedX *= airControlMin;


        //플레이어가 부드럽게 움직이도록 하는 과정
        //그냥 += moveSpeed하면 딱딱하게 움직임
        float decelVar = isGrounded ? groundDecel : airDecel;
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, 1f - decelVar);

        //일정량의 작은 미끄러짐은 0으로 보정
        if (xInput == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;

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
