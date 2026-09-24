using UnityEngine;

[CreateAssetMenu(fileName = "RFX_FullMoon", menuName = "Relics/Effects/Full Moon")]

//만월
//기력 획득을 늘리고 게이지가 가득 찼을 때만 피해량을 높인다.
public class FullMoonRelicEffect : RelicEffect
{
    [Header("Full Moon Settings")]
    [SerializeField, Min(0f), Tooltip("기력 추가 획득 비율. 0.5는 획득량 50% 증가")]
    private float energyGainBonus = 0.5f;

    [SerializeField, Min(0f), Tooltip("기력이 최대일 때의 최종 피해량 보너스")]
    private float fullEnergyDamageBonus = 0.2f;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new FullMoonRuntime(player, energyGainBonus, fullEnergyDamageBonus);
    }

    private sealed class FullMoonRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float gainBonus;
        private readonly float damageBonus;
        private PlayerEnergy energy;
        private OffensiveStats offense;
        private bool equipped;
        private bool damageApplied;

        public FullMoonRuntime(Player player, float gainBonus, float damageBonus)
        {
            this.player = player;
            this.gainBonus = gainBonus;
            this.damageBonus = damageBonus;
        }

        //기력 획득량을 높이고 게이지가 변경될 때마다 최대 충전 여부를 검사
        public void Equip()
        {
            if (equipped || player == null) return;

            energy = player.Energy;
            offense = player.Stats != null ? player.Stats.Offense : null;

            if (energy == null || offense == null) return;

            equipped = true;

            energy.ModifyEnergyGainMultiplier(gainBonus);
            energy.OnEnergyChanged += UpdateFullEnergyBonus;

            UpdateFullEnergyBonus();
        }

        //이벤트를 먼저 끊고 획득 및 피해 보너스를 각각 원상복구
        public void Unequip()
        {
            if (!equipped) return;

            if (energy != null)
            {
                energy.OnEnergyChanged -= UpdateFullEnergyBonus;
                energy.ModifyEnergyGainMultiplier(-gainBonus);
            }

            if (damageApplied && offense != null) offense.damageAmplification -= damageBonus;

            damageApplied = false;
            equipped = false;
            energy = null;
            offense = null;

        }

        //최대 기력에 도달하면 보너스를 더하고 소모하면 정확히 한 번 제거
        private void UpdateFullEnergyBonus()
        {
            if (!equipped || energy == null || offense == null) return;

            bool shouldApply = energy.IsFull;

            if (shouldApply == damageApplied) return;

            offense.damageAmplification += shouldApply ? damageBonus : -damageBonus;

            damageApplied = shouldApply;
        }
    }
}
