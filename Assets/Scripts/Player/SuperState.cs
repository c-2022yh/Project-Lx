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

            // 버튼을 꾹 누르고 있어도 한 번만 나가게 하려면 
            // 호출 후 false로 꺼버리는 처리가 필요할 수 있습니다.
            player.dashInputPressed = false;
        }
    }

    public override void DoJump()
    {
        //점프 로직 변경: 땅에 있거나, 공중 점프가 가능할 때 실행
        if (player.isGrounded) // 첫 번째 점프
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

    /*
     
    public override void Enter()
    {
        base.Enter();
        // 플레이어가 들고 있는 Follower 스크립트를 찾아 모드 변경
        player.orb.SetSuperMode(true);
        Debug.Log("각성: 팔로워 거대화!");
    }

    public override void Exit()
    {
 public override void Exit()       base.Exit();
        player.orb.SetSuperMode(false);
        Debug.Log("해제: 팔로워 축소");
    }*/
}