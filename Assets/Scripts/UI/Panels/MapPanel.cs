using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭을 누르고 있는 동안 보이는 반투명 지도.
///
/// 방 단위 격자를 이어붙이는 방식이 아니라, 한 장짜리 맵 이미지 위에
/// 플레이어 위치와 마커만 찍는다. 그래서 맵 담당이 채울 값이 두 개뿐이다.
///   worldCenter   - 맵 이미지 한가운데가 월드의 어느 지점인지
///   unitsToPixels - 월드 1유닛을 맵에서 몇 픽셀로 그릴지
///                   (Grid의 cellSize가 1이라 타일 한 칸 = 월드 1유닛이다)
///
/// 맵 이미지가 아직 없어도 동작한다. 그때는 빈 판 위에 마커만 찍히므로
/// 이미지가 나오기 전에 좌표 변환이 맞는지 먼저 확인할 수 있다.
///
/// 마커는 씬을 뒤져서 자동으로 모은다. 맵 담당이 제단이나 상자를 놓기만 하면
/// 맵에 따라오므로, 맵을 위해 따로 해줄 작업이 없다.
/// </summary>
public class MapPanel : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("마커와 플레이어 점이 붙는 기준. 피벗과 앵커가 가운데여야 한다.")]
    [SerializeField] private RectTransform mapContent;

    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform playerDot;
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private GameObject markerPrefab;

    [Header("좌표 기준")]
    [Tooltip("맵 이미지 한가운데가 월드의 어느 지점인지.")]
    [SerializeField] private Vector2 worldCenter = Vector2.zero;

    [Tooltip("월드 1유닛을 맵에서 몇 픽셀로 그릴지. 타일 한 칸이 월드 1유닛이다.")]
    [SerializeField] private float unitsToPixels = 4f;

    [Tooltip("맵 이미지가 화면에 줄어들어 그려지는 만큼 마커 위치도 같이 줄인다.\n" +
             "이미지를 창 크기에 맞춰 넣어도 좌표가 어긋나지 않게 해준다.")]
    [SerializeField] private bool matchImageScale = true;

    [Header("동작")]
    [Tooltip("제단·유물 상자·포털을 자동으로 찾아 마커로 찍는다.")]
    [SerializeField] private bool autoCollectMarkers = true;

    /// <summary>한 번 만든 마커를 들고 있다가 닫힐 때 치운다.</summary>
    private readonly List<GameObject> spawnedMarkers = new List<GameObject>();

    private Transform player;

    /// <summary>수집 결과를 잠깐 담는 상자. 이름까지 들고 있어야 라벨을 찍을 수 있다.</summary>
    private struct MarkerInfo
    {
        public MapMarkerKind Kind;
        public string Label;
        public Vector2 Position;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible) return;

        player = FindPlayer();
        Rebuild();
    }

    private void OnDisable()
    {
        ClearMarkers();
    }

    private void Update()
    {
        if (playerDot == null) return;

        // 씬을 넘어오면 플레이어가 바뀌므로 없으면 다시 찾는다.
        if (player == null) player = FindPlayer();

        if (player == null)
        {
            playerDot.gameObject.SetActive(false);
            return;
        }

        playerDot.gameObject.SetActive(true);
        playerDot.anchoredPosition = WorldToMap(player.position);
    }

    /// <summary>월드 좌표를 맵 위 좌표로. 이 한 줄이 맵의 전부다.</summary>
    public Vector2 WorldToMap(Vector2 worldPosition)
    {
        return (worldPosition - worldCenter) * unitsToPixels * ImageDisplayScale;
    }

    /// <summary>
    /// 맵 이미지가 원본보다 얼마나 줄어들어 그려지고 있는지.
    ///
    /// unitsToPixels는 "원본 이미지에서 월드 1유닛이 몇 픽셀인지"를 뜻한다.
    /// 그런데 이미지는 창에 맞춰 줄어들어 그려지므로, 마커도 같은 비율로 줄여야
    /// 이미지 위의 제자리에 찍힌다. 이미지가 없으면 1이라 값이 그대로 쓰인다.
    /// </summary>
    private float ImageDisplayScale
    {
        get
        {
            if (!matchImageScale) return 1f;
            if (mapImage == null || mapImage.sprite == null) return 1f;

            Rect area = mapImage.rectTransform.rect;
            float textureWidth = mapImage.sprite.rect.width;
            float textureHeight = mapImage.sprite.rect.height;

            if (textureWidth <= 0f || textureHeight <= 0f) return 1f;

            // preserveAspect라 가로·세로 중 더 빡빡한 쪽에 맞춰 줄어든다.
            return Mathf.Min(area.width / textureWidth, area.height / textureHeight);
        }
    }

    private void Rebuild()
    {
        ClearMarkers();
        UpdateNotice();

        if (mapContent == null || markerPrefab == null) return;

        List<MarkerInfo> markers = new List<MarkerInfo>();
        Collect(markers);

        foreach (MarkerInfo info in markers) SpawnMarker(info);

        // 마커를 나중에 만들면 플레이어 점 위에 덮인다. 점을 맨 위로 올린다.
        if (playerDot != null) playerDot.SetAsLastSibling();
    }

    private void Collect(List<MarkerInfo> result)
    {
        HashSet<GameObject> handled = new HashSet<GameObject>();

        // 직접 붙인 것이 우선이다. 자동 수집과 겹치면 이쪽만 남는다.
        foreach (MapMarkerSource source in
                 FindObjectsByType<MapMarkerSource>(FindObjectsSortMode.None))
        {
            if (source == null) continue;

            handled.Add(source.gameObject);

            result.Add(new MarkerInfo
            {
                Kind = source.Kind,
                Label = source.Label,
                Position = source.WorldPosition
            });
        }

        if (!autoCollectMarkers) return;

        foreach (Shrine shrine in FindObjectsByType<Shrine>(FindObjectsSortMode.None))
            AddAuto(result, handled, shrine, MapMarkerKind.Shrine);

        foreach (RelicChest chest in FindObjectsByType<RelicChest>(FindObjectsSortMode.None))
            AddAuto(result, handled, chest, MapMarkerKind.RelicChest);

        foreach (ScenePortal portal in FindObjectsByType<ScenePortal>(FindObjectsSortMode.None))
            AddAuto(result, handled, portal, MapMarkerKind.Exit);

        foreach (AutoScenePortal portal in FindObjectsByType<AutoScenePortal>(FindObjectsSortMode.None))
            AddAuto(result, handled, portal, MapMarkerKind.Exit);
    }

    private static void AddAuto(List<MarkerInfo> result, HashSet<GameObject> handled,
                                Component component, MapMarkerKind kind)
    {
        if (component == null) return;
        if (!handled.Add(component.gameObject)) return;

        result.Add(new MarkerInfo
        {
            Kind = kind,
            Label = "",
            Position = component.transform.position
        });
    }

    private void SpawnMarker(MarkerInfo info)
    {
        GameObject go = Instantiate(markerPrefab, mapContent, false);
        go.SetActive(true);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = WorldToMap(info.Position);

        MapMarkerView view = go.GetComponent<MapMarkerView>();
        if (view != null) view.Bind(info.Kind, info.Label);

        spawnedMarkers.Add(go);
    }

    private void ClearMarkers()
    {
        foreach (GameObject go in spawnedMarkers)
        {
            if (go != null) Destroy(go);
        }

        spawnedMarkers.Clear();
    }

    private void UpdateNotice()
    {
        bool hasImage = mapImage != null && mapImage.sprite != null;

        // 스프라이트가 없는 Image는 흰 사각형으로 그려진다. 그럴 땐 아예 끈다.
        if (mapImage != null) mapImage.enabled = hasImage;

        if (noticeText == null) return;

        noticeText.enabled = !hasImage;

        if (!hasImage)
        {
            noticeText.text =
                "맵 이미지가 아직 없습니다. 마커와 현재 위치만 표시합니다.\n" +
                "(MapPanel의 World Center / Units To Pixels 값으로 위치를 맞춥니다)";
        }
    }

    private static Transform FindPlayer()
    {
        Player found = FindAnyObjectByType<Player>();
        return found != null ? found.transform : null;
    }
}
