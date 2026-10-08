using UnityEngine;

//일반 스킬 지급 유물의 공통 동작
public abstract class SkillGrantRelicEffectBase : RelicEffect
{
    protected abstract SkillData Skill { get; }
    protected virtual string LogTag => "SkillGrant";

    //플레이어별 스킬 지급 Runtime 생성
    public sealed override IRelicRuntime CreateRuntime(Player player)
    {
        return new SkillGrantRelicRuntime(player, Skill, LogTag);
    }

    private sealed class SkillGrantRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly SkillData skill;
        private readonly string logTag;

        private PlayerSkill playerSkill;
        private bool isEquipped;

        public SkillGrantRelicRuntime(Player player, SkillData skill, string logTag)
        {
            this.player = player;
            this.skill = skill;
            this.logTag = logTag;
        }

        //스킬을 지급하고 지급 출처를 등록
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) throw new System.InvalidOperationException($"[{logTag}] Player가 없습니다.");

            playerSkill = player.GetComponent<PlayerSkill>();

            if (playerSkill == null || skill == null)
                throw new System.InvalidOperationException($"[{logTag}] 스킬 또는 PlayerSkill이 없습니다.");

            if (!playerSkill.GrantSkill(skill, this))
                throw new System.InvalidOperationException($"[{logTag}] 일반 스킬을 지급하지 못했습니다.");

            isEquipped = true;

            Debug.Log(
                $"[{logTag}] 스킬 지급 유물 장착 - " +
                $"{skill.name} 보유 및 빈 슬롯 자동 장착"
            );
        }

        //자신이 지급한 스킬만 회수
        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerSkill != null && skill != null)
            {
                playerSkill.RevokeSkill(skill, this);
            }

            isEquipped = false;
            playerSkill = null;

            Debug.Log($"[{logTag}] 스킬 지급 유물 해제 - 지급 스킬 제거");
        }
    }
}

[CreateAssetMenu(
    fileName = "RFX_SkillGrant",
    menuName = "Relics/Effects/Skill Grant"
)]

//인스펙터에서 SkillData를 연결하는 일반 스킬 지급 유물 효과
public class SkillGrantRelicEffect : SkillGrantRelicEffectBase
{
    [Header("Skill Grant")]
    [Tooltip("이 유물이 지급할 일반 스킬")]
    [SerializeField]
    private SkillData skill;

    protected override SkillData Skill => skill;
}
