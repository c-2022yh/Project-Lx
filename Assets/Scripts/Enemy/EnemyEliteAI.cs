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
    [Tooltip("근거리 공격이 발동되는 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float meleeAttackRange = 1.5f;

    [Header("Charge Range")]
    [Tooltip("돌진을 시작할 최소 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeMinRange = 3f;

    [Tooltip("돌진을 시작할 최대 거리입니다.")]
    [Min(0f)]
    [SerializeField] private float chargeMaxRange = 6f;

    [Header("Attack Test")]
    [SerializeField] private bool enableMeleeAttack = false;
    [SerializeField] private bool enableChargeAttack = true;

    private EnemyAI ai;

    //공격 담당 스크립트
    private EnemyEliteAttack eliteAttack;

    private void OnValidate()
    {
        chargeMaxRange = Mathf.Max(chargeMinRange, chargeMaxRange);
    }

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
            if (enableMeleeAttack &&
                eliteAttack != null &&
                eliteAttack.CanUseMeleeAttack())
            {
                EnterAttack();
                eliteAttack.StartMeleeAttack(OnAttackFinished);
                return;
            }

            ai.StopMovement();
            return;
        }

        //돌진 공격
        if (enableChargeAttack &&
            distance >= chargeMinRange &&
            distance <= chargeMaxRange)
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
        if (eliteAttack == null || !eliteAttack.IsCharging)
        {
            ai.StopMovement();
        }
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