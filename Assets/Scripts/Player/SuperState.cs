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
        player.DoMove(1.5f, 1.2f);

        //더블탭 감지
        CheckDoubleTapDash();
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

    private void CheckDoubleTapDash()
    {
        if (player.moveInput.x != 0 && player.lastInput.x == 0)
        {
            float timeSinceLastTap = Time.time - player.lastTapTime;
            Debug.Log($"입력 감지! 이전 입력과의 간격: {timeSinceLastTap}");

            if (timeSinceLastTap < 0.5f && player.moveInput.x == player.lastDirection.x)
            {
                Debug.Log("조건 일치! 대쉬 실행!");
                player.ExecuteDash(player.moveInput);
            }

            player.lastTapTime = Time.time;
            player.lastDirection = player.moveInput;
        }
    }



    public override void OnTransformSuper()
    {
        player.ChangeState(new NormalState(player));
    }
}