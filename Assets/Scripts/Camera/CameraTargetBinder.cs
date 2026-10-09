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
        cinemachineCamera.PreviousStateIsValid = false; //첫 프레임에 바로 플레이어 위치로 이동
    }
}