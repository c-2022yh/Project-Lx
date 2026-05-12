using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    //컴포넌트
    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    //이동속도 세팅
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 15f;

    //대쉬관련 세팅
    [Header("Dash Settings")]
    public float dashForce = 2f;
    public float dashCooldown = 0.5f;
    public float waitSecond = 0.01f;

    //각성 시 이동속도 변환
    [Header("Transformation Settings")]
    public float superSpeedMultiplier = 1.5f;
    public float superJumpMultiplier = 1.1f;
    public float animalSpeedMultiplier = 1.8f;
    public float animalJumpMultiplier = 1f;

    public float lastDashTime; //대쉬 내부쿨 관련 변수

    //공중에서 마찰계수 정하기
    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    //땅에 닿았는지 확인하는 변수
    [Header("Ground Check")]
    public Transform groundCheck;
    Vector2 boxSize = new Vector2(0.7f, 0.1f); //캐릭터 너비에 맞춘 납작한 박스
    public LayerMask groundLayer;

    //플레이어 상태 처리
    [Header("State Data")]
    public bool isGrounded;
    public bool isJumpPressed;
    public bool canDoubleJump;
    public Vector2 moveInput;
    public bool dashInputPressed;
    public bool isDashing = false;
    public bool isAttacking = false;
    public bool isSkillActive = false;
    public AttackPattern currentAttackPattern; //현재 공격패턴 정보
    public bool isFacingRight = true;

    public Vector3 initialScale;
    public Vector3 scaleAnimal = new Vector3(1.4f, 1f, 1f); //환수폼 크기 설정

    //컴포넌트 참조
    private PlayerState currentState;
    private PlayerMove playerMove;
    private PlayerAttack playerAttack;
    private PlayerSkill playerSkill;

    //플레이어를 따라다니는 보주
    public FollowingOrb orb;
    [SerializeField] private GameObject ghostPrefab; //잔상 프리펩

    //중력 값을 저장하는 변수 freeze 함수 내부에서 사용
    private float originalGravity;
    private float originalDrag;

    //처음 한번만 실행하는 함수
    //변수 초기화
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        rb.freezeRotation = true;
        playerMove = GetComponent<PlayerMove>();
        playerAttack = GetComponent<PlayerAttack>();
        playerSkill = GetComponent<PlayerSkill>();

        ChangeState(new NormalState(this));
    }

    void Update()
    {
        if (isGrounded) canDoubleJump = true;
        currentState?.DoUpdate(); //현재 상태의 Update()실행
    }
    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);
        currentState?.DoFixedUpdate();
    }


    //함수 연결
    public void DoMove(float sMult = 1f, float aMult = 1f) => playerMove.DoMove(this, sMult, aMult);
    public void ExecuteJump(float jMult = 1f) => playerMove.ExecuteJump(this, jMult);
    public void ExecuteDash() => playerMove.ExecuteDash(this);

    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    public void OnJump(InputValue value) { if (value.isPressed && !isSkillActive) currentState?.DoJump(); }
    public void OnDash(InputValue value) { dashInputPressed = value.isPressed; }
    public void OnAttack(InputValue value) { if (value.isPressed && !isAttacking) playerAttack.ExecuteAttack(this); }
    public void OnTransformSuper(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformSuper(); }
    public void OnTransformAnimal(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformAnimal(); }
    public void OnSkillA(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillA(this); }
    public void OnSkillS(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillS(this); }
    public void OnSkillD(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillD(this); }
    public void OnSkillF(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillF(this); }


    public void ChangeState(PlayerState newState)
    {
        currentState?.ExitTransform();
        currentState = newState;
        currentState.EnterTransform();
    }


    //
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.blue : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, boxSize);
        }
    }

    //중력 일시정으로 정지시키는 함수
    public void SetPhysicsFreeze(bool freeze)
    {
        if (freeze)
        {
            //중력 일시정지
            originalGravity = rb.gravityScale;
            originalDrag = rb.linearDamping;

            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            //상태 복구
            rb.gravityScale = originalGravity;
            rb.linearDamping = originalDrag;
            rb.linearVelocity = Vector2.zero;

        }
    }


}
