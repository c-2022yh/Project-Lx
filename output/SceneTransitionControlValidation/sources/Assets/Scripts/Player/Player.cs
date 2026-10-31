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

    [Header("Input Buffer")]
    [Tooltip("Seconds to remember a basic attack pressed before the current action finishes. Jump uses PlayerMove > Buffer Time.")]
    [SerializeField, Min(0f)] private float attackBufferTime = 0.15f;
    private float lastAttackRequestTime = float.NegativeInfinity;

    //땅에 닿았는지 확인하는 변수
    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    [SerializeField, Range(0f, 80f)] private float maxGroundAngle = 50f;
    [Tooltip("Short downward cast used when physical ground contacts are missing.")]
    [SerializeField, Min(0f)] private float groundProbeDistance = 0.08f;
    [Tooltip("Allow small upward motion for the fallback probe, but not airborne ascent.")]
    [SerializeField, Min(0f)] private float groundProbeMaxUpwardSpeed = 0.5f;
    [Tooltip("Minimum time to ignore grounding after a jump. Ascent stays excluded afterward.")]
    [SerializeField, Min(0f)] private float jumpGroundIgnoreDuration = 0.08f;
    private Collider2D[] bodyColliders;
    private readonly List<RaycastHit2D> groundProbeHits = new List<RaycastHit2D>(16);
    private readonly List<Collider2D> supportingGround = new List<Collider2D>(4);
    private bool jumpGroundingBlocked;
    private float ignoreGroundUntil;
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
        bodyColliders = GetComponentsInChildren<Collider2D>();

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
        Move.UpdateDropCollisions();
        //땅에 닿았는지 체크
        isGrounded = CheckGrounded();
        Move.TickMovementAudio();

        if (SceneFader.IsPlayerControlBlocked)
        {
            ClearBufferedActionInputs();
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            Move.ExecuteMove();
            ProcessBufferedAttack();
            Move.ExecuteJump();
        }
        Move.ApplyApexGravity();
        Move.ApplyFallSpeedLimit();

        //currentState?.DoFixedUpdate();
    }

    public void NotifyJumpStarted()
    {
        isGrounded = false;
        jumpGroundingBlocked = true;
        ignoreGroundUntil = Time.time + jumpGroundIgnoreDuration;
        supportingGround.Clear();
    }

    private bool CheckGrounded()
    {
        groundContacts.Clear();
        supportingGround.Clear();

        // Explicit jumps cannot re-ground from residual contacts or the nearby floor.
        if (jumpGroundingBlocked)
        {
            if (Time.time < ignoreGroundUntil || rb.linearVelocity.y > 0.01f) return false;
            jumpGroundingBlocked = false;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        rb.GetContacts(filter, groundContacts);

        float minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        foreach (ContactPoint2D contact in groundContacts)
        {
            Collider2D ground = GetGroundCollider(contact);
            if (contact.normal.y >= minGroundNormalY && !Move.IsIgnoringPlatform(ground))
            {
                AddSupportingGround(ground);
            }
        }

        // Real contacts also count while moving upward along a slope.
        if (supportingGround.Count > 0) return true;
        if (groundProbeDistance <= 0f || rb.linearVelocity.y > groundProbeMaxUpwardSpeed)
            return false;

        foreach (Collider2D body in bodyColliders)
        {
            if (!body.isActiveAndEnabled || body.isTrigger || body.attachedRigidbody != rb)
                continue;

            groundProbeHits.Clear();
            body.Cast(Vector2.down, filter, groundProbeHits, groundProbeDistance, true);
            foreach (RaycastHit2D hit in groundProbeHits)
            {
                // Initial overlaps have a synthetic normal opposite the cast direction.
                // They must not make the underside of a one-way platform into ground.
                if (hit.collider == null || hit.distance <= 0f || hit.normal.y < minGroundNormalY ||
                    Move.IsIgnoringPlatform(hit.collider) || Physics2D.GetIgnoreCollision(body, hit.collider))
                    continue;

                AddSupportingGround(hit.collider);
            }
        }

        return supportingGround.Count > 0;
    }

    private void AddSupportingGround(Collider2D ground)
    {
        if (!supportingGround.Contains(ground)) supportingGround.Add(ground);
    }

    private Collider2D GetGroundCollider(ContactPoint2D contact)
    {
        return contact.collider.attachedRigidbody == rb ? contact.otherCollider : contact.collider;
    }

    // Read-only audio query; grounding and one-way platform decisions remain in CheckGrounded.
    public bool TryGetGroundSupport(out Collider2D ground, out Vector2 point)
    {
        ground = null;
        point = groundCheck != null ? (Vector2)groundCheck.position : rb.position;
        if (!isGrounded || supportingGround.Count == 0) return false;
        float closest = float.PositiveInfinity;
        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        foreach (ContactPoint2D contact in groundContacts)
        {
            Collider2D candidate = GetGroundCollider(contact);
            if (contact.normal.y < minNormalY || !supportingGround.Contains(candidate)) continue;
            float horizontalDistance = Mathf.Abs(contact.point.x - rb.position.x);
            if (horizontalDistance >= closest) continue;
            closest = horizontalDistance;
            ground = candidate;
            point = contact.point;
        }
        if (ground != null) return true;
        ground = supportingGround[0];
        point = ground.ClosestPoint(point);
        return true;
    }

    public bool TryGetDropPlatforms(List<Collider2D> platforms)
    {
        platforms.Clear();
        if (!isGrounded) return false;

        foreach (Collider2D platform in supportingGround)
        {
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
    
    public void OnJump(InputValue value)
    {
        if (!value.isPressed || !CanBufferActionInput()) return;
        lastAttackRequestTime = float.NegativeInfinity;
        Move.RequestJump();
    }
    public void OnDash(InputValue value) { if (value.isPressed && ActionState.CanDash()) Move.ExecuteDash(); }
    public void OnAttack(InputValue value)
    {
        if (!value.isPressed || !CanBufferActionInput()) return;
        Move.ClearJumpRequest();
        lastAttackRequestTime = Time.time;
        ProcessBufferedAttack();
    }

    private bool CanBufferActionInput()
    {
        return !SceneFader.IsPlayerControlBlocked && ActionState != null && !ActionState.isDead && !ActionState.isHitStunned
            && !ActionState.isAwakening && (Health == null || !Health.IsDead);
    }

    private void ProcessBufferedAttack()
    {
        if (!CanBufferActionInput())
        {
            ClearBufferedActionInputs();
            return;
        }

        if (Time.time - lastAttackRequestTime > attackBufferTime)
        {
            lastAttackRequestTime = float.NegativeInfinity;
            return;
        }

        if (!ActionState.CanAttack()) return;
        lastAttackRequestTime = float.NegativeInfinity;
        Attack.ExecuteAttack();
    }

    public void ClearBufferedActionInputs()
    {
        lastAttackRequestTime = float.NegativeInfinity;
        if (Move != null) Move.ClearJumpRequest();
    }

    private void OnDisable()
    {
        ClearBufferedActionInputs();
    }

    public void OnSkillX(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillX(this); }
    public void OnSkillA(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillA(this); }
    public void OnSkillS(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillS(this); }
    public void OnSkillD(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillD(this); }
    public void OnSkillF(InputValue value) { if (value.isPressed && ActionState.CanSkill()) Skill.ExecuteSkillF(this); }

    //평상시 Q는 각성을 시도하고, 각성 중 Q는 폭주 유물의 초토화를 시도한다.
    //초토화는 PlayerSkill 내부에서 별도로 실행하므로 X/A/S/D/F 슬롯을 차지하지 않는다.
    public void OnAwaken(InputValue value)
    {
        if (SceneFader.IsPlayerControlBlocked || !value.isPressed || Awakening == null || ActionState == null) return;

        if (Awakening.IsAwakened)
        {
            Skill?.TryUseAwakeningSkill(this);
            return;
        }

        if (ActionState.CanAwakening()) Awakening.TryAwaken(this);
    }

    //public void OnTransformSuper(InputValue value) { if (value.isPressed && ActionState.CanTransform()) currentState?.OnTransformSuper(); }
    //public void OnTransformAnimal(InputValue value) { if (value.isPressed && ActionState.CanTransform()) currentState?.OnTransformAnimal(); }

    public void OnInteract()
    {
        if (SceneFader.IsPlayerControlBlocked) return;
        currentInteractable?.Interact(this);
    }


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
