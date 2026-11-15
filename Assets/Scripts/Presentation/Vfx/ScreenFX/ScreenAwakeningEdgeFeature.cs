using UnityEngine;
using UnityEngine.Rendering.Universal;

//URP의 전체 화면 패스를 사용하여 각성 중 필요한 카메라에만 왜곡을 적용하는 스크립트
public sealed class ScreenAwakeningEdgeFeature : FullScreenPassRendererFeature
{
    //플레이어 제어 코드에서 전달하는 왜곡 강도 속성
    private static readonly int StrengthId = Shader.PropertyToID("_LxScreenEdgeStrength");

    //카메라 종류와 마커, 왜곡 강도를 확인한 뒤 렌더링 패스 등록
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //대상 카메라가 아니거나 왜곡이 없으면 색 복사와 왜곡 패스 생략
        if (renderingData.cameraData.cameraType != CameraType.Game
            || !renderingData.cameraData.camera.TryGetComponent<ScreenEffectsCamera>(out var marker)
            || !marker.isActiveAndEnabled
            || Shader.GetGlobalFloat(StrengthId) <= 0.000001f) return;
        //URP 기본 전체 화면 패스의 Render Graph 구현 재사용
        base.AddRenderPasses(renderer, ref renderingData);
    }
}
