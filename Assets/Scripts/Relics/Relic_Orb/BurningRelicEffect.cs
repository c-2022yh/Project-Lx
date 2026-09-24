using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Burning", menuName = "Relics/Effects/Burning")]

//연소
//각성을 길게 유지하고, 각성 중 주기적으로 공격력이 증가한다.
public class BurningRelicEffect : RelicEffect
{
    [Header("Awakening")]
    [SerializeField, Min(0f)] private float durationBonus = 10f;

    [Header("Attack Gain")]
    [SerializeField, Min(0.01f)] private float interval = 1f;
    [SerializeField, Min(0f)] private float physicalAttackPerInterval = 0.2f;
    [SerializeField, Min(0f)] private float magicalAttackPerInterval = 0.2f;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new BurningRuntime(player, durationBonus, interval,
            physicalAttackPerInterval, magicalAttackPerInterval);
    }

    private sealed class BurningRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float durationBonus;
        private readonly float interval;
        private readonly float physicalStep;
        private readonly float magicalStep;
        private PlayerAwakening awakening;
        private OffensiveStats offense;
        private Coroutine gainRoutine;
        private float appliedPhysical;
        private float appliedMagical;
        private bool equipped;

        public BurningRuntime(Player player, float durationBonus, float interval, float physicalStep, float magicalStep)
        {
            this.player = player;
            this.durationBonus = durationBonus;
            this.interval = interval;
            this.physicalStep = physicalStep;
            this.magicalStep = magicalStep;
        }

        //각성 시작/종료에 구독하고 다음 각성의 지속시간을 늘린다.
        public void Equip()
        {
            if (equipped || player == null) return;

            awakening = player.Awakening;
            offense = player.Stats != null ? player.Stats.Offense : null;

            if (awakening == null || offense == null) return;

            equipped = true;
            awakening.ModifyAwakeningDuration(durationBonus);

            awakening.OnAwakeningStarted += HandleStart;
            awakening.OnAwakeningEnded += HandleEnd;

            if (awakening.IsAwakened) HandleStart();

        }

        //이벤트와 코루틴을 정리하고 쌓인 공격력만큼 정확히 뺀다.
        public void Unequip()
        {
            if (!equipped) return;

            if (awakening != null)
            {
                awakening.OnAwakeningStarted -= HandleStart;
                awakening.OnAwakeningEnded -= HandleEnd;

                awakening.ModifyAwakeningDuration(-durationBonus);
            }
            StopGain();

            RemoveAttackGain();

            equipped = false;

            awakening = null;

            offense = null;
        }

        //각성 한 번당 공격력 누적 코루틴을 하나만 시작한다.
        private void HandleStart()
        {
            if (!equipped || gainRoutine != null) return;

            gainRoutine = player.StartCoroutine(GainAttackWhileAwakened());
        }

        //각성이 종료되면 반복 증가를 중단하고 증가분을 모두 지운다.
        private void HandleEnd()
        {
            StopGain();
            RemoveAttackGain();
        }

        //설정한 간격마다 물리 및 마법 공격력을 하나씩 누적한다.
        private IEnumerator GainAttackWhileAwakened()
        {
            while (equipped && awakening != null && awakening.IsAwakened)
            {
                yield return new WaitForSeconds(interval);

                if (!equipped || awakening == null || !awakening.IsAwakened || offense == null) break;

                offense.physicalAttack += physicalStep;
                offense.magicalAttack += magicalStep;
                appliedPhysical += physicalStep;
                appliedMagical += magicalStep;
            }
            gainRoutine = null;
        }

        //코루틴이 다음 프레임에 다시 스탯을 올리지 않도록 중지한다.
        private void StopGain()
        {
            if (gainRoutine == null || player == null) return;

            player.StopCoroutine(gainRoutine);

            gainRoutine = null;
        }

        //다른 효과의 스탯을 보존하면서 연소가 실제로 올린 수치만 제거한다.
        private void RemoveAttackGain()
        {
            if (offense != null)
            {
                offense.physicalAttack -= appliedPhysical;
                offense.magicalAttack -= appliedMagical;
            }

            appliedPhysical = 0f;
            appliedMagical = 0f;
        }
    }
}
