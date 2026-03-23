using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f; //이동속도
    public float jumpForce = 15f; //점프력
    [Range(0, 1)] public float airControlMin = 0.8f; //공중에서 좌우로 움직이는 비율

    [Header("가감속 (핵심)")]
    //감속 계수
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    Rigidbody2D rb;
    float xInput;
    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; //네모가 뒤집어지지 않게

    }

    void Update()
    {
        //좌우 값 받기
        xInput = Input.GetAxisRaw("Horizontal");

        //바닥 판정 y축의 속도 변화가 없으면(바닥에 붙어있을 때)
        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        //점프
        if (Input.GetKey(KeyCode.C) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate() // 물리 계산은 여기서 해야 쫀득함이 살아납니다.
    {
        //이동
        float targetSpeedX = xInput * moveSpeed;

        //공중 제어 적용
        if (!isGrounded)
        {
            targetSpeedX *= airControlMin;
        }

        //Lerp 사용, 플레이어가 멈춰있다 움직일때 부드럽게 움직이도록
        float decelVar = isGrounded ? groundDecel : airDecel;
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, 1f - decelVar);

        //미끄러짐 방지 조금의 미끄러짐은 바로 멈추게
        if (xInput == 0 && Mathf.Abs(newSpeedX) < 0.1f)
        {
            newSpeedX = 0f;
        }

        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
    }
}