using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class ParallaxRepeater : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer templateRenderer;
    [Min(2)]
    [SerializeField] private int extraTiles = 2;

    private readonly List<Transform> tiles = new List<Transform>();
    private Vector3 startLocalPosition;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogError(
                "ParallaxRepeater: Orthographic 카메라를 지정하세요.",
                this);
            enabled = false;
            return;
        }

        if (templateRenderer == null ||
            templateRenderer.sprite == null ||
            templateRenderer.transform.parent != transform)
        {
            Debug.LogError(
                "ParallaxRepeater: 바로 아래 자식 Center의 " +
                "SpriteRenderer를 지정하세요.",
                this);
            enabled = false;
            return;
        }

        startLocalPosition = templateRenderer.transform.localPosition;
        tiles.Add(templateRenderer.transform);
    }

    private void LateUpdate()
    {
        if (tiles.Count == 0 || targetCamera == null ||
            templateRenderer == null || templateRenderer.sprite == null)
        {
            return;
        }

        float spriteWidth =
            templateRenderer.drawMode == SpriteDrawMode.Simple
            ? templateRenderer.sprite.bounds.size.x
            : templateRenderer.size.x;

        float tileWidthLocal = spriteWidth *
            Mathf.Abs(templateRenderer.transform.localScale.x);

        float parentScaleX = Mathf.Abs(transform.lossyScale.x);

        if (tileWidthLocal <= Mathf.Epsilon ||
            parentScaleX <= Mathf.Epsilon)
        {
            return;
        }

        float cameraWidthWorld =
            2f * targetCamera.orthographicSize * targetCamera.aspect;

        float cameraWidthLocal = cameraWidthWorld / parentScaleX;

        int requiredCount = Mathf.Max(
            3,
            Mathf.CeilToInt(cameraWidthLocal / tileWidthLocal) +
            Mathf.Max(2, extraTiles));

        if (requiredCount % 2 == 0)
        {
            requiredCount++;
        }

        while (tiles.Count < requiredCount)
        {
            GameObject clone = Instantiate(
                templateRenderer.gameObject,
                transform);

            clone.name = templateRenderer.gameObject.name +
                "_Repeat_" + tiles.Count;

            tiles.Add(clone.transform);
        }

        Vector3 cameraLocal =
            transform.InverseTransformPoint(targetCamera.transform.position);

        int centerIndex = Mathf.RoundToInt(
            (cameraLocal.x - startLocalPosition.x) / tileWidthLocal);

        int firstIndex = centerIndex - tiles.Count / 2;

        for (int i = 0; i < tiles.Count; i++)
        {
            Vector3 position = startLocalPosition;
            position.x += (firstIndex + i) * tileWidthLocal;
            tiles[i].localPosition = position;
        }
    }
}