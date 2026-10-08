using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_Regeneration",
    menuName = "Relics/Effects/Regeneration"
)]

// 장착 중 일정한 주기마다 플레이어의 체력을 회복하는 신체 유물 효과
public class RegenerationRelicEffect : RelicEffect
{
    [Header("Regeneration Settings")]
    [Tooltip("체력을 회복하는 주기(초)")]
    [SerializeField, Min(0.1f)]
    private float healInterval = 1f;

    [Tooltip("한 번의 틱마다 회복하는 체력")]
    [SerializeField, Min(0f)]
    private float healAmount = 0.1f;

    // 플레이어별 회복 유물 Runtime 생성
    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new RegenerationRelicRuntime(
            player,
            healInterval,
            healAmount
        );
    }

    private sealed class RegenerationRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly float healInterval;
        private readonly float healAmount;

        private PlayerHealth playerHealth;
        private Coroutine regenerationCoroutine;
        private bool isEquipped;

        // 회복 유물에 필요한 플레이어와 회복 설정 보관
        public RegenerationRelicRuntime(
            Player player,
            float healInterval,
            float healAmount)
        {
            this.player = player;
            this.healInterval = healInterval;
            this.healAmount = healAmount;
        }

        // 회복 코루틴 시작
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null)
                throw new InvalidOperationException("[Regeneration] Player가 없습니다.");

            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                throw new InvalidOperationException("[Regeneration] PlayerHealth가 없습니다.");

            isEquipped = true;
            regenerationCoroutine = player.StartCoroutine(RegenerationRoutine());
        }

        // 회복 코루틴 정지
        public void Unequip()
        {
            isEquipped = false;

            if (regenerationCoroutine != null && player != null)
            {
                player.StopCoroutine(regenerationCoroutine);
                regenerationCoroutine = null;
            }

            playerHealth = null;
        }

        // 설정한 주기마다 체력 회복
        private IEnumerator RegenerationRoutine()
        {
            while (isEquipped)
            {
                yield return new WaitForSeconds(healInterval);

                if (!isEquipped || playerHealth == null) yield break;

                playerHealth.Heal(healAmount);
            }
        }
    }
}
