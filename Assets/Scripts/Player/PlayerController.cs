using UnityEngine;
using System.Collections;

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
        float yInput = Input.GetAxisRaw("Vertical"); // 위아래 입력 받기

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 수정된 조건: 바닥에 닿아 있고 + 동시에 위로 올라가는 중이 아닐 때만(y 속도가 0 이하) 점프 허용
        if (Input.GetKey(KeyCode.C) && isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }


        // 하단 점프: 바닥에 있고 + 아래 방향키(S)를 누르면서 + 점프(C)를 눌렀을 때
        if (isGrounded && yInput < 0 && Input.GetKeyDown(KeyCode.C))
        {
            StartCoroutine(DisableCollision());
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

    IEnumerator DisableCollision()
    {
        // 현재 내가 딛고 있는 발판의 PlatformEffector2D를 찾아서 잠시 끕니다.
        // 레이어 설정이 복잡할 때 가장 확실한 방법입니다.

        // 발판 레이어(Ground)와의 충돌을 잠시 무시
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Ground"), true);

        yield return new WaitForSeconds(0.3f); // 통과할 시간 확보

        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Ground"), false);
    }
}
