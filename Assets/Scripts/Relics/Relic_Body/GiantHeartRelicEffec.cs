
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_GiantHeart", menuName = "Relics/Effects/Giant Heart")]

//거인의 심장
//장착 시 최대 체력을 증가시킨다.
public class GiantHeartRelicEffect : RelicEffect
{
    [Header("Giant Heart")]
    [Tooltip("장착 시 증가할 최대 체력")]
    [SerializeField]
    [Min(1)]
    private int bonusMaxHealth = 1;


    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new GiantHeartRuntime(player, bonusMaxHealth);
    }


    private sealed class GiantHeartRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly int bonusMaxHealth;

        private PlayerHealth playerHealth;
        private bool isEquipped;


        public GiantHeartRuntime(Player player, int bonusMaxHealth)
        {
            this.player = player;
            this.bonusMaxHealth = bonusMaxHealth;
        }


        public void Equip()
        {
            if (isEquipped) return;

            playerHealth = player.GetComponent<PlayerHealth>();

            //최대 체력 증가 및 증가량만큼 현재 체력 회복
            playerHealth.AddMaxHealth(bonusMaxHealth, true);
            
            isEquipped = true;

            Debug.Log(
                $"[GiantHeart] 거인의 심장 장착 - " +
                $"최대 체력 +{bonusMaxHealth}"
            );
        }


        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerHealth != null)
            {
                playerHealth.RemoveMaxHealth(bonusMaxHealth);
            }

            isEquipped = false;
            playerHealth = null;

            Debug.Log("[GiantHeart] 거인의 심장 장착 해제");
        }
    }
}