using UnityEngine;
public abstract class PlayerState
{
    protected Player player;
    public PlayerState(Player p)
    {
        this.player = p;
    }
    public abstract void EnterTransform(); //상태 진입 시 색상/크기 변경
    public abstract void ExitTransform(); //상태 탈출 시 색상/크기 변경
    public abstract void DoUpdate(); //입력, 타이머, 애니메이션 체크용
    public abstract void DoFixedUpdate(); //물리 업데이트

    public abstract void DoJump();  //점프(단일/더블) 로직

    protected void SyncVisualDirection(Vector3 targetScale) //변신 시 바라보는 방향 동기화
    {
        float direction = player.isFacingRight ? 1f : -1f;

        // 절대값에 방향을 곱해서 크기는 유지하고 방향만 확실히 바꿈
        player.transform.localScale = new Vector3(Mathf.Abs(targetScale.x) * direction, targetScale.y, targetScale.z);

    }


    public virtual void OnTransformSuper() { }
    public virtual void OnTransformAnimal() { }


}