using System.Collections;
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

    [Header("Dash Settings")]
    public float dashForce = 200f;
    public float dashCooldown = 0.5f;
    public float waitSecond = 0.01f;

    [Header("Transformation Settings")]
    public float superSpeedMultiplier = 1.5f;
    public float superJumpMultiplier = 1.1f;
    public float animalSpeedMultiplier = 1.8f;
    public float animalJumpMultiplier = 1f;

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
    public bool dashInputPressed;
    public bool isDashing;
    public bool isAttacking;
    public bool isFacingRight = true;

    public Vector3 initialScale;
    public Vector3 scaleAnimal = new Vector3(1.4f, 1f, 1f); //환수폼 크기 설정

    //컴포넌트 참조
    private PlayerState currentState;
    private PlayerMove playerMove;
    private PlayerAttack playerAttack;

    public FollowingOrb orb;
    [SerializeField] private GameObject ghostPrefab; //잔상 프리펩

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        rb.freezeRotation = true;
        playerMove = GetComponent<PlayerMove>();
        playerAttack = GetComponent<PlayerAttack>();

        ChangeState(new NormalState(this));
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);
        
        if (isGrounded) canDoubleJump = true;
        
        //현재 상태의 Update()실행
        currentState?.DoUpdate();
    }

    public void DoMove(float sMult = 1f, float aMult = 1f) => playerMove.DoMove(this, sMult, aMult);
    public void ExecuteJump(float jMult = 1f) => playerMove.ExecuteJump(this, jMult);
    public void ExecuteDash() => playerMove.ExecuteDash(this);

    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    public void OnJump(InputValue value) { if (value.isPressed) currentState?.DoJump(); }
    public void OnDash(InputValue value) { dashInputPressed = value.isPressed; }
    public void OnAttack(InputValue value) { if (value.isPressed && !isAttacking) playerAttack.ExecuteAttack(this); }
    public void OnTransformSuper(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformSuper(); }
    public void OnTransformAnimal(InputValue value) { if (value.isPressed && !isAttacking) currentState?.OnTransformAnimal(); }
    
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
