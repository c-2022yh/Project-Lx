using System.Collections;
using UnityEngine;

public class SuperState : PlayerState
{
    public SuperState(Player p) : base(p) { }

    public override void EnterTransform()
    {

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





    public override void OnTransformSuper()
    {
        player.ChangeState(new NormalState(player));
    }

   
}