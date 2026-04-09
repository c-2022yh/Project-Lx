using System.Collections;
using UnityEngine;

public class SuperState : PlayerState
{
    public SuperState(Player player) : base(player) { }

    public override void EnterTransform()
    {
        player.spriteRenderer.color = Color.red;
        player.transform.localScale = player.initialScale;

        player.canDoubleJump = true;

        Debug.Log("<color=red>각성!</color>");

    }

    public override void ExitTransform()
    {
        Debug.Log("<color=white>각성 해제</color>");

    }

    public override void DoUpdate()
    {
        if (!player.isDashing)
        {
            player.DoMove(player.superSpeedMultiplier, 1.2f);
        }



        if (player.dashInputPressed)
        {
            player.ExecuteDash();

            player.dashInputPressed = false;
        }
    }

    public override void DoJump()
    {
        //땅에 있거나, 공중 점프가 가능할 때 실행
        if (player.isGrounded) //첫 점프
        {
            player.ExecuteJump(player.superJumpMultiplier);
        }
        else if (player.canDoubleJump) //공중에서 더블 점프
        {
            player.canDoubleJump = false;
            player.ExecuteJump(player.superJumpMultiplier);
        }
    }


    public override void OnTransformSuper()
    {
        player.ChangeState(new NormalState(player));
    }

   
}