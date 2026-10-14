using UnityEngine;

[DefaultExecutionOrder(900)]
public class ParallaxLayer : MonoBehaviour
{
    public enum AxisMode
    {
        Fixed,
        FollowCamera,
        Parallax
    }

    [Header("Camera")]
    [SerializeField] private Transform targetCamera;

    [SerializeField] private float cameraDepthOffset = 10f;

    [Tooltip("시작할 때 배경 중심을 카메라 중심에 맞춥니다.")]
    [SerializeField] private bool centerOnCameraAtStart = true;

    [Tooltip("카메라가 한 프레임에 이 거리보다 많이 움직이면 순간이동으로 보고 기준점을 다시 잡습니다.")]
    [Min(1f)]
    [SerializeField] private float recenterDistance = 20f;

    [Header("X Axis")]
    [SerializeField] private AxisMode xMode = AxisMode.Parallax;

    [Range(0f, 1f)]
    [SerializeField] private float parallaxRatio = 0.5f;

    [Header("Y Axis")]
    [SerializeField] private AxisMode yMode = AxisMode.FollowCamera;

    [Range(0f, 1f)]
    [SerializeField] private float yParallaxRatio = 0.5f;

    [Header("Optional Y Limits")]
    [Tooltip("부모 배경의 월드 Y 좌표를 제한합니다.")]
    [SerializeField] private bool limitY = false;

    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 10f;

    private Vector3 initialPosition;
    private Vector3 initialCameraPosition;
    private Vector3 lastCameraPosition;
    private bool initialized;

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        if (targetCamera == null)
        {
            Debug.LogError("ParallaxLayer: Camera not found.", this);
            enabled = false;
        }
    }

    //카메라 위치를 기준점으로 설정
    private void Initialize()
    {
        Vector3 position = transform.position;

        if (centerOnCameraAtStart)
        {
            position.x = targetCamera.position.x;
            position.y = targetCamera.position.y;
        }

        position.z = targetCamera.position.z + cameraDepthOffset;

        if (limitY)
        {
            position.y = ClampY(position.y);
        }

        transform.position = position;

        initialPosition = position;
        initialCameraPosition = targetCamera.position;
        lastCameraPosition = targetCamera.position;

        initialized = true;
    }

    //외부에서 배경을 다시 카메라 중심으로 맞출 때 호출
    public void Recenter()
    {
        initialized = false;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        //처음이거나, 카메라가 순간이동했으면 기준점을 다시 잡기
        float jump = (targetCamera.position - lastCameraPosition).sqrMagnitude;

        if (!initialized || jump > recenterDistance * recenterDistance)
        {
            Initialize();
        }

        lastCameraPosition = targetCamera.position;

        Vector3 cameraDelta =
            targetCamera.position - initialCameraPosition;

        Vector3 position = initialPosition;

        position.x = CalculateAxis(
            initialPosition.x,
            cameraDelta.x,
            xMode,
            parallaxRatio);

        position.y = CalculateAxis(
            initialPosition.y,
            cameraDelta.y,
            yMode,
            yParallaxRatio);

        if (limitY)
        {
            position.y = ClampY(position.y);
        }

        transform.position = position;
    }

    private static float CalculateAxis(
        float initial,
        float cameraDelta,
        AxisMode mode,
        float ratio)
    {
        switch (mode)
        {
            case AxisMode.FollowCamera:
                return initial + cameraDelta;

            case AxisMode.Parallax:
                return initial + cameraDelta * (1f - ratio);

            default:
                return initial;
        }
    }

    private float ClampY(float y)
    {
        return Mathf.Clamp(
            y,
            Mathf.Min(minY, maxY),
            Mathf.Max(minY, maxY));
    }
}