using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "RFX_ResidualMoon", menuName = "Relics/Effects/Residual Moon")]

//잔월 유물 효과
public class ResidualMoonRelicEffect : RelicEffect
{
    [Header("Granted Skill")]
    [Tooltip("잔월 장착 시 X 슬롯에 지급할 대시 스킬")]
    [SerializeField]
    private SkillData dashSkill;


    //유물 장착 시 런타임 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new ResidualMoonRelicRuntime(p, dashSkill);
    }

    //유물 런타임 클래스
    private sealed class ResidualMoonRelicRuntime : IRelicRuntime
    {
        private const int XSkillSlot = 0;

        private readonly Player player;
        private readonly SkillData dashSkill;

        private PlayerSkill playerSkill;

        //적 처치 감지용 트래커
        private PlayerKillTracker playerKillTracker;
        private Coroutine resetCooldownCoroutine;

        private bool isEquipped;

        public ResidualMoonRelicRuntime(Player player, SkillData dashSkill)
        {
            this.player = player;
            this.dashSkill = dashSkill;
        }

        public void Equip()
        {
            if (player == null) return;

            playerSkill = player.GetComponent<PlayerSkill>();
            playerKillTracker = player.GetComponent<PlayerKillTracker>();

            bool equipped = playerSkill.EquipSkill(XSkillSlot, dashSkill);

            if (!equipped) return;
            
            isEquipped = true;

            // 모든 적 처치 감지
            playerKillTracker.OnEnemyKilled += HandleEnemyKilled;

            Debug.Log("[ResidualMoon] 잔월 장착 완료");
        }

        public void Unequip()
        {
            isEquipped = false;

            playerKillTracker.OnEnemyKilled -= HandleEnemyKilled;

            if (playerSkill != null)
            {
                playerSkill.UnequipSkill(XSkillSlot, dashSkill);
            }

            playerSkill = null;
        }

        private void HandleEnemyKilled(GameObject killedEnemy)
        {
            if (!isEquipped) return;
            if (playerSkill == null) return;

            //스킬 종료 후 쿨타임이 설정된 다음 초기화
            player.StartCoroutine(ResetDashCooldownRoutine());
        }

        private IEnumerator ResetDashCooldownRoutine()
        {
            // 현재 대시 스킬 실행이 끝날 때까지 대기
            while (player != null && player.ActionState.isSkillActive)
            {
                yield return null;
            }

            //한 프레임 늦게 초기화, 그래야 정상적으로 쿨타임이 초기화됨
            yield return null;

            if (!isEquipped || playerSkill == null)
            {
                yield break;
            }

            playerSkill.ResetCooldown(XSkillSlot);

            Debug.Log("[ResidualMoon] 적 처치: 대시 스킬 쿨타임 초기화");

        }
    }

}