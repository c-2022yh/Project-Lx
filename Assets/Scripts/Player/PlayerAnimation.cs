using System.Collections;
using UnityEngine;

//플레이어의 애니메이션 출력을 처리하는 스크립트
public class PlayerAnimation : MonoBehaviour
{
    private Player player;
    private PlayerSkill playerSkill;
    private bool hasSkillAnimationParameters;
    [SerializeField] private Animator animator;

    //애니메이션 제어 변수
    private static readonly int Speed = Animator.StringToHash("Speed");
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

    private bool isDashAnimating;

    private void Awake()
    {
        player = GetComponent<Player>();
        playerSkill = GetComponent<PlayerSkill>();
        CacheSkillAnimationParameters();
    }

    private void OnEnable()
    {
        if (playerSkill != null) playerSkill.OnSkillUsed += HandleSkillUsed;
    }

    private void OnDisable()
    {
        if (playerSkill != null) playerSkill.OnSkillUsed -= HandleSkillUsed;
        if (animator != null && hasSkillAnimationParameters)
        {
            animator.ResetTrigger(Skill);
            animator.SetBool(IsSkillActive, false);
        }
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

    private void HandleSkillUsed(SkillData skill)
    {
        if (skill != null) PlaySkill(skill.animationType);
    }

    public void PlaySkill(SkillAnimationType animationType)
    {
        if (animator == null || !hasSkillAnimationParameters) return;
        animator.ResetTrigger(Skill);
        if (animationType == SkillAnimationType.None) return;
        animator.SetInteger(SkillIndex, (int)animationType);
        animator.SetBool(IsSkillActive, true);
        animator.SetTrigger(Skill);
    }

    private void Update()
    {
        animator.SetFloat(Speed, Mathf.Abs(player.rb.linearVelocity.x));
        animator.SetBool(IsGrounded, player.isGrounded);
        animator.SetFloat(VerticalVelocity, player.rb.linearVelocity.y);
        animator.SetBool(IsDashing, player.ActionState.isDashing);
        animator.SetBool(IsAttacking, player.ActionState.isAttacking);
        if (hasSkillAnimationParameters)
        {
            bool isSkillActive = player.ActionState.isSkillActive;
            animator.SetBool(IsSkillActive, isSkillActive);
            if (!isSkillActive) animator.ResetTrigger(Skill);
        }
    }


    //대쉬 시간 후에도 대쉬모션을 유지하기 위한 함수
    public void PlayDash(float holdTime)
    {
        StopAllCoroutines();
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
        animator.SetInteger(AttackIndex, index);
        animator.SetTrigger(Attack);
    }
    

}