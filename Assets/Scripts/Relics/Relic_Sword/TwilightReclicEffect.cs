using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "RFX_Twilight", menuName = "Relics/Effects/Twilight")]

//황혼 유물 효과
public class TwilightRelicEffect : RelicEffect
{
    [Header("Third Ground Attack")]
    [SerializeField]
    private AttackPattern thirdAttackPattern;

    //유물 장착 시 런타임 생성
    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new TwilightRelicRuntime(p, thirdAttackPattern );
    }

    //유물 장착 시 런타임에서 패턴 추가
    private sealed class TwilightRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly AttackPattern thirdAttackPattern;

        private PlayerAttack playerAttack;

        //실제로 패턴 추가에 성공했는지 기록
        private bool patternAdded;

        public TwilightRelicRuntime(Player player, AttackPattern thirdAttackPattern)
        {
            this.player = player;
            this.thirdAttackPattern = thirdAttackPattern;
        }

        public void Equip()
        {
            if (player == null) return;

            playerAttack = player.GetComponent<PlayerAttack>();

            if (playerAttack == null) return;
            if (thirdAttackPattern == null) return;
            
            patternAdded = playerAttack.AddGroundAttackPattern(thirdAttackPattern);

            if (patternAdded)
            {
                Debug.Log("[Twilight] 황혼 장착");
            }
            else
            {
                Debug.LogWarning("[Twilight] 3타 패턴을 추가하지 못했습니다.");
            }

        }

        public void Unequip()
        {
            if (playerAttack != null && patternAdded)
            {
                playerAttack.RemoveGroundAttackPattern(thirdAttackPattern);

                Debug.Log("[Twilight] 황혼 해제: 기본공격 콤보가 원래대로 돌아왔습니다.");

            }

            patternAdded = false;
            playerAttack = null;
        }

    }
}