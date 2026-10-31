using UnityEngine;

/// <summary>
/// 튜토리얼 동굴 배경.
///
///   [동굴 안] [동굴 안(좌우 뒤집음)] [출구]  을 가로로 이어 붙인 긴 그림을 만들고,
///   카메라 x에 맞춰 보이는 부분을 정한다. 시작에서는 맨 왼쪽, 끝(전투 구역)에서는 출구 구멍이 화면 가운데에 온다.
///   두 번째 동굴 그림은 좌우를 뒤집어서 이음새가 자연스럽게 맞는다.
///   출구 그림은 왼쪽 끝이 흐려지게 만들어 둬서 동굴 그림 위에 겹쳐 이어진다.
///
/// 보이는 위치를 카메라 위치만으로 정하므로, 죽고 제단에서 다시 살아나도 배경이 어긋나지 않는다.
/// 빛을 안 받는 머티리얼을 써서 동굴 밝기 조명(CaveLightRamp)에 어두워지지 않는다. 밝고 어두움은 그림 자체로 보인다.
/// 튜토리얼 맵 빌더가 Background_Cave 오브젝트에 붙이고 그림을 채운다.
/// </summary>
[DefaultExecutionOrder(900)]   // 카메라가 움직인 다음에 자리를 정한다 (ParallaxLayer와 같은 순서)
public class CavePanoramaBackground : MonoBehaviour
{
    [Header("그림")]
    [SerializeField] private Sprite insideSprite;
    [SerializeField] private Sprite exitSprite;

    [Header("크기·위치")]
    [Tooltip("배경 기본 높이 (월드 단위). 화면이 더 크면(카메라 크기 7이면 높이 14) 화면에 맞춰 자동으로 키운다")]
    [SerializeField] private float height = 11f;
    [Tooltip("출구 그림이 앞 그림과 겹치는 비율 (그림 왼쪽 흐린 부분 폭)")]
    [SerializeField, Range(0f, 0.5f)] private float exitOverlap = 0.15f;
    [Tooltip("출구 그림에서 구멍 가운데 (0 왼쪽 ~ 1 오른쪽). 끝에서 이 지점이 화면 가운데에 온다")]
    [SerializeField, Range(0f, 1f)] private float exitFocus = 0.58f;
    [Tooltip("배경을 카메라보다 위아래로 옮김")]
    [SerializeField] private float yOffset = 0f;

    [Header("카메라가 보여주는 맵 범위 (월드 x, CameraConfiner와 같게)")]
    [SerializeField] private float viewLeftX = 0f;
    [SerializeField] private float viewRightX = 140f;

    [Header("그리기 순서")]
    [SerializeField] private string sortingLayer = "BackGround";
    [SerializeField] private int sortingOrder = -100;

    private Camera cam;
    private Transform strip;
    private float stripWidth;
    private float exitCenter;

    private void Start()
    {
        strip = new GameObject("Strip").transform;
        strip.SetParent(transform, false);

        float wIn = Width(insideSprite);
        float wEx = Width(exitSprite);
        Piece(insideSprite, 0f, false, 0);
        Piece(insideSprite, wIn, true, 0);
        float exitLeft = 2f * wIn - wEx * exitOverlap;
        Piece(exitSprite, exitLeft, false, 1);

        stripWidth = exitLeft + wEx;
        exitCenter = exitLeft + wEx * exitFocus;
    }

    private float Width(Sprite s) => s == null ? 0f : height * s.rect.width / s.rect.height;

    private void Piece(Sprite s, float left, bool flip, int orderAdd)
    {
        if (s == null) return;

        GameObject go = new GameObject(s.name);
        go.transform.SetParent(strip, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.flipX = flip;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder + orderAdd;

        // 피벗과 상관없이 왼쪽 끝을 left에, 세로 가운데를 0에
        Bounds b = s.bounds;
        float scale = height / b.size.y;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        float minX = flip ? -b.max.x : b.min.x;
        go.transform.localPosition = new Vector3(left - minX * scale, -b.center.y * scale, 0f);
    }

    private void LateUpdate()
    {
        if (strip == null) return;
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // 화면 크기에 맞춰 그림을 키운다 (위아래가 잘려 보이지 않게)
        float viewH = cam.orthographic ? cam.orthographicSize * 2f : 14f;
        float halfW = viewH * 0.5f * cam.aspect;
        float s = Mathf.Max(1f, (viewH + 1f) / height);
        strip.localScale = new Vector3(s, s, 1f);
        float width = stripWidth * s;
        float focus = exitCenter * s;

        float from = halfW + 0.2f;                                         // 시작: 그림 왼쪽 끝이 화면 왼쪽
        float to = Mathf.Max(from, Mathf.Min(focus, width - halfW - 0.2f)); // 끝: 출구 구멍이 가운데 (그림 밖은 안 보이게)
        // 카메라가 실제로 움직일 수 있는 범위 (맵 범위에서 화면 반 폭씩 안쪽)
        float k = Mathf.InverseLerp(viewLeftX + halfW, viewRightX - halfW, cam.transform.position.x);
        float local = Mathf.Lerp(from, to, k);

        Vector3 p = cam.transform.position;
        transform.position = new Vector3(p.x - local, p.y + yOffset, 0f);
    }
}
