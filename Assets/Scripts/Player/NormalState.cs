using System.Diagnostics;
using UnityEngine;

public class NormalState : PlayerState
{
    public NormalState(Player player) : base(player) { }

    public override void EnterTransform()
    {
        SyncVisualDirection();
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