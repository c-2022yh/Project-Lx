using System;
using System.Collections;
using UnityEngine;

//플레이어 각성 상태를 관리하는 스크립트
public class PlayerAwakening : MonoBehaviour
{
    [System.Serializable]
    private class CombatBonus
    {
        [Header("Attack")]
        [Min(0f)] public float physicalAttack;
        [Min(0f)] public float magicalAttack;

        [Header("Penetration")]
        [Min(0f)] public float physicalPenetration;
        [Min(0f)] public float magicalPenetration;

        [Header("Critical")]
        [Range(0f, 1f)]
        public float criticalChance;

        [Min(0f)]
        public float criticalMultiplier;

        [Header("Final Damage")]
        [Min(0f)]
        public float damageAmplification;

        //현재 값을 복사해서 적용 당시 수치로 저장
        public CombatBonus Copy()
        {
            return new CombatBonus
            {
                physicalAttack = physicalAttack,
                magicalAttack = magicalAttack,

                physicalPenetration = physicalPenetration,
                magicalPenetration = magicalPenetration,

                criticalChance = criticalChance,
                criticalMultiplier = criticalMultiplier,

                damageAmplification = damageAmplification
            };
        }


        //전투 능력치 적용 또는 제거
        public void ApplyTo(OffensiveStats offense, float sign)
        {
            if (offense == null) return;

            offense.physicalAttack += physicalAttack * sign;
            offense.magicalAttack += magicalAttack * sign;
            offense.physicalPenetration += physicalPenetration * sign;
            offense.magicalPenetration += magicalPenetration * sign;
            offense.criticalChance += criticalChance * sign;
            offense.criticalMultiplier += criticalMultiplier * sign;
            offense.damageAmplification += damageAmplification * sign;

        }


        public void Clear()
        {
            physicalAttack = 0f;
            magicalAttack = 0f;

            physicalPenetration = 0f;
            magicalPenetration = 0f;

            criticalChance = 0f;
            criticalMultiplier = 0f;

            damageAmplification = 0f;
        }
    }


    [Header("Awakening Settings")]
    [SerializeField] private float awakeningDuration = 20f;
    [SerializeField] private float awakeningFreezeTime = 0.8f;

    //일반 각성에 필요한 기력
    [SerializeField] private float normalAwakeningEnergy = 100f;

    //강화 각성에 필요한 기력
    [SerializeField] private float enhancedAwakeningEnergy = 200f;


    [Header("Awakening Color")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color awakenedColor = Color.red;
    [SerializeField] private Color enhancedAwakenedColor = Color.magenta;


    [Header("Movement Bonus")]
    [SerializeField, Min(0.01f)]
    private float awakenedMoveMultiplier = 1.3f;

    [SerializeField, Min(0.01f)]
    private float awakenedJumpMultiplier = 1.2f;


    [Header("Normal Awakening Combat Bonus")]
    [SerializeField]
    private CombatBonus normalCombatBonus = new();


    [Header("Effect")]
    [SerializeField]
    private AwakeningEffect awakeningEffect;


    //현재 각성 상태
    private bool isAwakened;

    //각성 연출 중인 상태
    private bool isAwakening;

    //만월 장착으로 강화 각성이 해금됐는지
    private bool isEnhancedAwakeningUnlocked;

    //현재 발동한 각성이 강화 각성인지
    private bool isEnhancedAwakening;

    //각성이 완전히 종료됐을 때 발생
    public event Action OnAwakeningEnded;

    //실제로 적용한 이동 관련 배율
    private float appliedMoveMultiplier = 1f;
    private float appliedJumpMultiplier = 1f;


    //만월이 등록한 강화 각성 추가 전투 보너스
    private CombatBonus enhancedCombatBonus = new();


    //현재 실제로 적용된 전투 보너스
    //각성 종료 시 같은 수치를 빼기 위해 복사해 둔다.
    private CombatBonus appliedNormalCombatBonus;
    private CombatBonus appliedEnhancedCombatBonus;

    private OffensiveStats activeOffense;
    private Player activePlayer;

    private Coroutine awakeningCoroutine;


    public bool IsAwakened => isAwakened;
    public bool IsAwakening => isAwakening;

    public bool IsEnhancedAwakening => isEnhancedAwakening;

    public bool IsEnhancedAwakeningUnlocked => isEnhancedAwakeningUnlocked; 

    //각성 진입 시도
    public void TryAwaken(Player p)
    {
        if (p == null) return;
        if (p.Energy == null) return;

        //이미 각성 중이거나 변신 중이면 실행하지 않음
        if (isAwakened || isAwakening) return;

        //기력 100 미만이면 일반 각성도 불가능
        if (!p.Energy.HasEnergy(normalAwakeningEnergy)) return;
        

        //만월 장착 + 기력 200 이상이면 강화 각성
        isEnhancedAwakening = isEnhancedAwakeningUnlocked && p.Energy.HasEnergy(enhancedAwakeningEnergy);

        activePlayer = p;
            
        awakeningCoroutine = StartCoroutine(AwakeningRoutine(p));

    }


    //각성 연출 및 지속시간 처리
    private IEnumerator AwakeningRoutine(Player p)
    {
        isAwakening = true;

        p.ActionState.EnterAwakening();

        //변신 연출 중 물리 고정
        p.SetPhysicsFreeze(true);

        if (awakeningEffect != null)
        {
            awakeningEffect.SetAndShow(
                p.transform.position
            );
        }

        yield return new WaitForSeconds(
            awakeningFreezeTime
        );

        p.SetPhysicsFreeze(false);

        //실제 각성 능력치 적용
        EnterAwakened(p);

        isAwakening = false;

        if (p.ActionState.isAwakening)
        {
            p.ActionState.EnterNormal();
        }

        yield return new WaitForSeconds(
            awakeningDuration
        );

        ExitAwakened(p);

        awakeningCoroutine = null;
        activePlayer = null;
    }


    //각성 능력치 적용
    private void EnterAwakened(Player p)
    {
        isAwakened = true;

        //이동속도와 점프력은
        //일반/강화 각성 모두 동일하게 적용
        appliedMoveMultiplier =
            awakenedMoveMultiplier;

        appliedJumpMultiplier =
            awakenedJumpMultiplier;

        p.Move.moveSpeed *=
            appliedMoveMultiplier;

        p.Move.jumpForce *=
            appliedJumpMultiplier;


        //기본 각성 전투 버프 적용
        ApplyNormalCombatBonus(p);


        if (isEnhancedAwakening)
        {
            p.sr.color = enhancedAwakenedColor;

            //만월의 추가 전투 버프 적용
            ApplyEnhancedCombatBonus();

        }
        else
        {
            p.sr.color = awakenedColor;

        }
    }


    //PlayerStats 연결
    private bool TryGetOffensiveStats(Player p)
    {
        PlayerStats playerStats =  p.GetComponent<PlayerStats>();

        activeOffense = playerStats.Offense;

        return true;
    }


    //기본 각성 전투 버프 적용
    private void ApplyNormalCombatBonus(Player p)
    {
        if (!TryGetOffensiveStats(p))
        {
            return;
        }

        //적용 당시 수치를 복사
        appliedNormalCombatBonus = normalCombatBonus.Copy();
        appliedNormalCombatBonus.ApplyTo(activeOffense, 1f);



    }


    //만월 강화 각성 추가 버프 적용
    private void ApplyEnhancedCombatBonus()
    {
        if (activeOffense == null) return;

        //만월이 등록한 수치를 복사
        appliedEnhancedCombatBonus = enhancedCombatBonus.Copy();

        appliedEnhancedCombatBonus.ApplyTo(activeOffense, 1f);

    }


    //기본 각성 전투 버프 제거
    private void RemoveNormalCombatBonus()
    {
        if (activeOffense == null) return;
        if (appliedNormalCombatBonus == null) return;

        appliedNormalCombatBonus.ApplyTo(activeOffense, -1f);

        appliedNormalCombatBonus = null;
    }


    //만월 강화 각성 추가 버프 제거
    private void RemoveEnhancedCombatBonus()
    {
        if (activeOffense == null) return;
        if (appliedEnhancedCombatBonus == null) return;

        appliedEnhancedCombatBonus.ApplyTo(activeOffense, -1f);

        appliedEnhancedCombatBonus = null;
    }


    //각성 종료
    private void ExitAwakened(Player p)
    {
        bool wasEnhancedAwakening = isEnhancedAwakening;

        isAwakened = false;
        p.sr.color = normalColor;
        //이동 능력치 복구
        if (appliedMoveMultiplier > 0f)
        {
            p.Move.moveSpeed /= appliedMoveMultiplier;
        }

        if (appliedJumpMultiplier > 0f)
        {
            p.Move.jumpForce /= appliedJumpMultiplier;
        }

        appliedMoveMultiplier = 1f;
        appliedJumpMultiplier = 1f;


        //강화 보너스를 먼저 제거
        RemoveEnhancedCombatBonus();

        //기본 각성 보너스 제거
        RemoveNormalCombatBonus();

        activeOffense = null;


        //기력 초기화
        p.Energy.ResetEnergy();

        //각성 종료 알림
        OnAwakeningEnded?.Invoke();

        isEnhancedAwakening = false;

    }


    //만월 유물이 강화 각성 추가 능력치를 등록
    public void SetEnhancedAwakeningBonus(
        float physicalAttackBonus,
        float magicalAttackBonus,
        float physicalPenetrationBonus,
        float magicalPenetrationBonus,
        float criticalChanceBonus,
        float criticalMultiplierBonus,
        float damageAmplificationBonus)
    {
        enhancedCombatBonus.physicalAttack = physicalAttackBonus;

        enhancedCombatBonus.magicalAttack = magicalAttackBonus;

        enhancedCombatBonus.physicalPenetration = physicalPenetrationBonus;

        enhancedCombatBonus.magicalPenetration = magicalPenetrationBonus;

        enhancedCombatBonus.criticalChance = criticalChanceBonus;

        enhancedCombatBonus.criticalMultiplier = criticalMultiplierBonus;

        enhancedCombatBonus.damageAmplification = damageAmplificationBonus; 
        isEnhancedAwakeningUnlocked = true;

    }


    //만월 장착 해제
    public void ClearEnhancedAwakeningBonus()
    {
        isEnhancedAwakeningUnlocked = false;

        //각성 연출 도중 해제된 경우
        //강화 각성을 일반 각성으로 변경
        if (isAwakening && !isAwakened)
        {
            isEnhancedAwakening = false;
        }

        //강화 각성 도중 만월이 해제되면
        //만월 추가 보너스만 즉시 제거
        if (isAwakened && isEnhancedAwakening)
        {
            RemoveEnhancedCombatBonus();

            isEnhancedAwakening = false;

            if (activePlayer != null && activePlayer.sr != null)
            {
                activePlayer.sr.color = awakenedColor;
            }
        }

        enhancedCombatBonus.Clear();

    }
}