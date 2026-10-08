using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "RFX_ResidualMoon", menuName = "Relics/Effects/Residual Moon")]

public class ResidualMoonRelicEffect : RelicEffect
{
    [Header("Granted Skill")]
    [Tooltip("잔월 장착 시 X 슬롯에 지급할 대시 스킬")]
    [SerializeField]
    private SkillData dashSkill;

    //플레이어별 유물 효과 Runtime 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new ResidualMoonRelicRuntime(p, dashSkill);
    }

    private sealed class ResidualMoonRelicRuntime : IRelicRuntime
    {
        private const int XSkillSlot = 0;

        private readonly Player player;
        private readonly SkillData dashSkill;

        private PlayerSkill playerSkill;

        private PlayerKillTracker playerKillTracker;
        private Coroutine resetCooldownCoroutine;

        private bool isEquipped;

        //잔월 효과에 필요한 플레이어와 스킬 보관
        public ResidualMoonRelicRuntime(Player player, SkillData dashSkill)
        {
            this.player = player;
            this.dashSkill = dashSkill;
        }

        //스킬을 지급하고 유물 고유 효과의 이벤트 등록
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) throw new System.InvalidOperationException("[ResidualMoon] Player가 없습니다.");

            playerSkill = player.GetComponent<PlayerSkill>();
            playerKillTracker = player.GetComponent<PlayerKillTracker>();

            if (playerSkill == null || playerKillTracker == null || dashSkill == null)
                throw new System.InvalidOperationException("[ResidualMoon] 스킬 또는 필수 컴포넌트가 없습니다.");

            if (!playerSkill.SetExclusiveSkill(dashSkill, this))
                throw new System.InvalidOperationException("[ResidualMoon] X 스킬을 지급하지 못했습니다.");

            isEquipped = true;

            playerKillTracker.OnEnemyKilled += HandleEnemyKilled;

            Debug.Log("[ResidualMoon] 잔월 장착 완료");
        }

        //유물 이벤트와 예약 작업을 정리하고 자신이 지급한 스킬 회수
        public void Unequip()
        {
            isEquipped = false;

            if (playerKillTracker != null) playerKillTracker.OnEnemyKilled -= HandleEnemyKilled;
            if (resetCooldownCoroutine != null && player != null)
            {
                player.StopCoroutine(resetCooldownCoroutine);
                resetCooldownCoroutine = null;
            }

            if (playerSkill != null)
            {
                playerSkill.ClearExclusiveSkill(this);
            }

            playerSkill = null;
        }

        //적 처치 시 스킬 종료 후 쿨타임 초기화를 예약
        private void HandleEnemyKilled(GameObject killedEnemy)
        {
            if (!isEquipped) return;
            if (playerSkill == null) return;

            if (resetCooldownCoroutine == null)
                resetCooldownCoroutine = player.StartCoroutine(ResetDashCooldownRoutine());
        }

        //스킬 종료와 쿨타임 설정을 기다린 뒤 잔월 X의 쿨타임 초기화
        private IEnumerator ResetDashCooldownRoutine()
        {
            while (player != null && player.ActionState.isSkillActive)
            {
                yield return null;
            }

            yield return null;

            if (!isEquipped || playerSkill == null)
            {
                yield break;
            }

            if (playerSkill.ExclusiveSkill == dashSkill) playerSkill.ResetCooldown(XSkillSlot);
            resetCooldownCoroutine = null;

            Debug.Log("[ResidualMoon] 적 처치: 대시 스킬 쿨타임 초기화");

        }
    }

}
