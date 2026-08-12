
using UnityEngine;

[CreateAssetMenu( fileName = "RFX_WindShoes", menuName = "Relics/Effects/Wind Shoes")]

//바람의 신발
//장착 시 Shift 대쉬를 사용할 수 있게 한다.
public class WindShoesRelicEffect : RelicEffect
{
    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new WindShoesRuntime(player);
    }

    private sealed class WindShoesRuntime : IRelicRuntime
    {
        private readonly Player player;

        private PlayerMove playerMove;
        private bool isEquipped;

        public WindShoesRuntime(Player player)
        {
            this.player = player;
        }

        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) return;
            

            playerMove = player.GetComponent<PlayerMove>();

            if (playerMove == null) return;
            

            //Shift 대쉬 해금
            playerMove.SetDashUnlocked(true);

            isEquipped = true;

            Debug.Log("[WindShoes] 바람의 신발 장착 - 대쉬 활성화");

        }

        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerMove != null)
            {
                //Shift 대쉬 잠금
                playerMove.SetDashUnlocked(false);
            }

            isEquipped = false;
            playerMove = null;

            Debug.Log("[WindShoes] 바람의 신발 해제 - 대쉬 비활성화");
        }
    }
}