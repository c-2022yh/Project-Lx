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
        [Range(0f, 1f)] public float criticalChance;
        [Min(0f)] public float criticalMultiplier;

        [Header("Final Damage")]
        [Min(0f)] public float damageAmplification;

        //현재 값을 복사해서 각성 종료 시 적용 수치를 보존
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

        //전투 능력치를 적용하거나 제거
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
    }

    [Header("Awakening Settings")]
    [SerializeField, Min(0f)] private float awakeningFreezeTime = 0.8f;

    //각성에 필요한 최소 기력
    [SerializeField, Min(0f)] private float awakeningRequiredEnergy = 100f;

    //각성 중 초당 소모되는 기력
    [SerializeField, Min(0f)] private float awakeningEnergyDrainPerSecond = 5f;

    [Header("Awakening Color")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color awakenedColor = Color.red;

    [Header("Movement Bonus")]
    [SerializeField, Min(0.01f)] private float awakenedMoveMultiplier = 1.3f;
    [SerializeField, Min(0.01f)] private float awakenedJumpMultiplier = 1.2f;

    [Header("Awakening Combat Bonus")]
    [SerializeField] private CombatBonus awakeningCombatBonus = new();

    [Header("Effect")]
    [SerializeField] private AwakeningEffect awakeningEffect;

    //현재 각성 상태
    private bool isAwakened;

    //각성 연출 중인 상태
    private bool isAwakening;

    //각성 시도를 차단하는 상태
    private bool awakeningBlocked;

    //각성 시작과 종료 시 발생하는 이벤트
    public event Action OnAwakeningStarted;
    public event Action OnAwakeningEnded;

    //실제로 적용한 이동 배율
    private float appliedMoveMultiplier = 1f;
    private float appliedJumpMultiplier = 1f;

    //각성 종료 시 제거할 전투 보너스
    private CombatBonus appliedAwakeningCombatBonus;

    private OffensiveStats activeOffense;
    private Player activePlayer;
    private Coroutine awakeningCoroutine;

    public bool IsAwakened => isAwakened;
    public bool IsAwakening => isAwakening;

    //각성 진입을 시도
    public void TryAwaken(Player p)
    {
        if (p == null) return;
        if (p.Energy == null) return;
        if (awakeningBlocked) return;

        //이미 각성 중이거나 각성 연출 중이면 실행하지 않음
        if (isAwakened || isAwakening) return;

        //최대 기력과 현재 기력이 모두 충분해야 각성 가능
        if (p.Energy.MaxEnergy < awakeningRequiredEnergy) return;
        if (!p.Energy.HasEnergy(awakeningRequiredEnergy)) return;

        activePlayer = p;
        awakeningCoroutine = StartCoroutine(AwakeningRoutine(p));
    }

    //각성 연출과 기력 소모를 처리
    private IEnumerator AwakeningRoutine(Player p)
    {
        isAwakening = true;

        p.ActionState.EnterAwakening();
        p.SetPhysicsFreeze(true);

        if (awakeningEffect != null)
        {
            awakeningEffect.SetAndShow(p.transform.position);
        }

        yield return new WaitForSeconds(awakeningFreezeTime);

        p.SetPhysicsFreeze(false);

        EnterAwakened(p);

        isAwakening = false;

        if (p.ActionState.isAwakening)
        {
            p.ActionState.EnterNormal();
        }

        //기력이 남아 있는 동안 각성 상태 유지
        while (isAwakened && p.Energy.CurrentEnergy > 0f)
        {
            p.Energy.DrainEnergy(awakeningEnergyDrainPerSecond * Time.deltaTime);
            yield return null;
        }

        if (isAwakened)
        {
            ExitAwakened(p);
        }

        awakeningCoroutine = null;
        activePlayer = null;
    }

    //각성 능력치를 적용
    private void EnterAwakened(Player p)
    {
        isAwakened = true;

        appliedMoveMultiplier = awakenedMoveMultiplier;
        appliedJumpMultiplier = awakenedJumpMultiplier;

        p.Move.moveSpeed *= appliedMoveMultiplier;
        p.Move.jumpForce *= appliedJumpMultiplier;

        ApplyAwakeningCombatBonus(p);

        if (p.sr != null)
        {
            p.sr.color = awakenedColor;
        }

        OnAwakeningStarted?.Invoke();
    }

    //플레이어의 공격 능력치 참조를 연결
    private bool TryGetOffensiveStats(Player p)
    {
        PlayerStats playerStats = p.GetComponent<PlayerStats>();

        activeOffense = playerStats != null ? playerStats.Offense : null;

        return activeOffense != null;
    }

    //각성 전투 보너스를 적용
    private void ApplyAwakeningCombatBonus(Player p)
    {
        if (!TryGetOffensiveStats(p)) return;

        appliedAwakeningCombatBonus = awakeningCombatBonus.Copy();
        appliedAwakeningCombatBonus.ApplyTo(activeOffense, 1f);
    }

    //각성 전투 보너스를 제거
    private void RemoveAwakeningCombatBonus()
    {
        if (activeOffense == null) return;
        if (appliedAwakeningCombatBonus == null) return;

        appliedAwakeningCombatBonus.ApplyTo(activeOffense, -1f);
        appliedAwakeningCombatBonus = null;
    }

    //각성 상태를 종료하고 능력치를 복구
    private void ExitAwakened(Player p)
    {
        isAwakened = false;

        if (p.sr != null)
        {
            p.sr.color = normalColor;
        }

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

        RemoveAwakeningCombatBonus();

        activeOffense = null;

        //각성이 끝나면 남은 기력도 비움
        p.Energy.ResetEnergy();

        OnAwakeningEnded?.Invoke();
    }

    //유물 등 외부에서 각성 시도를 차단
    public void SetAwakeningBlocked(bool blocked)
    {
        awakeningBlocked = blocked;
    }

    //기존 시간제 유물 코드와의 호환을 위한 함수
    public void ModifyAwakeningDuration(float amount)
    {
        //기력 기반 각성에서는 지속시간을 직접 수정하지 않음
    }

    //각성 중이거나 각성 연출 중인 상태를 강제로 종료
    public void EndAwakening()
    {
        if (!isAwakened && !isAwakening) return;

        if (awakeningCoroutine != null)
        {
            StopCoroutine(awakeningCoroutine);
            awakeningCoroutine = null;
        }

        if (isAwakening && activePlayer != null)
        {
            activePlayer.SetPhysicsFreeze(false);

            if (activePlayer.ActionState.isAwakening)
            {
                activePlayer.ActionState.EnterNormal();
            }
        }

        isAwakening = false;

        if (isAwakened && activePlayer != null)
        {
            ExitAwakened(activePlayer);
        }

        activePlayer = null;
    }
}
