using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "RFX_Devour", menuName = "Relics/Effects/Devour")]

//포식 유물 효과
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

    //유물 장착 시 런타임 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new DevourRelicRuntime(p, devourSkill, healPerHit);
    }

    //유물 런타임 클래스
    private sealed class DevourRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly SkillData devourSkill;
        private readonly float healPerHit;

        private PlayerSkill playerSkill;
        private PlayerAttack playerAttack;
        private PlayerHealth playerHealth;

        private bool isEquipped;

        public DevourRelicRuntime(Player player, SkillData devourSkill, float healPerHit)
        {
            this.player = player;
            this.devourSkill = devourSkill;
            this.healPerHit = healPerHit;
        }

        public void Equip()
        {
            if (player == null) return;

            playerSkill = player.GetComponent<PlayerSkill>();
            playerAttack = player.GetComponent<PlayerAttack>();
            playerHealth = player.GetComponent<PlayerHealth>();

            //X 슬롯: 인덱스 0
            bool skillEquipped = playerSkill.EquipSkill(0, devourSkill);

            isEquipped = true;

            //기본공격 적중 이벤트 구독
            playerAttack.OnAttackHit += HandleAttackHit;

            Debug.Log("[Devour] 포식 유물 장착 완료");
        }

        public void Unequip()
        {
            isEquipped = false;

            if (playerAttack != null)
            {
                playerAttack.OnAttackHit -= HandleAttackHit;
            }

            if (playerSkill != null)
            {
                playerSkill.UnequipSkill(0, devourSkill);
            }

            playerSkill = null;
            playerAttack = null;
            playerHealth = null;

            Debug.Log("[Devour] 포식 유물 장착 해제");
        }

        //평타 또는 포식 X 스킬이 적중할 때마다 호출
        private void HandleAttackHit()
        {
            //현재 체력이 최대 체력 이상이면 회복하지 않음
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) return;
            
            //회복
            playerHealth.Heal(healPerHit);
            Debug.Log($"[Devour] 적중 회복: {healPerHit:0.##}");

        }
    }
}