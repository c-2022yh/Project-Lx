using UnityEngine;

[CreateAssetMenu(fileName = "RFX_Berserk", menuName = "Relics/Effects/Berserk")]

//폭주
//각성 한 번에 초토화를 한 번 사용
public class BerserkRelicEffect : RelicEffect
{
    [Header("Awakening Skill")]
    [SerializeField, Tooltip("각성 중 Q에 연결할 초토화 SkillData SO")]
    private ScorchedEarthSkillData scorchedEarth;

    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new BerserkRuntime(player, scorchedEarth);
    }

    private sealed class BerserkRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly ScorchedEarthSkillData skillData;
        private PlayerAwakening awakening;
        private PlayerSkill skills;
        private bool equipped;

        public BerserkRuntime(Player player, ScorchedEarthSkillData skillData)
        {
            this.player = player;
            this.skillData = skillData;
        }

        //각성 시작 이벤트에 구독하고 별도의 Q 전용 스킬을 등록
        public void Equip()
        {
            if (equipped || player == null || skillData == null) return;

            awakening = player.Awakening;
            skills = player.Skill;

            if (awakening == null || skills == null) return;
            if (!skills.RegisterAwakeningSkill(skillData)) return;

            equipped = true;

            awakening.OnAwakeningStarted += HandleStart;
            awakening.OnAwakeningEnded += HandleEnd;

            if (awakening.IsAwakened) HandleStart();
        }

        //이벤트를 해제하고 진행 중인 Q 스킬까지 취소
        public void Unequip()
        {
            if (!equipped) return;
            if (awakening != null)
            {
                awakening.OnAwakeningStarted -= HandleStart;
                awakening.OnAwakeningEnded -= HandleEnd;
            }

            if (skills != null) skills.UnregisterAwakeningSkill(skillData);

            equipped = false;
            awakening = null;
            skills = null;
        }

        //새 각성을 시작할 때 한 번의 Q 사용권 지급
        private void HandleStart()
        {
            if (equipped && skills != null) skills.SetAwakeningSkillAvailable(true);
        }

        //각성이 종료되면 사용하지 않은 Q 사용권 박탈
        private void HandleEnd()
        {
            if (skills != null) skills.SetAwakeningSkillAvailable(false);
        }
    }
}
