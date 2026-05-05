using UnityEngine;
public abstract class PlayerState
{
    protected Player player;
    public PlayerState(Player player)
    {
        this.player = player;
    }
    public abstract void EnterTransform(); //상태 진입 시 색상/크기 변경
    public abstract void ExitTransform(); //상태 탈출 시 색상/크기 변경
    public abstract void DoUpdate(); //이동 가감속 로직
    public abstract void DoJump();  //점프(단일/더블) 로직

    protected void SyncVisualDirection() //변신 시 바라보는 방향 동기화
    {
        // player가 현재 바라보고 있어야 할 방향을 '강제로' 주입
        float direction = player.isFacingRight ? 1f : -1f;

        // 절대값에 방향을 곱해서 크기는 유지하고 방향만 확실히 바꿈
        Vector3 scale = player.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        player.transform.localScale = scale;

    }


    public virtual void OnTransformSuper() { }
    public virtual void OnTransformAnimal() { }


}