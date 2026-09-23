using UnityEngine;

[CreateAssetMenu(fileName = "RFX_DarkMoon", menuName = "Relics/Effects/Dark Moon")]

//그믐
//최대 기력을 낮춰 각성을 막고 평상시 피해량을 높인다.
public class DarkMoonRelicEffect : RelicEffect
{
    [Header("Dark Moon Settings")]
    [SerializeField, Min(0f), Tooltip("최대 기력에서 차감할 양. 각성 요구량 미만이 되도록 설정")]
    private float maxEnergyReduction = 50f;

    [SerializeField, Min(0f), Tooltip("OffensiveStats.damageAmplification에 더할 값. 0.25는 25% 보너스")]
    private float damageAmplificationBonus = 0.25f;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new DarkMoonRuntime(player, maxEnergyReduction, damageAmplificationBonus);
    }

    private sealed class DarkMoonRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float reduction;
        private readonly float damageBonus;
        private PlayerEnergy energy;
        private PlayerAwakening awakening;
        private OffensiveStats offense;
        private float appliedReduction;
        private bool equipped;

        public DarkMoonRuntime(Player player, float reduction, float damageBonus)
        {
            this.player = player;
            this.reduction = reduction;
            this.damageBonus = damageBonus;
        }

        public void Equip()
        {
            if (equipped || player == null) return;

            energy = player.Energy;
            awakening = player.Awakening;

            offense = player.Stats != null ? player.Stats.Offense : null;

            if (energy == null || awakening == null || offense == null) return;

            awakening.SetAwakeningBlocked(true);

            if (awakening.IsAwakened || awakening.IsAwakening) awakening.EndAwakening();

            appliedReduction = Mathf.Min(reduction, energy.MaxEnergy);
            energy.ModifyMaxEnergy(-appliedReduction);
            offense.damageAmplification += damageBonus;

            equipped = true;
        }

        public void Unequip()
        {
            if (!equipped) return;

            if (offense != null) offense.damageAmplification -= damageBonus;
            if (energy != null) energy.ModifyMaxEnergy(appliedReduction);
            if (awakening != null) awakening.SetAwakeningBlocked(false);

            appliedReduction = 0f;

            equipped = false;
            energy = null;
            awakening = null;
            offense = null;
        }
    }
}
