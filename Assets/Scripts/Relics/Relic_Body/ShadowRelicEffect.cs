
using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_Shadow",
    menuName = "Relics/Effects/Shadow"
)]

//그림자 유물
//장착 시 지정한 슬롯에 특정 스킬을 지급한다.
public class ShadowRelicEffect : RelicEffect
{
    [Header("Shadow Skill")]
    [Tooltip("그림자 유물이 지급할 스킬")]
    [SerializeField]
    private SkillData shadowSkill;

    [Tooltip("스킬을 지급할 슬롯: 0=X, 1=A, 2=S, 3=D, 4=F")]
    [SerializeField]
    [Range(0, 4)]
    private int skillSlotIndex = 4;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new ShadowRelicRuntime(player, shadowSkill, skillSlotIndex);
    }

    private sealed class ShadowRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly SkillData shadowSkill;
        private readonly int skillSlotIndex;

        private PlayerSkill playerSkill;
        private bool isEquipped;

        public ShadowRelicRuntime(Player player, SkillData shadowSkill, int skillSlotIndex)
        {
            this.player = player;
            this.shadowSkill = shadowSkill;
            this.skillSlotIndex = skillSlotIndex;
        }

        public void Equip()
        {
            if (isEquipped) return;
            playerSkill = player.GetComponent<PlayerSkill>();

            bool equipped = playerSkill.EquipSkill(skillSlotIndex, shadowSkill);

            isEquipped = true;

            Debug.Log(
                $"[Shadow] 그림자 유물 장착 - " +
                $"{skillSlotIndex}번 슬롯에 스킬 지급"
            );
        }

        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerSkill != null && shadowSkill != null)
            {
                //그림자 유물이 지급한 스킬과 일치할 때만 제거
                playerSkill.UnequipSkill(skillSlotIndex, shadowSkill);
            }

            isEquipped = false;
            playerSkill = null;

            Debug.Log("[Shadow] 그림자 유물 해제 - 지급 스킬 제거");

        }
    }
}