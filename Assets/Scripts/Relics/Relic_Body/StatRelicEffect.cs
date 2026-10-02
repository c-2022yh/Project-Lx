using System;
using UnityEngine;

public enum RelicStatType
{
    PhysicalAttack,
    MagicalAttack,
    PhysicalPenetration,
    MagicalPenetration,
    CriticalChance,
    CriticalMultiplier,
    DamageAmplification,
    PhysicalDefense,
    MagicalDefense,
    Durability
}

[CreateAssetMenu(
    fileName = "RFX_StatRelic",
    menuName = "Relics/Effects/Stat Relic"
)]

//플레이어의 전투 스탯을 일정 수치만큼 증가시키는 유물 효과
public class StatRelicEffect : RelicEffect
{
    [Header("Stat Relic Settings")]
    [SerializeField]
    private RelicStatType statType;

    [SerializeField, Min(0f)]
    private float amount = 1f;

    //플레이어별 스탯 유물 Runtime 생성
    public override IRelicRuntime CreateRuntime(Player player)
    {
        return new StatRelicRuntime(player, statType, amount);
    }

    private sealed class StatRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly RelicStatType statType;
        private readonly float amount;

        private PlayerStats playerStats;
        private bool isEquipped;

        //스탯 효과에 필요한 플레이어와 증가 수치 보관
        public StatRelicRuntime(Player player, RelicStatType statType, float amount)
        {
            this.player = player;
            this.statType = statType;
            this.amount = amount;
        }

        //선택한 스탯 증가
        public void Equip()
        {
            if (isEquipped) return;
            if (player == null) throw new InvalidOperationException("[StatRelic] Player가 없습니다.");

            playerStats = player.Stats != null
                ? player.Stats
                : player.GetComponent<PlayerStats>();

            if (playerStats == null)
                throw new InvalidOperationException("[StatRelic] PlayerStats가 없습니다.");

            ApplyStat(1f);
            isEquipped = true;
        }

        //적용한 스탯 증가분 제거
        public void Unequip()
        {
            if (!isEquipped) return;

            if (playerStats != null)
            {
                ApplyStat(-1f);
            }

            isEquipped = false;
            playerStats = null;
        }

        //선택한 스탯에 증가량 적용
        private void ApplyStat(float sign)
        {
            OffensiveStats offense = playerStats.Offense;
            DefensiveStats defense = playerStats.Defense;
            float value = amount * sign;

            switch (statType)
            {
                case RelicStatType.PhysicalAttack:
                    offense.physicalAttack += value;
                    break;
                case RelicStatType.MagicalAttack:
                    offense.magicalAttack += value;
                    break;
                case RelicStatType.PhysicalPenetration:
                    offense.physicalPenetration += value;
                    break;
                case RelicStatType.MagicalPenetration:
                    offense.magicalPenetration += value;
                    break;
                case RelicStatType.CriticalChance:
                    offense.criticalChance += value;
                    break;
                case RelicStatType.CriticalMultiplier:
                    offense.criticalMultiplier += value;
                    break;
                case RelicStatType.DamageAmplification:
                    offense.damageAmplification += value;
                    break;
                case RelicStatType.PhysicalDefense:
                    defense.physicalDefense += value;
                    break;
                case RelicStatType.MagicalDefense:
                    defense.magicalDefense += value;
                    break;
                case RelicStatType.Durability:
                    defense.durability += value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
