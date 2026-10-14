using UnityEngine;

//카메라 화면 높이에 맞춰 배경 레이어 크기를 자동으로 맞추는 스크립트
[DisallowMultipleComponent]
[DefaultExecutionOrder(950)]
public class ParallaxFitToCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Reference")]
    [Tooltip("높이 기준 SpriteRenderer. 보통 ParallaxRepeater의 Center를 넣습니다.")]
    [SerializeField] private SpriteRenderer referenceRenderer;

    [Tooltip("화면 높이 대비 배경 높이. 1이면 화면과 같고, 세로로 움직이는 레이어는 1보다 크게 둡니다.")]
    [Min(0.1f)]
    [SerializeField] private float heightMultiplier = 1f;

    private float lastOrthographicSize = -1f;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogError("ParallaxFitToCamera: Orthographic 카메라를 지정하세요.", this);
            enabled = false;
            return;
        }

        if (referenceRenderer == null || referenceRenderer.sprite == null)
        {
            Debug.LogError("ParallaxFitToCamera: 기준 SpriteRenderer를 지정하세요.", this);
            enabled = false;
            return;
        }

        Fit();
    }

    private void LateUpdate()
    {
        //카메라 크기가 바뀌었을 때만 다시 계산
        if (!Mathf.Approximately(targetCamera.orthographicSize, lastOrthographicSize))
        {
            Fit();
        }
    }

    private void Fit()
    {
        lastOrthographicSize = targetCamera.orthographicSize;

        float spriteHeight =
            referenceRenderer.drawMode == SpriteDrawMode.Simple
            ? referenceRenderer.sprite.bounds.size.y
            : referenceRenderer.size.y;

        spriteHeight *= Mathf.Abs(referenceRenderer.transform.localScale.y);

        if (spriteHeight <= Mathf.Epsilon)
        {
            return;
        }

        float targetHeight = 2f * lastOrthographicSize * heightMultiplier;

        float parentScale = transform.parent != null
            ? Mathf.Abs(transform.parent.lossyScale.y)
            : 1f;

        float scale = targetHeight / spriteHeight / parentScale;

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}