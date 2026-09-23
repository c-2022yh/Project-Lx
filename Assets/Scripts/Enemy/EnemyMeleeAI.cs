using UnityEngine;

//근거리 적의 행동 판단
public class EnemyMeleeAI : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Chase
    }

    private AIState currentState = AIState.Patrol;


    [Header("Chase")]

    //이 거리 이상 떨어져 있으면 플레이어 방향으로 방향을 보정
    [SerializeField] private float chaseTurnDistance = 1f;


    private EnemyAI ai;


    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }


    private void OnEnable()
    {
        currentState = AIState.Patrol;
    }


    private void FixedUpdate()
    {
        if (ai == null) return;

        //사망 / 넉백 / AI 정지 상태 처리
        if (!ai.BeginBehaviourTick()) return;

        if (currentState == AIState.Patrol)
        {
            if (ai.CanDetectPlayer())
            {
                EnterChase();
            }
        }
        else if (currentState == AIState.Chase)
        {
            if (ai.ShouldStopChasing())
            {
                EnterPatrol();
            }
        }

        switch (currentState)
        {
            case AIState.Patrol:
                ai.UpdatePatrol();
                break;

            case AIState.Chase:
                UpdateChase();
                break;
        }
    }

    private void UpdateChase()
    {
        Transform player = ai.Player;

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        if (!ai.CanMoveForward())
        {
            ai.HandleObstacle();
            return;
        }


        float differenceX = player.position.x - transform.position.x;

        float distanceX = Mathf.Abs(differenceX);

        if (distanceX >= chaseTurnDistance)
        {
            int playerDirection = differenceX > 0f ? 1 : -1;

            ai.TrySetDirection(playerDirection);
        }

        ai.Move();
    }

    //상태 전환
    private void EnterChase()
    {
        currentState = AIState.Chase;

        ai.FacePlayer();
    }

    private void EnterPatrol()
    {
        currentState = AIState.Patrol;

        ai.StartPatrolIdle();
    }
}