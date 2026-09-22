using UnityEngine;

//원거리 적의 행동 판단
//플레이어에게 접근하거나 거리를 유지하고, 너무 가까워지면 후퇴한다.
public class EnemyRangedAI : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Chase
    }

    private AIState currentState = AIState.Patrol;


    [Header("Ranged Distance")]

    //이 거리보다 멀면 플레이어에게 접근
    [SerializeField] private float attackRange = 6f;

    private EnemyAI ai;

    public bool IsInAttackRange { get; private set; }


    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }


    private void OnEnable()
    {
        currentState = AIState.Patrol;

        IsInAttackRange = false;
    }


    private void FixedUpdate()
    {
        if (ai == null) return;


        if (!ai.BeginBehaviourTick())
        {
            IsInAttackRange = false;
            return;
        }

        //상태 전환
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

                IsInAttackRange = false;

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

        //플레이어와 실제 2D 거리
        Vector2 difference = player.position - transform.position;

        float distance = difference.magnitude;

        //좌우 바라보는 방향은 X값으로 결정
        int playerDirection = difference.x > 0f ? 1 : -1;

        //공격 사거리 안
        if (distance <= attackRange)
        {
            IsInAttackRange = true;

            ai.StopMovement();

            //플레이어 방향 바라보기
            ai.TrySetDirection(playerDirection);

            return;
        }

        //공격 사거리 밖
        IsInAttackRange = false;

        ai.TrySetDirection(playerDirection);


        if (!ai.CanMoveForward())
        {
            ai.HandleObstacle();
            return;
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

        IsInAttackRange = false;

        ai.StartPatrolIdle();
    }
}