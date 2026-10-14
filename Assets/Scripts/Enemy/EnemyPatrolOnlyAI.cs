using UnityEngine;

// 플레이어를 추적하지 않고 기존 지상 패트롤만 사용하는 AI
// 이동 규칙은 EnemyAI가 담당하고, 이 스크립트는 패트롤 업데이트만 호출한다.
[RequireComponent(typeof(EnemyAI))]
public class EnemyPatrolOnlyAI : MonoBehaviour
{
    private EnemyAI ai;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }

    private void FixedUpdate()
    {
        if (ai == null || !ai.BeginBehaviourTick())
        {
            return;
        }

        ai.UpdatePatrol();
    }
}
