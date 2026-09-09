using System.Collections;
using UnityEngine;

//플레이어의 애니메이션 출력을 처리하는 스크립트
public class PlayerAnimation : MonoBehaviour
{
    private Player player;
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




    private bool isDashAnimating;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        animator.SetFloat(Speed, Mathf.Abs(player.rb.linearVelocity.x));
        animator.SetBool(IsGrounded, player.isGrounded);
        animator.SetFloat(VerticalVelocity, player.rb.linearVelocity.y);
        animator.SetBool(IsDashing, player.ActionState.isDashing);
        animator.SetBool(IsAttacking, player.ActionState.isAttacking);
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