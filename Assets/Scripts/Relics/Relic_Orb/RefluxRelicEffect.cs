
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Reflux", menuName = "Relics/Effects/Reflux")]

//환류
//각성이 종료되면 기력을 일정량 회복한다.
public class RefluxRelicEffect : RelicEffect
{
    [Header("Reflux Settings")]
    [SerializeField, Min(0f)]
    private float recoveryEnergy = 30f;


    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new RefluxRuntime(
            player,
            recoveryEnergy
        );
    }


    private sealed class RefluxRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float recoveryEnergy;

        private PlayerEnergy playerEnergy;
        private PlayerAwakening playerAwakening;

        private bool isEquipped;


        public RefluxRuntime(
            Player player,
            float recoveryEnergy)
        {
            this.player = player;
            this.recoveryEnergy = recoveryEnergy;
        }


        public void Equip()
        {
            if (isEquipped) return;

            if (player == null)
            {
                Debug.LogError(
                    "[Reflux] Player가 없습니다."
                );

                return;
            }

            playerEnergy =
                player.GetComponent<PlayerEnergy>();

            playerAwakening =
                player.GetComponent<PlayerAwakening>();


            if (playerEnergy == null)
            {
                Debug.LogError(
                    "[Reflux] PlayerEnergy를 찾지 못했습니다."
                );

                return;
            }

            if (playerAwakening == null)
            {
                Debug.LogError(
                    "[Reflux] PlayerAwakening을 찾지 못했습니다."
                );

                return;
            }


            //각성 종료 이벤트 구독
            playerAwakening.OnAwakeningEnded +=
                HandleAwakeningEnded;

            isEquipped = true;

            Debug.Log(
                $"[Reflux] 환류 장착 - " +
                $"각성 종료 시 기력 {recoveryEnergy} 회복"
            );
        }


        public void Unequip()
        {
            if (!isEquipped) return;

            //이벤트 구독 해제
            if (playerAwakening != null)
            {
                playerAwakening.OnAwakeningEnded -=
                    HandleAwakeningEnded;
            }

            isEquipped = false;

            playerEnergy = null;
            playerAwakening = null;

            Debug.Log(
                "[Reflux] 환류 장착 해제"
            );
        }


        //각성 종료 시 호출
        private void HandleAwakeningEnded()
        {
            if (!isEquipped) return;
            if (playerEnergy == null) return;

            playerEnergy.GainEnergy(
                recoveryEnergy
            );

            Debug.Log(
                $"[Reflux] 각성 종료 - " +
                $"기력 {recoveryEnergy} 회복"
            );
        }
    }
}