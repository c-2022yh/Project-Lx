using System.Collections;
using UnityEngine;

//플레이어의 애니메이션 출력을 처리하는 스크립트
public class PlayerAnimation : MonoBehaviour
{
    private Player player;
    private PlayerSkill playerSkill;
    private PlayerHealth playerHealth;
    private bool hasReactionAnimationParameters;

    [Header("Respawn Animation") ]
    [SerializeField] private string respawnStateName = "Base Layer.Player_Idle";
    private bool hasSkillAnimationParameters;
    private bool hasAwakeningAnimationParameters;
    [SerializeField] private Animator animator;

    [Header("Walk To Run Animation")]
    [Tooltip("Seconds of continuous grounded movement in the same direction before the run animation starts.")]
    [SerializeField, Min(0f)] private float runStartDelay = 0.6f;
    private float continuousMoveTime;
    private int continuousMoveDirection;
    private bool hasRunningAnimationParameter;

    //애니메이션 제어 변수
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");

    private static readonly int IsDashing = Animator.StringToHash("IsDashing");
    private static readonly int IsDashAnimating = Animator.StringToHash("IsDashAnimating");

    private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");




    private static readonly int SkillIndex = Animator.StringToHash("SkillIndex");
    private static readonly int Skill = Animator.StringToHash("Skill");
    private static readonly int IsSkillActive = Animator.StringToHash("IsSkillActive");

    private static readonly int Awakening = Animator.StringToHash("Awakening");
    private static readonly int IsAwakening = Animator.StringToHash("IsAwakening");

    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int IsHitStunned = Animator.StringToHash("IsHitStunned");
    private static readonly int IsDead = Animator.StringToHash("IsDead");

    private bool isDashAnimating;

    private bool IsDeadNow => (playerHealth != null && playerHealth.IsDead)
        || (player != null && player.ActionState != null && player.ActionState.isDead);
    private bool IsHitStunnedNow => player != null && player.ActionState != null
        && player.ActionState.isHitStunned;


    private void Awake()
    {
        player = GetComponent<Player>();
        playerSkill = GetComponent<PlayerSkill>();
        playerHealth = GetComponent<PlayerHealth>();
        CacheSkillAnimationParameters();
        CacheAwakeningAnimationParameters();
        CacheReactionAnimationParameters();
        CacheRunningAnimationParameter();
    }

    private void OnEnable()
    {
        if (playerSkill != null) playerSkill.OnSkillUsed += HandleSkillUsed;
    }

    private void OnDisable()
    {
        ResetRunningAnimation();
        if (playerSkill != null) playerSkill.OnSkillUsed -= HandleSkillUsed;
        ResetAwakeningAnimation();
        if (animator != null && hasSkillAnimationParameters)
        {
            animator.ResetTrigger(Skill);
            animator.SetBool(IsSkillActive, false);
        }
    }

    // Controllers without IsRunning can keep using their existing movement animations.
    private void CacheRunningAnimationParameter()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == IsRunning && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasRunningAnimationParameter = true;
                break;
            }
        }
    }

    private void ResetRunningAnimation()
    {
        continuousMoveTime = 0f;
        continuousMoveDirection = 0;
        if (animator != null && hasRunningAnimationParameter)
            animator.SetBool(IsRunning, false);
    }

    private void UpdateRunningAnimation()
    {
        float inputX = player.moveInput.x;
        float velocityX = player.rb.linearVelocity.x;
        // Require input and actual movement: holding against a wall must not build up run time.
        if (!player.isGrounded || !player.ActionState.isNormal || IsDeadNow ||
            isDashAnimating || Mathf.Abs(inputX) <= 0.01f || Mathf.Abs(velocityX) <= 0.1f ||
            Mathf.Sign(inputX) != Mathf.Sign(velocityX))
        {
            ResetRunningAnimation();
            return;
        }

        int direction = inputX > 0f ? 1 : -1;
        if (direction != continuousMoveDirection)
        {
            ResetRunningAnimation();
            continuousMoveDirection = direction;
        }

        continuousMoveTime += Time.deltaTime;
        if (hasRunningAnimationParameter)
            animator.SetBool(IsRunning, continuousMoveTime >= runStartDelay);
    }

    // Existing controllers remain usable until the skill parameters are added.
    private void CacheSkillAnimationParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool hasIndex = false;
        bool hasTrigger = false;
        bool hasActive = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == SkillIndex && parameter.type == AnimatorControllerParameterType.Int) hasIndex = true;
            if (parameter.nameHash == Skill && parameter.type == AnimatorControllerParameterType.Trigger) hasTrigger = true;
            if (parameter.nameHash == IsSkillActive && parameter.type == AnimatorControllerParameterType.Bool) hasActive = true;
        }
        hasSkillAnimationParameters = hasIndex && hasTrigger && hasActive;
    }

    private void CacheAwakeningAnimationParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool hasTrigger = false;
        bool hasActive = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == Awakening && parameter.type == AnimatorControllerParameterType.Trigger) hasTrigger = true;
            if (parameter.nameHash == IsAwakening && parameter.type == AnimatorControllerParameterType.Bool) hasActive = true;
        }
        hasAwakeningAnimationParameters = hasTrigger && hasActive;
        if (!hasAwakeningAnimationParameters)
            Debug.LogWarning("Add Animator parameters: Awakening (Trigger), IsAwakening (Bool).", this);
    }

    private void ResetAwakeningAnimation()
    {
        if (animator == null || !hasAwakeningAnimationParameters) return;
        animator.ResetTrigger(Awakening);
        animator.SetBool(IsAwakening, false);
    }

    private void CacheReactionAnimationParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool hasHit = false, hasStun = false, hasDead = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == Hit && parameter.type == AnimatorControllerParameterType.Trigger) hasHit = true;
            if (parameter.nameHash == IsHitStunned && parameter.type == AnimatorControllerParameterType.Bool) hasStun = true;
            if (parameter.nameHash == IsDead && parameter.type == AnimatorControllerParameterType.Bool) hasDead = true;
        }
        hasReactionAnimationParameters = hasHit && hasStun && hasDead;
        if (!hasReactionAnimationParameters)
            Debug.LogWarning("Add Animator parameters: Hit (Trigger), IsHitStunned (Bool), IsDead (Bool).", this);
    }

    private void CancelActionAnimation()
    {
        ResetRunningAnimation();
        StopAllCoroutines();
        isDashAnimating = false;
        ResetAwakeningAnimation();
        if (animator == null) return;
        animator.ResetTrigger(Attack);
        animator.SetBool(IsAttacking, false);
        animator.SetBool(IsDashing, false);
        animator.SetBool(IsDashAnimating, false);
        if (hasSkillAnimationParameters)
        {
            animator.ResetTrigger(Skill);
            animator.SetBool(IsSkillActive, false);
        }
    }

    public void PlayHit()
    {
        if (IsDeadNow) return;
        CancelActionAnimation();
        if (animator == null || !hasReactionAnimationParameters) return;
        animator.SetBool(IsDead, false);
        animator.SetBool(IsHitStunned, true);
        animator.ResetTrigger(Hit);
        animator.SetTrigger(Hit);
    }

    public void PlayDeath()
    {
        CancelActionAnimation();
        if (animator == null || !hasReactionAnimationParameters) return;
        animator.ResetTrigger(Hit);
        animator.SetBool(IsHitStunned, false);
        animator.SetBool(IsDead, true);
    }

    public void ResetOnRespawn()
    {
        CancelActionAnimation();
        if (animator == null) return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(parameter.nameHash);
        if (hasReactionAnimationParameters)
        {
            animator.SetBool(IsHitStunned, false);
            animator.SetBool(IsDead, false);
        }
        animator.SetFloat(Speed, 0f);
        animator.SetFloat(VerticalVelocity, 0f);
        animator.SetBool(IsGrounded, player != null && player.isGrounded);
        int stateHash = Animator.StringToHash(respawnStateName);
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }
        else
            Debug.LogWarning("Respawn state not found. Set Respawn State Name to the full Idle state path: " + respawnStateName, this);
    }

    private void HandleSkillUsed(SkillData skill)
    {
        if (skill != null) PlaySkill(skill.animationType);
    }

    public void PlaySkill(SkillAnimationType animationType)
    {
        if (animator == null || !hasSkillAnimationParameters || IsDeadNow || IsHitStunnedNow) return;
        ResetRunningAnimation();
        animator.ResetTrigger(Skill);
        if (animationType == SkillAnimationType.None) return;
        animator.SetInteger(SkillIndex, (int)animationType);
        animator.SetBool(IsSkillActive, true);
        animator.SetTrigger(Skill);
    }

    public void PlayAwakening()
    {
        if (animator == null || !hasAwakeningAnimationParameters || IsDeadNow || IsHitStunnedNow) return;
        ResetRunningAnimation();
        animator.ResetTrigger(Awakening);
        animator.SetBool(IsAwakening, true);
        animator.SetTrigger(Awakening);
    }

    private void Update()
    {
        if (animator == null || player == null || player.rb == null || player.ActionState == null) return;
        UpdateRunningAnimation();
        bool blocked = IsDeadNow || IsHitStunnedNow;
        if (hasReactionAnimationParameters)
        {
            animator.SetBool(IsDead, IsDeadNow);
            animator.SetBool(IsHitStunned, !IsDeadNow && IsHitStunnedNow);
        }
        animator.SetFloat(Speed, Mathf.Abs(player.rb.linearVelocity.x));
        animator.SetBool(IsGrounded, player.isGrounded);
        animator.SetFloat(VerticalVelocity, player.rb.linearVelocity.y);
        animator.SetBool(IsDashing, !blocked && player.ActionState.isDashing);
        animator.SetBool(IsAttacking, !blocked && player.ActionState.isAttacking);
        if (hasAwakeningAnimationParameters)
        {
            bool isAwakening = !blocked && player.ActionState.isAwakening;
            animator.SetBool(IsAwakening, isAwakening);
            if (!isAwakening) animator.ResetTrigger(Awakening);
        }
        if (hasSkillAnimationParameters)
        {
            bool isSkillActive = !blocked && player.ActionState.isSkillActive;
            animator.SetBool(IsSkillActive, isSkillActive);
            if (!isSkillActive) animator.ResetTrigger(Skill);
        }
    }


    //대쉬 시간 후에도 대쉬모션을 유지하기 위한 함수
    public void PlayDash(float holdTime)
    {
        if (animator == null || IsDeadNow || IsHitStunnedNow) return;
        CancelActionAnimation();
        StartCoroutine(DashAnimationRoutine(holdTime));
    }

    private IEnumerator DashAnimationRoutine(float holdTime)
    {
        isDashAnimating = true;
        animator.SetBool(IsDashAnimating, true);

        yield return new WaitForSeconds(holdTime);

        isDashAnimating = false;
        animator.SetBool(IsDashAnimating, false);
    }

    //공격 애니메이션 재생 함수
    public void PlayAttack(int index)
    {
        if (animator == null || IsDeadNow || IsHitStunnedNow) return;
        ResetRunningAnimation();
        animator.SetInteger(AttackIndex, index);
        animator.SetTrigger(Attack);
    }
    

}
