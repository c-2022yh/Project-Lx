using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f;
    public float jumpForce = 15f;
    [Range(0, 1)] public float airControlMin = 0.8f;

    [Header("가감속")]
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    [Header("바닥 판정 설정")]
    public Transform groundCheck; // 발밑에 빈 오브젝트 하나 만들어서 넣어주세요.
    public float checkRadius = 0.2f; // 체크할 범위
    public LayerMask groundLayer; // Ground 레이어 지정 필수!

    Rigidbody2D rb;
    float xInput;
    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        // 핵심 변경 부분: 물리적으로 바닥 레이어와 접촉 중인지 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 점프
        if (Input.GetKey(KeyCode.C) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        float targetSpeedX = xInput * moveSpeed;
        if (!isGrounded) targetSpeedX *= airControlMin;

        float decelVar = isGrounded ? groundDecel : airDecel;
        float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeedX, 1f - decelVar);

        if (xInput == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;

        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
    }

    // 에디터 뷰에서 바닥 체크 범위를 시각적으로 확인
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}