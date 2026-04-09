using System.Collections;
using UnityEngine;

public class SuperState : PlayerState
{
    // 생성자 (부모에게 player 전달)
    public SuperState(Player player) : base(player) { }

    public override void EnterTransform()
    {
        player.spriteRenderer.color = Color.red;
        player.transform.localScale = player.initialScale;

        player.canDoubleJump = true;
    }

    public override void DoUpdate()
    {
        if (!player.isDashing)
        {
            player.DoMove(1.5f, 1.2f);
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
}