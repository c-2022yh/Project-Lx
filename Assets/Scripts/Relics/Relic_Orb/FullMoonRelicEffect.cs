using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "RFX_FullMoon", menuName = "Relics/Effects/Full Moon")]

//만월
//기력 획득을 늘리고 게이지가 가득 찼을 때만 기본 공격 피해량을 높인다.
public class FullMoonRelicEffect : RelicEffect
{
    [Header("Full Moon Settings")]
    [SerializeField, Min(0f), Tooltip("기력 추가 획득 비율. 0.5는 획득량 50% 증가")]
    private float energyGainBonus = 0.5f;

    [FormerlySerializedAs("fullEnergyDamageBonus")]
    [SerializeField, Min(0f), Tooltip("기력이 최대일 때 기본 공격 피해 증가 비율. 0.2는 20% 증가. 지상/공중 기본 공격에만 적용")]
    private float fullEnergyBasicAttackBonus = 0.2f;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new FullMoonRuntime(player, energyGainBonus, fullEnergyBasicAttackBonus);
    }

    private sealed class FullMoonRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float gainBonus;
        private readonly float damageBonus;
        private PlayerEnergy energy;
        private PlayerAttack attack;
        private PlayerAwakening awakening;
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
            attack = player.GetComponent<PlayerAttack>();
            awakening = player.GetComponent<PlayerAwakening>();

            if (energy == null || attack == null || awakening == null) return;

            equipped = true;

            awakening.SetAwakeningBlocked(true);
            if (awakening.IsAwakened || awakening.IsAwakening) awakening.EndAwakening();

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

            if (awakening != null) awakening.SetAwakeningBlocked(false);

            if (damageApplied && attack != null) attack.ModifyBasicAttackDamageBonus(-damageBonus);

            damageApplied = false;
            equipped = false;
            energy = null;
            attack = null;
            awakening = null;

        }

        //최대 기력에 도달하면 보너스를 더하고 소모하면 정확히 한 번 제거
        private void UpdateFullEnergyBonus()
        {
            if (!equipped || energy == null || attack == null) return;

            bool shouldApply = energy.IsFull;

            if (shouldApply == damageApplied) return;

            attack.ModifyBasicAttackDamageBonus(shouldApply ? damageBonus : -damageBonus);

            damageApplied = shouldApply;
        }
    }
}
