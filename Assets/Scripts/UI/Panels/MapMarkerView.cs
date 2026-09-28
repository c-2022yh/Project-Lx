using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 위에 찍히는 마커 한 개.
/// 아트가 나오기 전이라 색과 크기로만 구분한다.
/// </summary>
public class MapMarkerView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI labelText;

    private static readonly Color ShrineColor = new Color(0.45f, 0.85f, 1.00f, 1f);
    private static readonly Color ChestColor = new Color(1.00f, 0.82f, 0.35f, 1f);
    private static readonly Color BossColor = new Color(1.00f, 0.38f, 0.38f, 1f);
    private static readonly Color ExitColor = new Color(0.65f, 0.90f, 0.55f, 1f);
    private static readonly Color CustomColor = new Color(0.85f, 0.85f, 0.90f, 1f);

    public void Bind(MapMarkerKind kind, string label)
    {
        if (icon != null) icon.color = ColorOf(kind);

        if (labelText == null) return;

        bool hasLabel = !string.IsNullOrEmpty(label);
        labelText.enabled = hasLabel;

        if (hasLabel) labelText.text = label;
    }

    public static Color ColorOf(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Shrine => ShrineColor,
            MapMarkerKind.RelicChest => ChestColor,
            MapMarkerKind.Boss => BossColor,
            MapMarkerKind.Exit => ExitColor,
            _ => CustomColor
        };
    }

    /// <summary>범례에 쓸 이름.</summary>
    public static string NameOf(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Shrine => "제단",
            MapMarkerKind.RelicChest => "유물 상자",
            MapMarkerKind.Boss => "보스",
            MapMarkerKind.Exit => "출구",
            _ => "기타"
        };
    }
}
