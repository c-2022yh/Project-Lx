using System.Diagnostics;
using UnityEngine;

public class NormalState : PlayerState
{
    // 생성자: Player 본체를 부모(PlayerState)에게 전달
    public NormalState(Player player) : base(player) { }

    public override void EnterTransform()
    {
        player.spriteRenderer.color = Color.green;
        player.transform.localScale = player.initialScale;

        player.canDoubleJump = false;
        Physics2D.SyncTransforms();
    }

    public override void ExitTransform()
    {
        
    }

    public override void DoUpdate()
    {
        player.DoMove();
    }

    public override void DoJump()
    {
        // 땅에 있을 때만 점프 실행
        if (player.isGrounded)
        {
            player.ExecuteJump();
        }
    }

    public override void OnTransformSuper()
    {
        player.ChangeState(new SuperState(player));
    }
    public override void OnTransformAnimal()
    {
        player.ChangeState(new AnimalState(player));
    }
}