using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Reflux", menuName = "Relics/Effects/Reflux")]

//환류
//각성이 종료되면 기력을 일정량 회복한다.
public class RefluxRelicEffect : RelicEffect
{
    [Header("Reflux Settings")]
    [SerializeField, Min(0f)]
    private float recoveryEnergy = 30f;


    [Header("Audio")]
    [SerializeField] private SoundCue recoverySound = new SoundCue();

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new RefluxRuntime(
            player,
            recoveryEnergy, this
        );
    }


    private sealed class RefluxRuntime : IRelicRuntime
    {
        private readonly RefluxRelicEffect audioSettings;
        private readonly Player player;
        private readonly float recoveryEnergy;

        private PlayerEnergy playerEnergy;
        private PlayerAwakening playerAwakening;

        private bool isEquipped;


        public RefluxRuntime(
            Player player,
            float recoveryEnergy, RefluxRelicEffect audioSettings)
        {
            this.player = player;
            this.audioSettings = audioSettings;
            this.recoveryEnergy = recoveryEnergy;
        }


        public void Equip()
        {
            if (isEquipped) return;

            if (player == null) return;
            
            playerEnergy = player.GetComponent<PlayerEnergy>();
            playerAwakening = player.GetComponent<PlayerAwakening>();


            if (playerEnergy == null) return;
            
            if (playerAwakening == null) return;

            //각성 종료 이벤트 구독
            playerAwakening.OnAwakeningEnded += HandleAwakeningEnded;

            isEquipped = true;

        }


        public void Unequip()
        {
            if (!isEquipped) return;

            //이벤트 구독 해제
            if (playerAwakening != null)
            {
                playerAwakening.OnAwakeningEnded -= HandleAwakeningEnded;
            }

            isEquipped = false;

            playerEnergy = null;
            playerAwakening = null;

        }


        //각성 종료 시 호출
        private void HandleAwakeningEnded()
        {
            if (!isEquipped) return;
            if (playerEnergy == null) return;

            float before = playerEnergy.CurrentEnergy;
            playerEnergy.GainEnergy(recoveryEnergy);
            if (playerEnergy.CurrentEnergy > before && (player.Health == null || !player.Health.IsDead)) GameAudio.Play(audioSettings.recoverySound, player.transform.position, player.transform);

        }
    }
}