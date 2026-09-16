using UnityEngine;
using Unity.Cinemachine;

public class CameraTargetBinder : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Start()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning("Player를 찾지 못했습니다.", this);
            return;
        }

        cinemachineCamera.Follow = player.transform;
    }
}