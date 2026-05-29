using System.Collections;
using UnityEngine;

//플레이어 액션 형태를 enum으로 지정
public enum PlayerActionType
{
    Normal,         //일반 아무것도 안하고 있는 상태
    Attacking,      //공격 중
    Dashing,        //대쉬 중
    Skill,          //스킬 사용 중
    Transforming,   //변신 중
    HitStunned,     //피격 경직 중
    Dead            //사망 상태
}


public class PlayerActionState : MonoBehaviour
{
    //현재 상태
    [Header("Current Action State")]
    [SerializeField] private PlayerActionType currentState = PlayerActionType.Normal;

    public PlayerActionType CurrentState => currentState;

    //현재 상태 확인 함수
    public bool isNormal => currentState == PlayerActionType.Normal;
    public bool isAttacking => currentState == PlayerActionType.Attacking;
    public bool isDashing => currentState == PlayerActionType.Dashing;
    public bool isSkillActive => currentState == PlayerActionType.Skill;
    public bool isTransforming => currentState == PlayerActionType.Transforming;
    public bool isHitStunned => currentState == PlayerActionType.HitStunned;
    public bool isDead => currentState == PlayerActionType.Dead;

    //지금 뭔가 행동 중이면?
    public bool IsBusy()
    {
        return currentState != PlayerActionType.Normal;
    }
    
    //지금 동작 가능? 하고 물어봄
    //세부 플래그를 여기서 지정
    public bool CanMove() //이동 아무것도 안 하고 있어야 작동
    {
        return currentState == PlayerActionType.Normal;
    }

    public bool CanJump() //점프도 아무것도 안 하고 있어야 작동
    {
        return currentState == PlayerActionType.Normal;
    }

    public bool CanAttack() //공격도 아무것도 안 하고 있어야 작동
    {
        return currentState == PlayerActionType.Normal;
    }

    public bool CanDash() //대쉬는 cc기 아니면 다 발동
    {
        return currentState != PlayerActionType.Dead
            && currentState != PlayerActionType.HitStunned
            && currentState != PlayerActionType.Dashing;
    }

    public bool CanSkill() //스킬은 평캔이 가능
    {
        return currentState == PlayerActionType.Normal
            || currentState == PlayerActionType.Attacking;
    }

    public bool CanTransform() //변신은 아무것도 안 하고 있어야 작동
    {
        return currentState == PlayerActionType.Normal;
    }

    public bool CanTakeHit() //피격은 대쉬중엔 무시됨, 죽어있지 않으면 다 작동
    {
        return currentState != PlayerActionType.Dead
            && currentState != PlayerActionType.Dashing;
    }

    public bool CanDie()
    {
        return currentState != PlayerActionType.Dead;
    }

    //상태 전환
    public void ChangeState(PlayerActionType newState)
    {
        if (currentState == PlayerActionType.Dead)
            return;

        currentState = newState;
    }

    //동작을 들어갈 때 실행하는 함수
    //논외로 EnterNormal은 대쉬 끝, 공격 끝 등에 들어가야 겠죠?
    public void EnterNormal()
    {
        Debug.Log("<color=white>Normal State</color>");
        ChangeState(PlayerActionType.Normal);
    }

    public void EnterAttack()
    {
        Debug.Log("<color=yellow>Attack State</color>");
        ChangeState(PlayerActionType.Attacking);
    }

    public void EnterDash()
    {
        Debug.Log("<color=red>Dash State</color>");
        ChangeState(PlayerActionType.Dashing);
    }

    public void EnterSkill()
    {
        Debug.Log("<color=orange>Skill State</color>");
        ChangeState(PlayerActionType.Skill);
    }

    public void EnterTransform()
    {
        Debug.Log("<color=green>Transform State</color>");
        ChangeState(PlayerActionType.Transforming);
    }

    public void EnterHitStun()
    {
        Debug.Log("<color=purple>HitStun State</color>");
        ChangeState(PlayerActionType.HitStunned);
    }

    public void EnterDead()
    {
        Debug.Log("<color=black>Dead State</color>");
        currentState = PlayerActionType.Dead;
    }
}