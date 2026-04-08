public abstract class PlayerState
{
    protected Player player;
    public PlayerState(Player player)
    {
        this.player = player;
    }
    public abstract void EnterTransform(); //상태 진입 시 색상/크기 변경
    public abstract void DoUpdate(); // 이동 가감속 로직
    public abstract void DoJump();  // 점프(단일/더블) 로직

    // 변신 명령 (기본적으로는 아무 일도 안 함, 필요 시 오버라이드)
    public virtual void OnTransformSuper() { }
    public virtual void OnTransformAnimal() { }
}