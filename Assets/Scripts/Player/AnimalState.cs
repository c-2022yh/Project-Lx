using System.Collections;
using UnityEngine;

public class AnimalState : PlayerState
{
    public AnimalState(Player p) : base(p) { }

    public override void EnterTransform()
    {
        player.spriteRenderer.color = Color.yellow;
        SyncVisualDirection(player.initialScale);
        var rb = player.GetComponent<Rigidbody2D>();
        Physics2D.SyncTransforms();
        Debug.Log("<color=orange>동물 변신!</color>");

    }

    public override void ExitTransform()
    {
        player.transform.localScale = player.initialScale;
        Debug.Log("<color=white>동물 변신 해제</color>");
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