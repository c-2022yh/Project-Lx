using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 이 영역(트리거) 안에 플레이어가 있는 동안만 카메라를 넓게(줌 아웃) 보여준다.
/// 나가면 원래 크기로 돌아간다. 씬마다 필요한 곳에 BoxCollider2D(Is Trigger)와 같이 붙인다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CameraZoomZone : MonoBehaviour
{
    [Tooltip("영역 안에서 쓸 카메라 크기 (Orthographic Size). 기본 맵은 7")]
    [SerializeField] private float zoomSize = 9f;
    [Tooltip("바뀌는 데 걸리는 시간 (초)")]
    [SerializeField] private float blendTime = 0.6f;

    private CinemachineCamera cam;
    private CinemachineConfiner2D confiner;
    private float baseSize = -1f;
    private int inside;   // 플레이어 콜라이더가 여러 개일 수 있어서 개수로 센다

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private bool FindCamera()
    {
        if (cam != null) return true;
        cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam == null) return false;
        confiner = cam.GetComponent<CinemachineConfiner2D>();
        baseSize = cam.Lens.OrthographicSize;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null) return;
        inside++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null) return;
        inside = Mathf.Max(0, inside - 1);
    }

    private void OnDisable()
    {
        inside = 0;
        if (cam != null && baseSize > 0f) SetSize(baseSize);
    }

    private void LateUpdate()
    {
        if (!FindCamera() || baseSize <= 0f) return;
        float target = inside > 0 ? zoomSize : baseSize;
        float now = cam.Lens.OrthographicSize;
        if (Mathf.Abs(now - target) < 0.001f) return;
        // 원래 크기와 줌 크기 차이를 blendTime 동안 일정한 속도로 옮긴다
        float speed = Mathf.Abs(zoomSize - baseSize) / Mathf.Max(0.01f, blendTime);
        SetSize(Mathf.MoveTowards(now, target, speed * Time.unscaledDeltaTime));
    }

    private void SetSize(float size)
    {
        LensSettings lens = cam.Lens;
        lens.OrthographicSize = size;
        cam.Lens = lens;
        if (confiner != null) confiner.InvalidateLensCache();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
#endif
}
