using UnityEngine;

//근거리 / 원거리 적 AI가 공통으로 상속받는 기본 클래스
public abstract class EnemyAIBase : MonoBehaviour
{
    //현재 바라보는 방향
    //-1 = 왼쪽, 1 = 오른쪽
    public abstract int Direction { get; }

    //현재 AI의 기본 이동속도
    public abstract float MoveSpeed { get; }

    //AI의 이동만 정지
    public abstract void StopMovement();

    //AI 전체 행동 정지
    //사망 등의 상황에서 사용
    public abstract void StopAI();
}