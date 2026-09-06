using System.Collections;
using UnityEngine;

public class AnimalState : PlayerState
{
    public AnimalState(Player p) : base(p) { }

    public override void EnterTransform()
    {
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

    }

    public override void DoDoubleJump()
    {
        
    }

    public override void OnTransformAnimal()
    {
        player.ChangeState(new NormalState(player));
    }
}