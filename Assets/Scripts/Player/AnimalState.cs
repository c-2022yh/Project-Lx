using System.Collections;
using UnityEngine;

public class AnimalState : PlayerState
{
    public AnimalState(Player player) : base(player) { }

    public override void EnterTransform()
    {
        player.spriteRenderer.color = Color.yellow;
        player.transform.localScale = new Vector3(1.4f, 0.7f, 1.0f);

        Physics2D.SyncTransforms(); 
        
        
        var rb = player.GetComponent<Rigidbody2D>();
        rb.simulated = false; // 물리 시뮬레이션 잠시 껐다가
        rb.simulated = true;  // 즉시 다시 켜기
        Debug.Log("<color=orange>동물 변신!</color>");
    }

    public override void ExitTransform()
    {
        player.transform.localScale = player.initialScale;
        Debug.Log("<color=white>동물 변신 해제</color>");
    }

    public override void DoUpdate()
    {

        player.DoMove(player.animalSpeedMultiplier, 2.0f);
    }

    public override void DoJump()
    {
        if (player.isGrounded)
        {
            player.ExecuteJump(player.animalJumpMultiplier);
        }
    }

    public override void OnTransformAnimal()
    {
        player.ChangeState(new NormalState(player));
    }
}