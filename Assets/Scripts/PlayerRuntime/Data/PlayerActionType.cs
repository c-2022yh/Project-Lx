using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


//플레이어 액션 형태를 enum으로 지정
public enum PlayerActionType
{
    Normal,         //일반 아무것도 안하고 있는 상태
    Attacking,      //공격 중
    Dashing,        //대쉬 중
    Skill,          //스킬 사용 중
    Awakening,      //각성 변신 중
    HitStunned,     //피격 경직 중
    Dead            //사망 상태
}
