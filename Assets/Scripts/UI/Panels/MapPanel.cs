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
    [Tooltip("제단·유물 상자·포털·부술 수 있는 벽을 자동으로 찾아 마커로 찍는다.")]
    [SerializeField] private bool autoCollectMarkers = true;

    [Tooltip("살아 있는 몬스터도 찍는다. 끄면 지형과 목표물만 보인다.")]
    [SerializeField] private bool showEnemies = true;

    [Tooltip("맵 이미지가 없을 때, 찍을 것들이 판 안에 다 들어오도록 자동으로 맞춘다.")]
    [SerializeField] private bool autoFitWhenNoImage = true;

    /// <summary>한 번 만든 마커를 들고 있다가 닫힐 때 치운다.</summary>
    private readonly List<GameObject> spawnedMarkers = new List<GameObject>();

    private Transform player;

    /// <summary>자동 맞춤으로 정한 중심과 배율. 배율이 0이면 자동 맞춤을 안 쓴다는 뜻.</summary>
    private Vector2 fitCenter;
    private float fitScale;

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
        // 자동 맞춤이 켜져 있으면 인스펙터 값 대신 그때 계산한 값을 쓴다.
        if (fitScale > 0f) return (worldPosition - fitCenter) * fitScale;

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

        UpdateAutoFit(markers);

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

        // 부술 수 있는 벽은 "지금은 못 가는 길"이라 메트로베니아에서 제일 중요한 표시다.
        foreach (FireBreakableWall wall in FindObjectsByType<FireBreakableWall>(FindObjectsSortMode.None))
            AddAuto(result, handled, wall, MapMarkerKind.BreakableWall);

        if (!showEnemies) return;

        // FindObjectsByType은 꺼져 있는 오브젝트를 빼므로 풀에 들어간 몬스터는 안 잡힌다.
        // 죽는 중이라 아직 살아 있는 것만 걸러낸다.
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.IsDead) continue;

            bool isElite = enemy.GetComponent<EnemyEliteAI>() != null;

            AddAuto(result, handled, enemy,
                    isElite ? MapMarkerKind.EliteEnemy : MapMarkerKind.Enemy);
        }
    }

    /// <summary>
    /// 맵 이미지가 없을 때 쓰는 자동 맞춤.
    ///
    /// 이미지가 없으면 World Center / Units To Pixels는 기본값(0,0 / 4)이라,
    /// 스테이지가 원점에서 멀면 마커가 전부 판 밖으로 나가 잘려버린다.
    /// (뷰포트에 RectMask2D가 걸려 있어서 밖으로 나간 건 아예 안 보인다.)
    /// 그래서 이미지가 없는 동안만, 찍을 것들이 다 들어오게 중심과 배율을 정한다.
    /// 이미지를 넣으면 이 계산은 꺼지고 인스펙터 값이 그대로 쓰인다.
    /// </summary>
    private void UpdateAutoFit(List<MarkerInfo> markers)
    {
        fitScale = 0f;

        if (!autoFitWhenNoImage) return;
        if (mapImage != null && mapImage.sprite != null) return;
        if (mapContent == null) return;

        bool any = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;

        foreach (MarkerInfo info in markers) Expand(info.Position, ref any, ref min, ref max);

        if (player != null) Expand(player.position, ref any, ref min, ref max);

        if (!any) return;

        fitCenter = (min + max) * 0.5f;

        Rect board = mapContent.rect;

        // 창이 막 켜진 프레임에는 rect가 아직 0일 수 있다. 그럴 땐 대략치로 계산한다.
        float boardWidth = board.width > 1f ? board.width : 1400f;
        float boardHeight = board.height > 1f ? board.height : 660f;

        float width = Mathf.Max(max.x - min.x, 1f);
        float height = Mathf.Max(max.y - min.y, 1f);

        // 85%만 쓴다. 가장자리에 붙은 마커가 판 끝에 걸려 잘리지 않도록.
        float scale = Mathf.Min(boardWidth * 0.85f / width, boardHeight * 0.85f / height);

        // 찍을 게 하나뿐이면 배율이 터무니없이 커진다. 상식적인 범위로 묶는다.
        fitScale = Mathf.Clamp(scale, 0.5f, 24f);
    }

    private static void Expand(Vector2 point, ref bool any, ref Vector2 min, ref Vector2 max)
    {
        if (!any)
        {
            min = point;
            max = point;
            any = true;
            return;
        }

        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
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
                "(판 안에 다 들어오도록 자동으로 맞춰서 보여주는 중입니다)";
        }
    }

    private static Transform FindPlayer()
    {
        Player found = FindAnyObjectByType<Player>();
        return found != null ? found.transform : null;
    }
}
