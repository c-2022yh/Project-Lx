using UnityEngine;

/// <summary>
/// 각성 연출 색 한 곳에서 정하기.
///   보주 유물을 끼고 있고 그 유물에 각성 색이 정해져 있으면 → 그 유물의 오라 색 (보주마다 다름)
///   아니면 → 기본 파랑 (화면 가장자리 색)
/// 발밑 여우불·실루엣 아우라·화면 가장자리·베기 잔상이 모두 이 색을 쓴다. 밝기는 1로 맞춘다.
/// </summary>
public static class AwakeningColors
{
    public static readonly Color Default = new Color(0.2f, 0.5f, 1f, 1f);

    private static PlayerRelicManager cached;

    public static Color Main(Component from = null)
    {
        PlayerRelicManager rm = from != null ? from.GetComponentInParent<PlayerRelicManager>() : null;
        if (rm == null)
        {
            if (cached == null)
            {
                Player p = Object.FindAnyObjectByType<Player>();
                if (p != null) cached = p.GetComponent<PlayerRelicManager>();
            }
            rm = cached;
        }
        RelicData relic = rm != null ? rm.EquippedOrbRelic : null;
        if (relic == null || !relic.HasAwakeningColors) return Default;
        Color c = relic.AwakeningAuraColor;
        float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        return m <= 0.0001f ? Default : new Color(c.r / m, c.g / m, c.b / m, 1f);
    }
}
