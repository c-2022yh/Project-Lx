using UnityEngine;

//플레이어의 애니메이션 출력을 처리하는 스크립트
public class PlayerAnimation : MonoBehaviour
{
    //기본 변수
    private Player player;
    private Animator animator;

    private static readonly int Speed = Animator.StringToHash("Speed");
    //private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    //private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
    //private static readonly int Dash = Animator.StringToHash("Dash");

    private void Awake()
    {
        player = GetComponent<Player>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float speed = Mathf.Abs(player.rb.linearVelocity.x);
        animator.SetFloat(Speed, speed);
    }

    private void UpdateMovementAnimation()
    {
        animator.SetFloat(Speed, Mathf.Abs(player.rb.linearVelocity.x));
        //animator.SetBool(IsGrounded, player.isGrounded);
        //animator.SetFloat(VerticalVelocity, player.rb.linearVelocity.y);
    }

    public void PlayDash()
    {
        //animator.SetTrigger(Dash);
    }
}