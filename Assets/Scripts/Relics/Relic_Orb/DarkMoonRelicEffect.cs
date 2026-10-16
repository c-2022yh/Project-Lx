using UnityEngine;

[CreateAssetMenu(fileName = "RFX_DarkMoon", menuName = "Relics/Effects/Dark Moon")]

//그믐
//최대 기력을 낮춰 각성을 막고 평상시 피해량을 높인다.
public class DarkMoonRelicEffect : RelicEffect
{
    [Header("Dark Moon Settings")]
    [SerializeField, Min(0f), Tooltip("장착 중 실제 기력 상한. 기본 최대 기력과 별도로 적용")]
    private float maxEnergyCap = 30f;

    [SerializeField, Min(0f), Tooltip("OffensiveStats.damageAmplification에 더할 값. 0.25는 25% 보너스")]
    private float damageAmplificationBonus = 0.25f;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new DarkMoonRuntime(player, maxEnergyCap, damageAmplificationBonus);
    }

    private sealed class DarkMoonRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float cap;
        private readonly float damageBonus;
        private PlayerEnergy energy;
        private PlayerAwakening awakening;
        private OffensiveStats offense;
        private bool equipped;

        public DarkMoonRuntime(Player player, float cap, float damageBonus)
        {
            this.player = player;
            this.cap = cap;
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

            energy.SetMaxEnergyLimit(this, cap);
            offense.damageAmplification += damageBonus;

            equipped = true;
        }

        public void Unequip()
        {
            if (!equipped) return;

            if (offense != null) offense.damageAmplification -= damageBonus;
            if (energy != null) energy.RemoveMaxEnergyLimit(this);
            if (awakening != null) awakening.SetAwakeningBlocked(false);

            equipped = false;
            energy = null;
            awakening = null;
            offense = null;
        }
    }
}
