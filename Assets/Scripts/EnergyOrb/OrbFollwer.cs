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
    private Object presentationOwner;

    public bool IsPresentationControlledBy(Object owner) => owner != null && presentationOwner == owner;
    public Vector3 FollowPosition => target == null ? transform.position
        : GetTargetPosition() + Vector3.up * (Mathf.Sin(Time.time * floatFrequency) * floatAmplitude);

    public bool TryBeginPresentation(Object owner)
    {
        if (owner == null || target == null || !isActiveAndEnabled
            || (presentationOwner != null && presentationOwner != owner)) return false;
        presentationOwner = owner;
        velocity = Vector3.zero;
        return true;
    }

    public void EndPresentation(Object owner)
    {
        if (presentationOwner != owner) return;
        presentationOwner = null;
        velocity = Vector3.zero;
    }

    private void OnDisable()
    {
        presentationOwner = null;
        velocity = Vector3.zero;
    }

    public void Initialize(Transform newTarget)
    {
        target = newTarget;
        presentationOwner = null;
        velocity = Vector3.zero;

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
        if (target == null || presentationOwner != null)
            return;

        Vector3 targetPosition = FollowPosition;

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
