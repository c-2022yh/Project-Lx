using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

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

    //플레이어를 따라다니는 보주
    public FollowingOrb orb;
    [SerializeField] private GameObject ghostPrefab; //잔상 프리펩

    

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


    /// ----------------------------------------------------
    ///  [3단계 아키텍처로 함수 구현]
    ///  On어쩌구        InputSystem연결
    ///  Process어쩌구   로직과 동작 차단 if문
    ///  Execute어쩌구   실제 velocity값 조절   
    ///  ----------------------------------------------------

    //On
    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); } //방향값 설정
    public void OnJump(InputValue value) { isJumpPressed = value.isPressed; } //Move와 같게 설정
   
    public void OnDash(InputValue value) { dashInputPressed = value.isPressed; }
    public void OnAttack(InputValue value) { if (value.isPressed && !isAttacking) playerAttack.ExecuteAttack(this); }
    public void OnTransformSuper(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformSuper(); }
    public void OnTransformAnimal(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformAnimal(); }
    public void OnSkillA(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillA(this); }
    public void OnSkillS(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillS(this); }
    public void OnSkillD(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillD(this); }
    public void OnSkillF(InputValue value) { if (value.isPressed) playerSkill.ExecuteSkillF(this); }
    
    //Process
    public void ProcessMove(float sMult = 1f, float aMult = 1f) => playerMove.ProcessMove(this, sMult, aMult);
    public void ProcessJump(float jMult = 1f) => playerMove.ProcessJump(this, jMult);


    //Execute
    public void ExecuteDash() => playerMove.ExecuteDash(this);



    

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



}
