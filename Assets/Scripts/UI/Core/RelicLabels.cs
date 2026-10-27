using System.Text;
using UnityEngine;

/// <summary>
/// 유물을 화면에 보여줄 때 쓰는 이름·색·코스트 표시를 한곳에 모았다.
/// (10.17 회의) 계열 이름은 "검의 힘 / 보주의 힘 / 유물"로 통일하고, 코스트는 ◆ 개수로 보여준다.
/// 색은 유물창 칸 테두리(RelicSlotView)와 같은 색이다.
/// </summary>
public static class RelicLabels
{
    public const char CostFilled = '◆';
    public const char CostEmpty = '◇';

    public static readonly Color SwordColor = new Color(0.90f, 0.42f, 0.38f, 1f);
    public static readonly Color OrbColor = new Color(0.62f, 0.52f, 0.95f, 1f);
    public static readonly Color BodyColor = new Color(0.38f, 0.82f, 0.70f, 1f);

    /// <summary>빈 ◇ 색. 채운 ◆보다 확실히 흐리게.</summary>
    private const string EmptyHex = "FFFFFF55";

    public static string NameOf(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => "검의 힘",
            RelicCategory.Orb => "보주의 힘",
            _ => "유물"
        };
    }

    public static Color ColorOf(RelicCategory category)
    {
        return category switch
        {
            RelicCategory.Sword => SwordColor,
            RelicCategory.Orb => OrbColor,
            _ => BodyColor
        };
    }

    /// <summary>코스트 2 → "◆◆" (계열 색).</summary>
    public static string CostDiamonds(int cost, RelicCategory category = RelicCategory.Body)
    {
        if (cost <= 0) return "";
        return Colored(new string(CostFilled, cost), ColorOf(category));
    }

    /// <summary>쓴 코스트 2, 최대 5 → "◆◆◇◇◇".</summary>
    public static string CostGauge(int used, int max)
    {
        used = Mathf.Clamp(used, 0, max);
        var sb = new StringBuilder();
        if (used > 0) sb.Append(Colored(new string(CostFilled, used), BodyColor));
        if (max - used > 0) sb.Append($"<color=#{EmptyHex}>").Append(CostEmpty, max - used).Append("</color>");
        return sb.ToString();
    }

    /// <summary>툴팁·설명칸 두 번째 줄. 검/보주는 이름만, 유물은 "유물 · ◆◆".</summary>
    public static string MetaLine(RelicData relic)
    {
        if (relic == null) return "";

        string name = Colored(NameOf(relic.Category), ColorOf(relic.Category));
        if (relic.Category != RelicCategory.Body || relic.Cost <= 0) return name;

        return $"{name}   ·   {CostDiamonds(relic.Cost)}";
    }

    private static string Colored(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
