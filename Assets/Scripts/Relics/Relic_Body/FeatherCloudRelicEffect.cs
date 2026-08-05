
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_FeatherCloud", menuName = "Relics/Effects/Feather Cloud")]

//깃털구름
//장착 시 공중 점프 가능 횟수를 증가시킨다.
public class FeatherCloudRelicEffect : RelicEffect
{
    [Header("Feather Cloud")]
    [Tooltip("증가할 공중 점프 횟수")]
    [SerializeField]
    [Min(1)]
    private int bonusAirJumps = 1;


    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new FeatherCloudRuntime(player, bonusAirJumps);
    }


    private sealed class FeatherCloudRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly int bonusAirJumps;

        private PlayerMove playerMove;
        private bool isEquipped;


        public FeatherCloudRuntime(Player player, int bonusAirJumps)
        {
            this.player = player;
            this.bonusAirJumps = bonusAirJumps;
        }


        public void Equip()
        {
            if (isEquipped) return;

            playerMove = player.GetComponent<PlayerMove>();

            //공중 점프 횟수 증가
            playerMove.ModifyMaxAirJumps(bonusAirJumps);

            isEquipped = true;

            Debug.Log(
                $"[FeatherCloud] 깃털구름 장착 - " +
                $"공중 점프 +{bonusAirJumps}"
            );
        }


        public void Unequip()
        {
            if (!isEquipped)  return;

            if (playerMove != null)
            {
                //증가했던 공중 점프 횟수 복구
                playerMove.ModifyMaxAirJumps(-bonusAirJumps);
            }

            isEquipped = false;
            playerMove = null;

            Debug.Log("[FeatherCloud] 깃털구름 장착 해제");


        }
    }
}