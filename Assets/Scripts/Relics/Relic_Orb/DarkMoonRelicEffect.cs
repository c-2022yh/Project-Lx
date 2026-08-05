
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_DarkMoon", menuName = "Relics/Effects/Dark Moon")]

//그믐
//기력을 획득할 수 없는 대신 상시 피해 증폭을 얻는다.
public class DarkMoonRelicEffect : RelicEffect
{
    [Header("Dark Moon Settings")]
    [SerializeField, Min(0f)]
    private float damageAmplificationBonus = 0.25f;


    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new DarkMoonRuntime(player, damageAmplificationBonus);
    }

    private sealed class DarkMoonRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float damageAmplificationBonus;

        private PlayerEnergy playerEnergy;
        private OffensiveStats playerOffense;

        private bool isEquipped;


        public DarkMoonRuntime(Player player, float damageAmplificationBonus)
        {
            this.player = player;
            this.damageAmplificationBonus = damageAmplificationBonus;
        }


        public void Equip()
        {
            if (isEquipped) return;

            playerEnergy = player.GetComponent<PlayerEnergy>();

            PlayerStats playerStats = player.GetComponent<PlayerStats>();

            playerOffense = playerStats.Offense;

            //현재 기력을 0으로 만들고
            //이후 기력 획득도 차단
            playerEnergy.SetEnergyGainBlocked(true);

            //상시 피해 증폭 적용
            playerOffense.damageAmplification += damageAmplificationBonus;

            isEquipped = true;


            Debug.Log(
                $"[DarkMoon] 그믐 장착 - " +
                $"기력 획득 차단, " +
                $"피해 증폭 +{damageAmplificationBonus}"
            );
        }


        public void Unequip()
        {
            if (!isEquipped) return;


            //상시 피해 증폭 제거
            if (playerOffense != null)
            {
                playerOffense.damageAmplification -= damageAmplificationBonus;
            }

            //기력 획득 차단 해제
            if (playerEnergy != null)
            {
                playerEnergy.SetEnergyGainBlocked(false);
            }


            isEquipped = false;

            playerEnergy = null;
            playerOffense = null;

            Debug.Log("[DarkMoon] 그믐 장착 해제");


        }
    }
}