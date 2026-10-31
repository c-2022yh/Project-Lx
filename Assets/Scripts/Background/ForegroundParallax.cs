using UnityEngine;

/// <summary>
/// 캐릭터 앞을 지나가는 근경용 패럴랙스.
///
/// 맵에 놓은 자리를 기준으로, 카메라가 움직이면 땅보다 조금 더 빠르게 반대쪽으로 지나간다.
/// 그래서 땅보다 카메라에 가까이 있는 것처럼 보인다.
/// 카메라 중심이 이 물체가 놓인 자리에 올 때 정확히 놓은 자리에 보인다.
/// 에디터에서 Play를 누르지 않았을 때는 놓은 자리에 그대로 보이니 그냥 보이는 대로 배치하면 된다.
///
/// 사용법
/// 1) 근경 그림(나뭇가지, 풀, 기둥 등)을 맵에 놓고 SpriteRenderer의 Sorting Layer를 Player보다 위(예: Foreground)로 둔다.
/// 2) 이 스크립트를 붙인다. 그림마다 붙이거나, 가까이 모여 있는 그림들은 빈 부모에 한 번만 붙인다.
///    멀리 떨어진 그림들을 한 부모에 묶으면 기준점이 하나라 엉뚱한 자리에 보이니 묶지 않는다.
/// 3) Speed: 1이면 땅과 같이 움직이고, 클수록 빨리 지나가서 더 가까워 보인다. 보통 1.2~1.5.
/// </summary>
[DefaultExecutionOrder(900)] // ParallaxLayer와 같은 순서: 카메라가 움직인 다음에 위치를 정한다
public class ForegroundParallax : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("비워 두면 Main Camera")]
    [SerializeField] private Transform targetCamera;

    [Header("X")]
    [Tooltip("1이면 땅과 같이, 1보다 크면 땅보다 빠르게 지나간다 (가까워 보임). 보통 1.2~1.5")]
    [Range(1f, 3f)]
    [SerializeField] private float speed = 1.3f;

    [Header("Y")]
    [Tooltip("위아래로도 빠르게 움직일지. 끄면 위아래는 땅과 같이 움직인다.")]
    [SerializeField] private bool moveY = false;

    [Range(1f, 3f)]
    [SerializeField] private float ySpeed = 1.1f;

    private Vector3 placedPosition;

    private void Awake()
    {
        // 맵에 놓은 자리를 기억해 둔다
        placedPosition = transform.position;
    }

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        if (targetCamera == null)
        {
            Debug.LogError("ForegroundParallax: Camera not found.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.position;
        Vector3 position = placedPosition;

        // 카메라가 놓은 자리에서 멀어진 만큼, (speed - 1)배 더 반대쪽으로 민다
        position.x = placedPosition.x - (cameraPosition.x - placedPosition.x) * (speed - 1f);

        if (moveY)
        {
            position.y = placedPosition.y - (cameraPosition.y - placedPosition.y) * (ySpeed - 1f);
        }

        transform.position = position;
    }

#if UNITY_EDITOR
    // Scene 창에서 놓은 자리를 작은 십자로 보여 준다 (Play 중에 그림이 움직여도 기준점을 알 수 있게)
    private void OnDrawGizmosSelected()
    {
        Vector3 p = Application.isPlaying ? placedPosition : transform.position;
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.9f);
        Gizmos.DrawLine(p + Vector3.left * 0.5f, p + Vector3.right * 0.5f);
        Gizmos.DrawLine(p + Vector3.down * 0.5f, p + Vector3.up * 0.5f);
    }
#endif
}
