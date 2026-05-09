using System.Diagnostics;
using UnityEngine;

public class NormalState : PlayerState
{
    public NormalState(Player p) : base(p) { }

    public override void EnterTransform()
    {
        SyncVisualDirection(player.initialScale);
        player.spriteRenderer.color = Color.green;
        player.canDoubleJump = false;
        Physics2D.SyncTransforms();
    }

    public override void ExitTransform()
    {
        
    }

    public override void DoUpdate()
    {

    }
    public override void DoFixedUpdate()
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