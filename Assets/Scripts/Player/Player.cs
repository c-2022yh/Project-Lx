using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

//플레이어 전체 통제하는 스크립트
public class Player : MonoBehaviour
{
    //컴포넌트
    [Header("Components")]
    public Rigidbody2D rb { get; private set; }
    [SerializeField] private SpriteRenderer spriteRenderer;
    public SpriteRenderer sr => spriteRenderer;

    //플레이어 상태 처리
    [Header("State Data")]
    public bool isGrounded;
    public Vector2 moveInput;
    public bool isFacingRight = true;

    //땅에 닿았는지 확인하는 변수
    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    [SerializeField, Range(0f, 80f)] private float maxGroundAngle = 50f;
    private readonly List<ContactPoint2D> groundContacts = new List<ContactPoint2D>(16);

    //중력 값을 저장하는 변수 freeze 함수 내부에서 사용
    private float originalGravity;
    private float originalDrag;

    //현재 상호작용 가능한 오브젝트 참조
    private IInteractable currentInteractable;

    


    //컴포넌트 참조
    public PlayerState currentState { get; private set; }
    public PlayerMove Move { get; private set; }
    public PlayerAttack Attack { get; private set; }
    public PlayerSkill Skill { get; private set; }
    public PlayerHealth Health { get; private set; }
    public PlayerHitReaction HitReaction { get; private set; }
    public PlayerRespawn Respawn { get; private set; }
    public PlayerEnergy Energy { get; private set; }
    public PlayerAwakening Awakening { get; private set; }
    public PlayerActionState ActionState { get; private set; }
    public PlayerAnimation Animation { get; private set; }
    public PlayerStats Stats { get; private set; }


    //처음 한번만 실행하는 함수
    //변수 초기화
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        //스크립트 연결
        Move = GetComponent<PlayerMove>();
        Attack = GetComponent<PlayerAttack>();
        Skill = GetComponent<PlayerSkill>();
        Energy = GetComponent<PlayerEnergy>();
        Health = GetComponent<PlayerHealth>();
        HitReaction = GetComponent<PlayerHitReaction>();
        Respawn = GetComponent<PlayerRespawn>();
        Awakening = GetComponent<PlayerAwakening>();
        ActionState = GetComponent<PlayerActionState>();
        Stats = GetComponent<PlayerStats>();
        Animation = GetComponent<PlayerAnimation>();

        ChangeState(new NormalState(this));
    }

    void Update()
    {

    }
    void FixedUpdate()
    {
        //땅에 닿았는지 체크
        isGrounded = CheckGrounded();

        Move.ExecuteMove();
        Move.ExecuteJump();

        //currentState?.DoFixedUpdate();
    }

    private bool CheckGrounded()
    {
        //점프 직후 남아 있는 접촉으로 착지/공중 점프가 다시 충전되지 않게 한다.
        groundContacts.Clear();
        if (rb.linearVelocity.y > 0.01f) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        rb.GetContacts(filter, groundContacts);

        float minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        foreach (ContactPoint2D contact in groundContacts)
        {
            //같은 Ground 타일이어도 옆면/천장은 착지로 인정하지 않는다.
            if (contact.normal.y >= minGroundNormalY &&
                !Move.IsIgnoringPlatform(GetGroundCollider(contact))) return true;
        }

        return false;
    }

    private Collider2D GetGroundCollider(ContactPoint2D contact)
    {
        return contact.collider.attachedRigidbody == rb ? contact.otherCollider : contact.collider;
    }

    public bool TryGetDropPlatforms(List<Collider2D> platforms)
    {
        platforms.Clear();
        if (!isGrounded) return false;

        float minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        foreach (ContactPoint2D contact in groundContacts)
        {
            if (contact.normal.y < minGroundNormalY) continue;
            Collider2D platform = GetGroundCollider(contact);
            if (Move.IsIgnoringPlatform(platform)) continue;
            PlatformEffector2D effector = platform.GetComponent<PlatformEffector2D>();
            //일반 바닥도 동시에 밟고 있다면 하단 점프를 허용하지 않는다.
            if (!platform.usedByEffector || effector == null ||
                !effector.isActiveAndEnabled || !effector.useOneWay)
            {
                platforms.Clear();
                return false;
            }
            if (!platforms.Contains(platform)) platforms.Add(platform);
        }
        return platforms.Count > 0;
    }


    //인풋시스템과 연결
    public void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); } //방향값 설정
    
    public void OnJump(InputValue value) { if (value.isPressed && ActionState.CanJump()) Move.RequestJump(); }
    public void OnDash(InputValue value) { if (value.isPressed && ActionState.CanDash()) Move.ExecuteDash(); }
    public void OnAttack(InputValue value) { if (value.isPressed && ActionState.CanAttack()) Attack.ExecuteAttack(); }

    public void OnSkillX(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillX(this); }
    public void OnSkillA(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillA(this); }
    public void OnSkillS(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillS(this); }
    public void OnSkillD(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillD(this); }
    public void OnSkillF(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillF(this); }

    //평상시 Q는 각성을 시도하고, 각성 중 Q는 폭주 유물의 초토화를 시도한다.
    //초토화는 PlayerSkill 내부에서 별도로 실행하므로 X/A/S/D/F 슬롯을 차지하지 않는다.
    public void OnAwaken(InputValue value)
    {
        if (!value.isPressed || Awakening == null || ActionState == null) return;

        if (Awakening.IsAwakened)
        {
            Skill?.TryUseAwakeningSkill(this);
            return;
        }

        if (ActionState.CanAwakening()) Awakening.TryAwaken(this);
    }

    //public void OnTransformSuper(InputValue value) { if (value.isPressed && ActionState.CanTransform()) currentState?.OnTransformSuper(); }
    //public void OnTransformAnimal(InputValue value) { if (value.isPressed && ActionState.CanTransform()) currentState?.OnTransformAnimal(); }

    public void OnInteract() { currentInteractable?.Interact(this); }


    public void ChangeState(PlayerState newState)
    {
        currentState?.ExitTransform();
        currentState = newState;
        currentState.EnterTransform();
    }
    

    private void OnDrawGizmos()
    {
        float minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        foreach (ContactPoint2D contact in groundContacts)
        {
            Gizmos.color = contact.normal.y >= minGroundNormalY ? Color.blue : Color.red;
            Gizmos.DrawRay(contact.point, contact.normal * 0.4f);
        }
    }

    private bool isPhysicsFrozen = false;

    // 중력 일시정지 함수
    public void SetPhysicsFreeze(bool freeze)
    {
        if (freeze)
        {
            if (isPhysicsFrozen) return;

            isPhysicsFrozen = true;

            originalGravity = rb.gravityScale;
            originalDrag = rb.linearDamping;

            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            if (!isPhysicsFrozen) return;

            isPhysicsFrozen = false;

            rb.gravityScale = originalGravity;
            rb.linearDamping = originalDrag;
            rb.linearVelocity = Vector2.zero;
        }
    }

    //현재 상호작용 가능한 오브젝트 설정
    public void SetInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }

    public void ClearInteractable(IInteractable interactable)
    {
        if (currentInteractable == interactable)
        {
            currentInteractable = null;
        }
    }

}
