using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Tooltip("열린 씬에 맞는 밑그림을 Resources의 MapSketchLibrary에서 찾아 끼운다.\n" +
             "끄면 인스펙터에 직접 넣은 이미지와 좌표만 쓴다.")]
    [SerializeField] private bool useSceneSketch = true;

    /// <summary>
    /// 아직 걸어가 보지 않은 곳을 덮는 색. 완전히 불투명해서 지형이 전혀 안 보인다.
    /// 마커(상자·제단·출구·적)는 안개 위에 그리므로 안 가본 곳에서도 보인다.
    /// </summary>
    private static readonly Color FogColor = new Color(0.035f, 0.045f, 0.055f, 1f);

    /// <summary>안개를 다시 그리는 간격(초). 지도를 연 채로 걸어도 따라 걷힌다.</summary>
    private const float FogRefreshInterval = 0.25f;

    private RawImage fogImage;
    private Texture2D fogTexture;
    private float nextFogAt;

    /// <summary>한 번 만든 마커를 들고 있다가 닫힐 때 치운다.</summary>
    private readonly List<GameObject> spawnedMarkers = new List<GameObject>();

    private Transform player;

    /// <summary>자동 맞춤으로 정한 중심과 배율. 배율이 0이면 자동 맞춤을 안 쓴다는 뜻.</summary>
    private Vector2 fitCenter;
    private float fitScale;

    // ── 방 지도 (10.29 할로우나이트식) ──
    // Resources/MapRoomAtlas에 방 그림이 있으면 씬 밑그림·안개 대신 방 지도를 그린다.
    // 지금 방과 가 본 방만 이어 그리고, 안 가 본 방은 그리지 않는다.
    private static readonly Color CurrentRoomTint = new Color(0.875f, 0.918f, 0.933f, 1f);
    private static readonly Color VisitedRoomTint = new Color(0.59f, 0.68f, 0.62f, 1f);
    private const float RoomFitMargin = 0.86f;
    private const float MaxRoomScale = 10f;

    private bool atlasMode;
    private RectTransform roomLayer;
    private readonly List<GameObject> roomImages = new List<GameObject>();

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

        ApplySceneSketch();
        atlasMode = ApplyRoomAtlas();

        player = FindPlayer();
        Rebuild();
        RefreshFog();
    }

    /// <summary>
    /// 열린 씬에 맞는 밑그림을 끼운다.
    ///
    /// 맵 창은 Popup_Canvas 프리팹에 하나뿐인데 씬은 여러 개다. 이미지를 프리팹에
    /// 박아두면 어느 씬에 들어가도 같은 지형이 나오므로, 열 때마다 씬 이름으로 찾는다.
    /// 등록 안 된 씬이면 이미지를 비워서 아래 자동 맞춤으로 떨어뜨린다.
    /// </summary>
    private void ApplySceneSketch()
    {
        if (!useSceneSketch) return;

        MapSketchLibrary library = MapSketchLibrary.Load();

        // 저장소가 아예 없으면 인스펙터에 직접 넣어둔 값을 건드리지 않는다.
        if (library == null) return;

        MapSketchLibrary.Entry entry = library.Find(SceneManager.GetActiveScene().name);

        if (entry == null || entry.sketch == null)
        {
            if (mapImage != null) mapImage.sprite = null;
            return;
        }

        if (mapImage != null) mapImage.sprite = entry.sketch;

        worldCenter = entry.worldCenter;
        unitsToPixels = entry.unitsToPixels;
    }

    private void OnDisable()
    {
        ClearMarkers();
    }

    // ── 방 지도 ─────────────────────────────

    /// <summary>방 지도를 그린다. 이 씬의 방 그림이 없으면 false (예전처럼 씬 밑그림을 쓴다).</summary>
    private bool ApplyRoomAtlas()
    {
        ClearRooms();

        MapRoomAtlas atlas = MapRoomAtlas.Load();
        MapRoomAtlas.Room here = atlas != null ? atlas.Find(SceneManager.GetActiveScene().name) : null;
        if (here == null || mapContent == null) return false;

        // 지금 방 + 가 본 방
        List<MapRoomAtlas.Room> shown = new List<MapRoomAtlas.Room>();
        foreach (MapRoomAtlas.Room r in atlas.Rooms)
        {
            if (r == null || r.image == null) continue;
            if (r == here || VisitedScenes.HasVisited(r.sceneName)) shown.Add(r);
        }

        // 10.30: 할로우나이트처럼 전체 지도. 안 가 본 방도 자리는 차지해서(그림은 안 그림)
        // 지도가 늘 같은 크기·위치로 보인다. 포털로 이어진 같은 덩어리 방들만 센다 (테스트 씬 제외).
        Vector2 min = here.atlasMin, max = here.atlasMin + here.worldSize;
        foreach (MapRoomAtlas.Room r in atlas.Rooms)
        {
            if (r == null || r.image == null) continue;
            bool sameWorld = here.group != 0 ? r.group == here.group : shown.Contains(r);
            if (!sameWorld) continue;
            min = Vector2.Min(min, r.atlasMin);
            max = Vector2.Max(max, r.atlasMin + r.worldSize);
        }

        // 전체 지도가 판 안에 다 들어오게
        Vector2 size = max - min;
        Rect board = mapContent.rect;
        float scale = Mathf.Min(board.width * RoomFitMargin / Mathf.Max(1f, size.x),
                                board.height * RoomFitMargin / Mathf.Max(1f, size.y));
        scale = Mathf.Min(scale, MaxRoomScale);
        Vector2 center = (min + max) * 0.5f;

        EnsureRoomLayer();
        GameObject current = null;

        foreach (MapRoomAtlas.Room r in shown)
        {
            GameObject go = new GameObject("Room_" + r.sceneName, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(roomLayer, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = r.worldSize * scale;
            rt.anchoredPosition = (r.atlasMin + r.worldSize * 0.5f - center) * scale;

            Image image = go.GetComponent<Image>();
            image.sprite = r.image;
            image.raycastTarget = false;
            image.color = r == here ? CurrentRoomTint : VisitedRoomTint;
            ApplyReveal(r, image);

            if (r == here) current = go;
            roomImages.Add(go);
        }

        // 지금 방을 맨 위에 (옆 방과 겹친 테두리가 지금 방 색으로 보이게)
        if (current != null) current.transform.SetAsLastSibling();

        // 마커·플레이어 점은 지금 씬의 월드 좌표로 찍으므로, 그걸 전체 지도 좌표로 옮겨 준다.
        //   지도 = (월드 + (atlasMin - worldMin) - center) * scale
        fitCenter = center - (here.atlasMin - here.worldMin);
        fitScale = scale;
        return true;
    }

    private static bool ShowInRoomMap(MapMarkerKind kind) => kind == MapMarkerKind.Shrine;

    // ── 방 지도 안개 (10.30) ─────────────────
    // 들어간 방도 통째로 보이지 않고, 걸어서 밝힌 칸(MapExploration)만 보인다.
    // 방 그림마다 "밝힌 칸" 텍스처를 만들어 UI/MapRoomReveal 셰이더로 그 부분만 보이게 한다.

    private const string RevealMaterialPath = "Map/MapRoomReveal";
    private static readonly int RevealTexId = Shader.PropertyToID("_RevealTex");
    private static readonly int RevealStId = Shader.PropertyToID("_RevealST");

    private class RoomReveal
    {
        public string sceneName;
        public Material material;
        public Texture2D texture;
        public int cellMinX, cellMinY;
    }

    private readonly List<RoomReveal> reveals = new List<RoomReveal>();
    private Material revealBase;
    private bool revealLooked;

    /// <summary>방 그림에 "밝힌 칸만 보이기" 재질을 입힌다. 안개를 꺼 두었거나 재질이 없으면 통째로 보인다.</summary>
    private void ApplyReveal(MapRoomAtlas.Room r, Image image)
    {
        if (!MapExploration.FogEnabled) return;

        if (!revealLooked)
        {
            revealLooked = true;
            revealBase = Resources.Load<Material>(RevealMaterialPath);
        }
        if (revealBase == null) return;

        // 방 그림이 덮는 월드 영역을 칸 단위로. 텍셀 하나 = 칸 하나.
        float cs = MapExploration.CellSize;
        int x0 = Mathf.FloorToInt(r.worldMin.x / cs);
        int y0 = Mathf.FloorToInt(r.worldMin.y / cs);
        int x1 = Mathf.CeilToInt((r.worldMin.x + r.worldSize.x) / cs);
        int y1 = Mathf.CeilToInt((r.worldMin.y + r.worldSize.y) / cs);
        int w = Mathf.Max(1, x1 - x0), h = Mathf.Max(1, y1 - y0);

        Texture2D tex = new Texture2D(w, h, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "MapReveal_" + r.sceneName
        };

        Material mat = new Material(revealBase) { name = "MapReveal_" + r.sceneName };
        mat.SetTexture(RevealTexId, tex);
        // 방 그림 UV(0~1) -> 칸 텍스처 UV
        mat.SetVector(RevealStId, new Vector4(
            r.worldSize.x / cs / w, r.worldSize.y / cs / h,
            (r.worldMin.x / cs - x0) / w, (r.worldMin.y / cs - y0) / h));
        image.material = mat;

        RoomReveal reveal = new RoomReveal
        {
            sceneName = r.sceneName, material = mat, texture = tex, cellMinX = x0, cellMinY = y0
        };
        reveals.Add(reveal);
        FillReveal(reveal);
    }

    private static void FillReveal(RoomReveal reveal)
    {
        int w = reveal.texture.width, h = reveal.texture.height;
        byte[] pixels = new byte[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (RunState.Current.IsExplored(reveal.sceneName, MapExploration.CellKey(reveal.cellMinX + x, reveal.cellMinY + y)))
                    pixels[y * w + x] = 255;

        reveal.texture.SetPixelData(pixels, 0);
        reveal.texture.Apply(false);
    }

    private void ClearReveals()
    {
        foreach (RoomReveal reveal in reveals)
        {
            if (reveal.material != null) Destroy(reveal.material);
            if (reveal.texture != null) Destroy(reveal.texture);
        }
        reveals.Clear();
    }

    private Transform legend;

    /// <summary>
    /// 방 지도에서는 범례를 숨기고, 아니면 원래대로 보인다. 프리팹은 건드리지 않고 실행 중에만 바꾼다.
    /// </summary>
    private void ApplyLegend()
    {
        if (legend == null)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Legend") { legend = t; break; }
        }
        if (legend == null) return;

        // 방 지도에서는 범례 글자를 통째로 뺀다.
        legend.gameObject.SetActive(!atlasMode);
    }

    /// <summary>방 그림을 담는 층. 마커·플레이어 점보다 뒤(첫 번째 자식)에 둔다.</summary>
    private void EnsureRoomLayer()
    {
        if (roomLayer == null)
        {
            GameObject go = new GameObject("Rooms", typeof(RectTransform));
            roomLayer = go.GetComponent<RectTransform>();
            roomLayer.SetParent(mapContent, false);
            roomLayer.anchorMin = roomLayer.anchorMax = roomLayer.pivot = new Vector2(0.5f, 0.5f);
            roomLayer.anchoredPosition = Vector2.zero;
            roomLayer.sizeDelta = Vector2.zero;
        }

        roomLayer.SetSiblingIndex(0);
    }

    private void ClearRooms()
    {
        foreach (GameObject go in roomImages)
            if (go != null) Destroy(go);
        roomImages.Clear();
        ClearReveals();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextFogAt) RefreshFog();

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

        // 방 지도는 제단과 내 위치만 찍는다 (몬스터·상자·출구 등은 뺀다).
        // 10.30: 아직 안 밝힌 곳의 제단도 숨긴다.
        if (atlasMode)
        {
            string sceneId = SceneManager.GetActiveScene().name;
            markers.RemoveAll(m => !ShowInRoomMap(m.Kind) ||
                                   (MapExploration.FogEnabled && !MapExploration.IsExplored(sceneId, m.Position)));
        }
        ApplyLegend();

        UpdateAutoFit(markers);

        foreach (MarkerInfo info in markers) SpawnMarker(info);

        // 순서: 지형 그림 → 안개 → 마커 → 플레이어 점.
        // 안 가본 곳은 지형만 가리고, 마커는 안개 위에 보이게 한다.
        EnsureFogImage();
        if (fogImage != null)
        {
            int index = mapImage != null && mapImage.transform.parent == mapContent
                ? mapImage.transform.GetSiblingIndex() + 1
                : 0;
            fogImage.transform.SetSiblingIndex(index);
        }

        // 마커를 나중에 만들면 플레이어 점 위에 덮인다. 점을 맨 위로 올린다.
        if (playerDot != null) playerDot.SetAsLastSibling();
    }

    // ── 안개 ────────────────────────────────

    /// <summary>
    /// 지도 판 전체를 덮는 안개 텍스처를 다시 그린다.
    /// 텍스처 한 칸 = 탐험 기록 한 칸이라 작고, 이중선형 필터로 경계가 부드럽게 번진다.
    /// </summary>
    private void RefreshFog()
    {
        nextFogAt = Time.unscaledTime + FogRefreshInterval;

        // 방 지도는 판 전체를 덮는 안개 대신, 방 그림마다 밝힌 칸만 보이게 한다 (ApplyReveal).
        // 지금 방은 지도를 연 채로 걸어도 넓어지게 계속 다시 칠한다.
        if (atlasMode)
        {
            if (fogImage != null) fogImage.enabled = false;
            string here = SceneManager.GetActiveScene().name;
            foreach (RoomReveal reveal in reveals)
                if (reveal.sceneName == here) FillReveal(reveal);
            return;
        }

        if (mapContent == null) return;

        EnsureFogImage();
        if (fogImage == null) return;

        if (!MapExploration.FogEnabled)
        {
            fogImage.enabled = false;
            return;
        }

        GetMapTransform(out Vector2 center, out float scale);
        if (scale <= 0f) return;

        Rect board = mapContent.rect;
        if (board.width < 1f || board.height < 1f) return;

        // 판의 네 귀퉁이를 월드 좌표로 되돌린다. WorldToMap의 역변환이다.
        Vector2 worldMin = board.min / scale + center;
        Vector2 worldMax = board.max / scale + center;
        Vector2 worldSize = worldMax - worldMin;

        float cell = MapExploration.CellSize;
        int width = Mathf.Clamp(Mathf.CeilToInt(worldSize.x / cell), 2, 256);
        int height = Mathf.Clamp(Mathf.CeilToInt(worldSize.y / cell), 2, 256);

        if (fogTexture == null || fogTexture.width != width || fogTexture.height != height)
        {
            if (fogTexture != null) Destroy(fogTexture);

            fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapFog"
            };
        }

        string sceneId = SceneManager.GetActiveScene().name;
        Color32 fog = FogColor;
        Color32 clear = new Color32(fog.r, fog.g, fog.b, 0);
        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 world = new Vector2(
                    worldMin.x + (x + 0.5f) / width * worldSize.x,
                    worldMin.y + (y + 0.5f) / height * worldSize.y);

                pixels[y * width + x] = MapExploration.IsExplored(sceneId, world) ? clear : fog;
            }
        }

        fogTexture.SetPixels32(pixels);
        fogTexture.Apply(false);

        fogImage.texture = fogTexture;
        fogImage.enabled = true;
    }

    /// <summary>안개 그림을 mapContent 아래에 한 번만 만든다. 프리팹은 건드리지 않는다.</summary>
    private void EnsureFogImage()
    {
        if (fogImage != null || mapContent == null) return;

        GameObject go = new GameObject("Fog", typeof(RectTransform), typeof(RawImage));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(mapContent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fogImage = go.GetComponent<RawImage>();
        fogImage.raycastTarget = false;
        fogImage.enabled = false;
    }

    /// <summary>WorldToMap과 같은 기준. 월드 → 맵: (월드 - center) * scale.</summary>
    private void GetMapTransform(out Vector2 center, out float scale)
    {
        if (fitScale > 0f)
        {
            center = fitCenter;
            scale = fitScale;
            return;
        }

        center = worldCenter;
        scale = unitsToPixels * ImageDisplayScale;
    }

    private void OnDestroy()
    {
        ClearReveals();
        if (fogTexture != null) Destroy(fogTexture);
    }

    private void Collect(List<MarkerInfo> result)
    {
        HashSet<GameObject> handled = new HashSet<GameObject>();

        // 직접 붙인 것이 우선이다. 자동 수집과 겹치면 이쪽만 남는다.
        foreach (MapMarkerSource source in
                 FindObjectsByType<MapMarkerSource>())
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

        foreach (Shrine shrine in FindObjectsByType<Shrine>())
            AddAuto(result, handled, shrine, MapMarkerKind.Shrine);

        foreach (RelicChest chest in FindObjectsByType<RelicChest>())
            AddAuto(result, handled, chest, MapMarkerKind.RelicChest);

        // 안 가본 곳으로 가는 포탈에는 "?"를 단다. 가본 곳이면 그냥 출구로 찍는다.
        foreach (ScenePortal portal in FindObjectsByType<ScenePortal>())
            AddAuto(result, handled, portal, MapMarkerKind.Exit, ExitLabel(portal));

        foreach (AutoScenePortal portal in FindObjectsByType<AutoScenePortal>())
            AddAuto(result, handled, portal, MapMarkerKind.Exit, ExitLabel(portal));

        // 부술 수 있는 벽은 "지금은 못 가는 길"이라 메트로베니아에서 제일 중요한 표시다.
        foreach (FireBreakableWall wall in FindObjectsByType<FireBreakableWall>())
            AddAuto(result, handled, wall, MapMarkerKind.BreakableWall);

        if (!showEnemies) return;

        // FindObjectsByType은 꺼져 있는 오브젝트를 빼므로 풀에 들어간 몬스터는 안 잡힌다.
        // 죽는 중이라 아직 살아 있는 것만 걸러낸다.
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
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
        // 방 지도는 ApplyRoomAtlas가 맞춰 둔 값을 쓴다.
        if (atlasMode) return;

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
                                Component component, MapMarkerKind kind, string label = "")
    {
        if (component == null) return;
        if (!handled.Add(component.gameObject)) return;

        result.Add(new MarkerInfo
        {
            Kind = kind,
            Label = label,
            Position = component.transform.position
        });
    }

    /// <summary>포탈이 가는 씬에 아직 안 가봤으면 "?". 목적지를 모르면 빈칸.</summary>
    private static string ExitLabel(Component portal)
    {
        string target = PortalTarget.SceneOf(portal);

        if (string.IsNullOrEmpty(target)) return "";

        return VisitedScenes.HasVisited(target) ? "" : "?";
    }

    private void SpawnMarker(MarkerInfo info)
    {
        GameObject go = Instantiate(markerPrefab, mapContent, false);
        go.SetActive(true);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = WorldToMap(info.Position);

        MapMarkerView view = go.GetComponent<MapMarkerView>();
        // 방 지도에서는 마커 옆 이름 글자를 뺀다.
        if (view != null) view.Bind(info.Kind, atlasMode ? "" : info.Label);

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
        bool hasImage = atlasMode || (mapImage != null && mapImage.sprite != null);

        // 스프라이트가 없는 Image는 흰 사각형으로 그려진다. 그럴 땐 아예 끈다. 방 지도일 때도 끈다.
        if (mapImage != null) mapImage.enabled = !atlasMode && mapImage.sprite != null;

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
