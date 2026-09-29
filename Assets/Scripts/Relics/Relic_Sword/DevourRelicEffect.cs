using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Devour", menuName = "Relics/Effects/Devour")]

public class DevourRelicEffect : RelicEffect
{
    [Header("Devour Skill")]
    [Tooltip("포식 장착 시 X 슬롯에 지급할 스킬")]
    [SerializeField]
    private SkillData devourSkill;

    [Header("Life Steal")]
    [Tooltip("기본공격 1회 적중 시 누적되는 회복량")]
    [SerializeField]
    [Range(0.01f, 1f)]
    private float healPerHit = 0.5f;

    //플레이어별 유물 효과 Runtime 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new DevourRelicRuntime(p, devourSkill, healPerHit);
    }

    private sealed class DevourRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly SkillData devourSkill;
        private readonly float healPerHit;

        private PlayerSkill playerSkill;
        private PlayerAttack playerAttack;
        private PlayerHealth playerHealth;

        private bool isEquipped;

        //포식 효과에 필요한 플레이어·스킬·회복량 보관
        public DevourRelicRuntime(Player player, SkillData devourSkill, float healPerHit)
        {
            this.player = player;
            this.devourSkill = devourSkill;
            this.healPerHit = healPerHit;
        }

        //스킬을 지급하고 유물 고유 효과의 이벤트 등록
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) throw new System.InvalidOperationException("[Devour] Player가 없습니다.");

            playerSkill = player.GetComponent<PlayerSkill>();
            playerAttack = player.GetComponent<PlayerAttack>();
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerSkill == null || playerAttack == null || playerHealth == null || devourSkill == null)
                throw new System.InvalidOperationException("[Devour] 스킬 또는 필수 컴포넌트가 없습니다.");

            if (!playerSkill.SetExclusiveSkill(devourSkill, this))
                throw new System.InvalidOperationException("[Devour] X 스킬을 지급하지 못했습니다.");

            isEquipped = true;

            playerAttack.OnAttackHit += HandleAttackHit;

            Debug.Log("[Devour] 포식 유물 장착 완료");
        }

        //유물 이벤트와 예약 작업을 정리하고 자신이 지급한 스킬 회수
        public void Unequip()
        {
            isEquipped = false;

            if (playerAttack != null)
            {
                playerAttack.OnAttackHit -= HandleAttackHit;
            }

            if (playerSkill != null)
            {
                playerSkill.ClearExclusiveSkill(this);
            }

            playerSkill = null;
            playerAttack = null;
            playerHealth = null;

            Debug.Log("[Devour] 포식 유물 장착 해제");
        }

        //공격 적중 시 최대 체력 미만이면 설정된 회복량 적용
        private void HandleAttackHit(IDamageable target, DamageInfo damageInfo)
        {
            if (!isEquipped) return;
            if (playerHealth == null) return;

            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;

            playerHealth.Heal(healPerHit);

        }
    }
}
