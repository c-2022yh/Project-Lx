using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_Shadow",
    menuName = "Relics/Effects/Shadow"
)]

public class ShadowRelicEffect : RelicEffect
{
    [Header("Shadow Skill")]
    [Tooltip("그림자 유물이 지급할 스킬")]
    [SerializeField]
    private SkillData shadowSkill;

    //플레이어별 유물 효과 Runtime 생성
    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new ShadowRelicRuntime(player, shadowSkill);
    }

    private sealed class ShadowRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly SkillData shadowSkill;

        private PlayerSkill playerSkill;
        private bool isEquipped;

        //그림자 효과에 필요한 플레이어와 스킬 보관
        public ShadowRelicRuntime(Player player, SkillData shadowSkill)
        {
            this.player = player;
            this.shadowSkill = shadowSkill;
        }

        //스킬을 지급하고 유물 고유 효과의 이벤트 등록
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) throw new System.InvalidOperationException("[Shadow] Player가 없습니다.");
            playerSkill = player.GetComponent<PlayerSkill>();

            if (playerSkill == null || shadowSkill == null)
                throw new System.InvalidOperationException("[Shadow] 스킬 또는 PlayerSkill이 없습니다.");

            if (!playerSkill.GrantSkill(shadowSkill, this))
                throw new System.InvalidOperationException("[Shadow] 일반 스킬을 지급하지 못했습니다.");

            isEquipped = true;

            Debug.Log(
                $"[Shadow] 그림자 유물 장착 - " +
                $"{shadowSkill.name} 보유 및 빈 슬롯 자동 장착"
            );
        }

        //유물 이벤트와 예약 작업을 정리하고 자신이 지급한 스킬 회수
        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerSkill != null && shadowSkill != null)
            {
                playerSkill.RevokeSkill(shadowSkill, this);
            }

            isEquipped = false;
            playerSkill = null;

            Debug.Log("[Shadow] 그림자 유물 해제 - 지급 스킬 제거");

        }
    }
}
