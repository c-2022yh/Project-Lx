using UnityEngine;

//엘리트 적의 행동 판단을 담당
public class EnemyEliteAI : MonoBehaviour
{
    //상태 열거
    private enum AIState
    { 
        Patrol,
        Chase,
        Attack
    }

    private AIState currentState = AIState.Patrol;

    [Header("Attack Range")]
    [SerializeField] private float meleeAttackRange = 1.5f;

    [Header("Charge Range")]
    [SerializeField] private float chargeMinRange = 3f;
    [SerializeField] private float chargeMaxRange = 6f;

    private EnemyAI ai;

    //공격 담당 스크립트
    private EnemyEliteAttack eliteAttack;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        eliteAttack = GetComponent<EnemyEliteAttack>();
    }

    private void OnEnable()
    {
        currentState = AIState.Patrol;
    }

    private void FixedUpdate()
    {
        if (ai == null) return;

        //EnemyAI의 공통 행동 가능 여부 체크
        if (!ai.BeginBehaviourTick()) return;

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;

            case AIState.Chase:
                UpdateChase();
                break;

            case AIState.Attack:
                UpdateAttack();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (ai.CanDetectPlayer())
        {
            EnterChase();
            return;
        }

        ai.UpdatePatrol();
    }

    private void UpdateChase()
    {
        if (ai.ShouldStopChasing())
        {
            EnterPatrol();
            return;
        }

        Transform player = ai.Player;

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        Vector2 difference = player.position - transform.position;

        float distance = difference.magnitude;

        int playerDirection = difference.x >= 0f ? 1 : -1;

        ai.TrySetDirection(playerDirection);

        //근거리 공격
        if (distance <= meleeAttackRange)
        {
            if (eliteAttack != null && eliteAttack.CanUseMeleeAttack())
            {
                EnterAttack();
                eliteAttack.StartMeleeAttack(OnAttackFinished);
                return;
            }
        }

        //돌진 공격
        if (distance >= chargeMinRange && distance <= chargeMaxRange)
        {
            if (eliteAttack != null && eliteAttack.CanUseChargeAttack())
            {
                EnterAttack();
                eliteAttack.StartChargeAttack(
                    playerDirection,
                    OnAttackFinished
                );

                return;
            }
        }

        //공격할 수 없으면 추적
        if (!ai.CanMoveForward())
        {
            ai.HandleObstacle();
            return;
        }

        ai.Move();
    }

    private void UpdateAttack()
    {
        //실제 공격은 EnemyEliteAttack이 관리
        ai.StopMovement();
    }

    private void EnterPatrol()
    {
        currentState = AIState.Patrol;

        ai.StartPatrolIdle();
    }

    private void EnterChase()
    {
        currentState = AIState.Chase;

        ai.FacePlayer();
    }

    private void EnterAttack()
    {
        currentState = AIState.Attack;

        ai.StopMovement();
    }

    private void OnAttackFinished()
    {
        currentState = AIState.Chase;
    }
}