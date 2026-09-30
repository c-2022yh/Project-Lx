using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 위에 찍히는 마커 한 개.
///
/// 아이콘 스프라이트는 MapUIBuilder가 종류 순서대로 만들어 kindIcons에 넣어둔다.
/// 스프라이트가 없으면 예전처럼 색칠한 사각형으로 떨어진다.
/// </summary>
public class MapMarkerView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI labelText;

    [Tooltip("MapMarkerKind 번호 순서대로 넣는다. 빌더가 채운다.")]
    [SerializeField] private Sprite[] kindIcons;

    private static readonly Color ShrineColor = new Color(0.45f, 0.85f, 1.00f, 1f);
    private static readonly Color ChestColor = new Color(1.00f, 0.82f, 0.35f, 1f);
    private static readonly Color BossColor = new Color(1.00f, 0.38f, 0.38f, 1f);
    private static readonly Color ExitColor = new Color(0.65f, 0.90f, 0.55f, 1f);
    private static readonly Color CustomColor = new Color(0.85f, 0.85f, 0.90f, 1f);
    private static readonly Color EnemyColor = new Color(0.86f, 0.55f, 0.45f, 1f);
    private static readonly Color EliteColor = new Color(0.98f, 0.62f, 0.25f, 1f);
    private static readonly Color WallColor = new Color(0.72f, 0.66f, 0.55f, 1f);

    public void Bind(MapMarkerKind kind, string label)
    {
        if (icon != null)
        {
            icon.sprite = IconFor(kind);
            icon.color = ColorOf(kind);
            icon.preserveAspect = true;

            // 종류마다 눈에 띄어야 하는 정도가 다르다. 보스는 크게, 잡몹은 작게.
            float size = SizeOf(kind);
            icon.rectTransform.sizeDelta = new Vector2(size, size);
        }

        if (labelText == null) return;

        bool hasLabel = !string.IsNullOrEmpty(label);
        labelText.enabled = hasLabel;

        if (hasLabel) labelText.text = label;
    }

    private Sprite IconFor(MapMarkerKind kind)
    {
        if (kindIcons == null) return null;

        int index = (int)kind;

        if (index < 0 || index >= kindIcons.Length) return null;

        return kindIcons[index];
    }

    public static Color ColorOf(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Shrine => ShrineColor,
            MapMarkerKind.RelicChest => ChestColor,
            MapMarkerKind.Boss => BossColor,
            MapMarkerKind.Exit => ExitColor,
            MapMarkerKind.Enemy => EnemyColor,
            MapMarkerKind.EliteEnemy => EliteColor,
            MapMarkerKind.BreakableWall => WallColor,
            _ => CustomColor
        };
    }

    /// <summary>마커 한 변의 길이(픽셀).</summary>
    public static float SizeOf(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Boss => 30f,
            MapMarkerKind.Shrine => 22f,
            MapMarkerKind.RelicChest => 22f,
            MapMarkerKind.Exit => 22f,
            MapMarkerKind.BreakableWall => 20f,
            MapMarkerKind.EliteEnemy => 18f,
            MapMarkerKind.Enemy => 13f,
            _ => 16f
        };
    }

    /// <summary>범례에 올릴 순서. 중요한 것부터.</summary>
    public static readonly MapMarkerKind[] LegendOrder =
    {
        MapMarkerKind.Boss,
        MapMarkerKind.Shrine,
        MapMarkerKind.RelicChest,
        MapMarkerKind.Exit,
        MapMarkerKind.BreakableWall,
        MapMarkerKind.EliteEnemy,
        MapMarkerKind.Enemy
    };

    /// <summary>범례에 쓸 이름.</summary>
    public static string NameOf(MapMarkerKind kind)
    {
        return kind switch
        {
            MapMarkerKind.Shrine => "제단",
            MapMarkerKind.RelicChest => "유물 상자",
            MapMarkerKind.Boss => "보스",
            MapMarkerKind.Exit => "출구",
            MapMarkerKind.Enemy => "몬스터",
            MapMarkerKind.EliteEnemy => "정예",
            MapMarkerKind.BreakableWall => "부술 수 있는 벽",
            _ => "기타"
        };
    }
}
