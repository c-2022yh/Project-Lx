using System.Collections;
using UnityEngine;

public class SuperState : PlayerState
{
    public SuperState(Player p) : base(p) { }

    public override void EnterTransform()
    {
        SyncVisualDirection(player.initialScale);
        player.spriteRenderer.color = Color.red;
        Debug.Log("<color=red>각성!</color>");

    }

    public override void ExitTransform()
    {
        Debug.Log("<color=white>각성 해제</color>");

    }

    public override void DoUpdate()
    {

    }
    public override void DoFixedUpdate()
    {
        player.ExecuteMove(player.superSpeedMultiplier, 1.2f);
    }
    public override void DoDoubleJump()
    {

    }





    public override void OnTransformSuper()
    {
        player.ChangeState(new NormalState(player));
    }

   
}