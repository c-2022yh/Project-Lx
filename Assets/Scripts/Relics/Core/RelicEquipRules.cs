using System.Collections.Generic;

/// <summary>
/// 유물을 장착할 수 있는지 판정한다.
///
/// 규칙:
///   검   — 1개 (무기라 하나만)
///   보주 — 1개
///   신체 — 개수 제한 없음. 대신 코스트 예산 안에서만.
///
/// 코스트는 신체 유물에만 적용한다. 검/보주는 칸 자체가 하나뿐이라
/// 예산으로 또 막으면 이중 제한이 된다.
/// </summary>
public static class RelicEquipRules
{
    public const int SwordSlots = 1;
    public const int OrbSlots = 1;

    /// <summary>신체 유물에 쓸 수 있는 총 코스트.</summary>
    public const int BodyCostBudget = 5;

    public static int SlotLimitOf(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => SwordSlots,
            RelicCategory.Orb => OrbSlots,
            _ => int.MaxValue
        };
    }

    public static int CountOf(IEnumerable<RelicData> equipped, RelicCategory category)
    {
        int count = 0;

        foreach (RelicData relic in equipped)
            if (relic != null && relic.Category == category) count++;

        return count;
    }

    /// <summary>현재 장착 중인 신체 유물이 쓰고 있는 코스트 합.</summary>
    public static int UsedBodyCost(IEnumerable<RelicData> equipped)
    {
        int sum = 0;

        foreach (RelicData relic in equipped)
            if (relic != null && relic.Category == RelicCategory.Body) sum += relic.Cost;

        return sum;
    }

    public static int RemainingBodyCost(IEnumerable<RelicData> equipped)
    {
        return BodyCostBudget - UsedBodyCost(equipped);
    }

    /// <summary>
    /// 장착 가능 여부. 막히면 reason에 사람이 읽을 이유가 담긴다.
    /// 검/보주는 칸이 차 있어도 "교체"로 처리할 수 있어서 replaces에 밀려날 유물을 돌려준다.
    /// </summary>
    public static bool CanEquip(RelicData relic, IReadOnlyList<RelicData> equipped,
                                out string reason, out RelicData replaces)
    {
        reason = null;
        replaces = null;

        if (relic == null)
        {
            reason = "유물이 없습니다";
            return false;
        }

        foreach (RelicData e in equipped)
        {
            if (e != relic) continue;

            reason = "이미 장착 중입니다";
            return false;
        }

        if (relic.Category == RelicCategory.Body)
        {
            int remaining = RemainingBodyCost(equipped);

            if (relic.Cost > remaining)
            {
                reason = $"코스트가 부족합니다 (남은 코스트 {remaining}, 필요 {relic.Cost})";
                return false;
            }

            return true;
        }

        // 검 / 보주: 칸이 하나뿐이므로 차 있으면 교체 대상을 알려준다.
        int limit = SlotLimitOf(relic.Category);
        List<RelicData> sameCategory = new List<RelicData>();

        foreach (RelicData e in equipped)
            if (e != null && e.Category == relic.Category) sameCategory.Add(e);

        if (sameCategory.Count < limit) return true;

        replaces = sameCategory[0];
        return true;
    }

    public static string LabelOf(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => "검",
            RelicCategory.Orb => "보주",
            _ => "신체"
        };
    }
}
