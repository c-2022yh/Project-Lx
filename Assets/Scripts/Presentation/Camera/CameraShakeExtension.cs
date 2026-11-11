using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// CinemachineCamera에 붙어서 CameraShake의 흔들림을 카메라 위치에 더한다.
/// CameraShake가 알아서 붙이므로 직접 붙일 필요는 없다.
/// </summary>
[AddComponentMenu("")]
public class CameraShakeExtension : CinemachineExtension
{
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise) return;
        if (!Application.isPlaying) return;
        state.PositionCorrection += CameraShake.CurrentOffset;
    }
}
