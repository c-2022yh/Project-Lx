using UnityEngine;

public class OrbFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField]
    private Vector3 offset =
        new Vector3(-0.6f, 0.8f, 0f);

    [SerializeField, Min(0.01f)]
    private float followSpeed = 5f;

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatFrequency = 2f;

    private Transform target;
    private Vector3 velocity;

    public void Initialize(Transform newTarget)
    {
        target = newTarget;

        if (target == null)
            return;

        transform.position = GetTargetPosition();
    }

    private void Update()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null)
            return;

        Vector3 targetPosition = GetTargetPosition();

        float floatingOffset =
            Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        targetPosition.y += floatingOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            1f / followSpeed
        );
    }

    private Vector3 GetTargetPosition()
    {
        float lookDirection =
            target.localScale.x > 0f ? -1f : 1f;

        return target.position + new Vector3(
            offset.x * lookDirection,
            offset.y,
            offset.z
        );
    }
}