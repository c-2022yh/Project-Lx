using UnityEngine;

/// <summary>맵에 찍히는 표시의 종류.</summary>
public enum MapMarkerKind
{
    /// <summary>세이브 지점(제단).</summary>
    Shrine,

    /// <summary>유물 상자.</summary>
    RelicChest,

    /// <summary>보스 방.</summary>
    Boss,

    /// <summary>다른 구역으로 가는 출구.</summary>
    Exit,

    /// <summary>그 밖에 직접 찍고 싶은 것.</summary>
    Custom
}

/// <summary>
/// 맵에 표시하고 싶은 월드 오브젝트에 붙인다.
///
/// 제단·유물 상자·포털은 MapPanel이 알아서 찾으므로 붙이지 않아도 된다.
/// 보스방처럼 전용 스크립트가 없는 것이나,
/// 자동으로 잡힌 것에 이름을 달아주고 싶을 때만 붙이면 된다.
/// 같은 오브젝트에 이게 붙어 있으면 자동 수집 쪽은 건너뛴다.
/// </summary>
public class MapMarkerSource : MonoBehaviour
{
    [SerializeField] private MapMarkerKind kind = MapMarkerKind.Custom;

    [Tooltip("맵에서 마커 옆에 띄울 짧은 이름. 비워두면 아무것도 안 나온다.")]
    [SerializeField] private string label = "";

    [Tooltip("표시할 위치. 비워두면 이 오브젝트 위치를 쓴다.")]
    [SerializeField] private Transform pointOverride;

    public MapMarkerKind Kind => kind;

    public string Label => label;

    public Vector2 WorldPosition =>
        pointOverride != null ? (Vector2)pointOverride.position : (Vector2)transform.position;
}
