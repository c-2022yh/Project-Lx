using UnityEngine;

[RequireComponent(typeof(OrbFollower))]
public class EnergyOrb : MonoBehaviour
{
    [Header("Orb Components")]
    [SerializeField] private OrbLight orbLight;

    private OrbFollower follower;

    private void Awake()
    {
        follower = GetComponent<OrbFollower>();
    }

    public void Initialize(Transform target)
    {
        follower.Initialize(target);
    }

    public void SetLight(float intensity, float radius, Color color)
    {
        orbLight.SetLight(intensity, radius, color);
    }
}
