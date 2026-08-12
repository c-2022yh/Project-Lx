using System;
using UnityEngine;

//플레이어의 적 처치 이벤트를 관리
public class PlayerKillTracker : MonoBehaviour
{
    //어떤 적을 처치했는지 전달
    public event Action<GameObject> OnEnemyKilled;

    //적이 사망했을 때 호출
    public void NotifyEnemyKilled(GameObject killedEnemy)
    {
        OnEnemyKilled?.Invoke(killedEnemy);

        Debug.Log( $"[PlayerKillTracker] 적 처치: {killedEnemy.name}");

    }
}