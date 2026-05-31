using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    //컴포넌트
    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    //플레이어 상태 처리
    [Header("State Data")]
    public bool isGrounded;
    public Vector2 moveInput;
    public bool isFacingRight = true;

    //땅에 닿았는지 확인하는 변수
    [Header("Ground Check")]
    public Transform groundCheck;
    Vector2 boxSize = new Vector2(0.7f, 0.1f); //캐릭터 너비에 맞춘 납작한 박스
    public LayerMask groundLayer;

    //각성 시 이동속도 변환
    [Header("Transformation Settings")]
    public float superSpeedMultiplier = 1.5f;
    public float superJumpMultiplier = 1.1f;
    public float animalSpeedMultiplier = 1.8f;
    public float animalJumpMultiplier = 1f;





    [SerializeField] private GameObject ghostPrefab; //잔상 프리펩
    public Vector3 initialScale;
    public Vector3 scaleAnimal = new Vector3(1.4f, 1f, 1f); //환수폼 크기 설정

    
    //중력 값을 저장하는 변수 freeze 함수 내부에서 사용
    private float originalGravity;
    private float originalDrag;
    
    
    //컴포넌트 참조
    private PlayerState currentState;
    private PlayerMove playerMove;
    private PlayerAttack playerAttack;
    private PlayerSkill playerSkill;
    private PlayerEnergy playerEnergy;

    public PlayerActionState playerActionState;


    //처음 한번만 실행하는 함수
    //변수 초기화
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        rb.freezeRotation = true;

        //스크립트 연결
        playerMove = GetComponent<PlayerMove>();
        playerAttack = GetComponent<PlayerAttack>();
        playerSkill = GetComponent<PlayerSkill>();
        playerEnergy = GetComponent<PlayerEnergy>();
        playerActionState = GetComponent<PlayerActionState>();

        ChangeState(new NormalState(this));
    }

    void Update()
    {

    }
    void FixedUpdate()
    {
        //땅에 닿았는지 체크
        isGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);

        playerMove.ExecuteMove(this);
        playerMove.ExecuteJump(this);

        //currentState?.DoFixedUpdate();
    }


    //인풋시스템과 연결
    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); } //방향값 설정
    
    public void OnJump(InputValue value) { if (value.isPressed && playerActionState.CanJump()) playerMove.RequestJump(); }
    public void OnDash(InputValue value) { if (value.isPressed && playerActionState.CanDash()) playerMove.ExecuteDash(this); }
    public void OnAttack(InputValue value) { if (value.isPressed && playerActionState.CanAttack()) playerAttack.ExecuteAttack(this); }

    public void OnSkillX(InputValue value) { if (value.isPressed && playerActionState.CanSkill()) playerSkill.ExecuteSkillX(this); }
    public void OnSkillA(InputValue value) { if (value.isPressed && playerActionState.CanSkill()) playerSkill.ExecuteSkillA(this); }
    public void OnSkillS(InputValue value) { if (value.isPressed && playerActionState.CanSkill()) playerSkill.ExecuteSkillS(this); }
    public void OnSkillD(InputValue value) { if (value.isPressed && playerActionState.CanSkill()) playerSkill.ExecuteSkillD(this); }
    public void OnSkillF(InputValue value) { if (value.isPressed && playerActionState.CanSkill()) playerSkill.ExecuteSkillF(this); }

    public void OnAwaken(InputValue value) { if (value.isPressed && playerActionState.CanAwakening()) currentState?.OnTransformSuper(); }
    //public void OnTransformSuper(InputValue value) { if (value.isPressed && playerActionState.CanTransform()) currentState?.OnTransformSuper(); }
    //public void OnTransformAnimal(InputValue value) { if (value.isPressed && playerActionState.CanTransform()) currentState?.OnTransformAnimal(); }

    


    public void ChangeState(PlayerState newState)
    {
        currentState?.ExitTransform();
        currentState = newState;
        currentState.EnterTransform();
    }






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
