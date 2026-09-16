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

    //이 거리보다 가까우면 플레이어에게서 후퇴
    [SerializeField] private float retreatRange = 3f;

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


        float differenceX = player.position.x - transform.position.x;

        float distanceX = Mathf.Abs(differenceX);

        int playerDirection = differenceX > 0f ? 1 : -1;

        //너무 멀면 접근하기
        if (distanceX > attackRange)
        {
            IsInAttackRange = false;

            //플레이어 방향 바라보기
            ai.TrySetDirection(playerDirection);

            //벽 / 낭떠러지
            if (!ai.CanMoveForward())
            {
                ai.HandleObstacle();
                return;
            }

            ai.Move();
            return;
        }

        //너무 가까우면 후퇴하기
        if (distanceX < retreatRange)
        {
            IsInAttackRange = false;

            int retreatDirection = -playerDirection;


            //플레이어 반대 방향으로 회전
            bool turned = ai.TrySetDirection(retreatDirection);

            //방향전환 쿨다운 때문에
            //아직 반대 방향을 못 봤다면 일단 정지
            if (!turned)
            {
                ai.StopMovement();
                return;
            }


            //후퇴 방향이 벽 / 낭떠러지라면
            //더 이상 후퇴하지 않는다.
            if (!ai.CanMoveForward())
            {
                ai.StopMovement();

                //다시 플레이어 바라보기
                ai.TrySetDirection(playerDirection);

                //구석에 몰렸다면 공격 가능 상태로 처리
                IsInAttackRange = true;

                return;
            }


            ai.Move();

            return;
        }

        //적정 공격 거리라면 
        ai.StopMovement();

        ai.TrySetDirection(playerDirection);

        IsInAttackRange = true;
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