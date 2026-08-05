
using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_FullMoon",
    menuName = "Relics/Effects/Full Moon"
)]

//만월
//최대 기력을 증가시키고 강화 각성을 해금한다.
public class FullMoonRelicEffect : RelicEffect
{
    [Header("Energy")]
    [SerializeField, Min(1f)]
    private float bonusMaxEnergy = 100f;


    [Header("Enhanced Awakening - Attack")]
    [SerializeField, Min(0f)]
    private float physicalAttackBonus = 1f;

    [SerializeField, Min(0f)]
    private float magicalAttackBonus = 1f;


    [Header("Enhanced Awakening - Penetration")]
    [SerializeField, Min(0f)]
    private float physicalPenetrationBonus = 10f;

    [SerializeField, Min(0f)]
    private float magicalPenetrationBonus = 10f;


    [Header("Enhanced Awakening - Critical")]
    [SerializeField, Range(0f, 1f)]
    private float criticalChanceBonus = 0.2f;

    [SerializeField, Min(0f)]
    private float criticalMultiplierBonus = 0.5f;


    [Header("Enhanced Awakening - Final Damage")]
    [SerializeField, Min(0f)]
    private float damageAmplificationBonus = 0.2f;


    public override IRelicRuntime CreateRuntime(
        Player player)
    {
        return new FullMoonRuntime(
            player,
            bonusMaxEnergy,
            physicalAttackBonus,
            magicalAttackBonus,
            physicalPenetrationBonus,
            magicalPenetrationBonus,
            criticalChanceBonus,
            criticalMultiplierBonus,
            damageAmplificationBonus
        );
    }


    private sealed class FullMoonRuntime :
        IRelicRuntime
    {
        private readonly Player player;

        private readonly float bonusMaxEnergy;

        private readonly float physicalAttackBonus;
        private readonly float magicalAttackBonus;

        private readonly float physicalPenetrationBonus;
        private readonly float magicalPenetrationBonus;

        private readonly float criticalChanceBonus;
        private readonly float criticalMultiplierBonus;

        private readonly float damageAmplificationBonus;


        private PlayerEnergy playerEnergy;
        private PlayerAwakening playerAwakening;

        private bool isEquipped;


        public FullMoonRuntime(
            Player player,
            float bonusMaxEnergy,
            float physicalAttackBonus,
            float magicalAttackBonus,
            float physicalPenetrationBonus,
            float magicalPenetrationBonus,
            float criticalChanceBonus,
            float criticalMultiplierBonus,
            float damageAmplificationBonus)
        {
            this.player = player;
            this.bonusMaxEnergy = bonusMaxEnergy;
            this.physicalAttackBonus = physicalAttackBonus;
            this.magicalAttackBonus = magicalAttackBonus;
            this.physicalPenetrationBonus = physicalPenetrationBonus;
            this.magicalPenetrationBonus = magicalPenetrationBonus;
            this.criticalChanceBonus = criticalChanceBonus;
            this.criticalMultiplierBonus = criticalMultiplierBonus;
            this.damageAmplificationBonus = damageAmplificationBonus;
        }


        public void Equip()
        {
            if (isEquipped) return;
            
            playerEnergy = player.GetComponent<PlayerEnergy>();
            playerAwakening = player.GetComponent<PlayerAwakening>();

            //최대 기력 증가
            playerEnergy.ModifyMaxEnergy(bonusMaxEnergy );


            //강화 각성 능력치 등록
            playerAwakening.SetEnhancedAwakeningBonus(
                physicalAttackBonus,
                magicalAttackBonus,
                physicalPenetrationBonus,
                magicalPenetrationBonus,
                criticalChanceBonus,
                criticalMultiplierBonus,
                damageAmplificationBonus
            );

            isEquipped = true;
        }


        public void Unequip()
        {
            if (!isEquipped) return;


            //강화 각성 등록 해제
            if (playerAwakening != null)
            {
                playerAwakening.ClearEnhancedAwakeningBonus();
            }


            //최대 기력 복구
            if (playerEnergy != null)
            {
                playerEnergy.ModifyMaxEnergy(-bonusMaxEnergy);
            }


            isEquipped = false;

            playerEnergy = null;
            playerAwakening = null;


            Debug.Log("[FullMoon] 만월 장착 해제");

        }
    }
}