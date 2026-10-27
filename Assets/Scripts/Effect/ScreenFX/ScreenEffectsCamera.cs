using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

//화면 효과를 적용할 실제 게임 카메라를 구분하는 스크립트
//Cinemachine 가상 카메라와 UI 전용 카메라에는 추가하지 않음
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class ScreenEffectsCamera : MonoBehaviour
{
    //현재 씬 파일을 바꾸지 않고 게임 카메라만 연결
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneEvents()
    {
        SceneManager.sceneLoaded -= ConfigureScene;
        SceneManager.sceneLoaded += ConfigureScene;
    }

    private static void ConfigureScene(Scene scene, LoadSceneMode mode)
    {
        foreach (Camera camera in Object.FindObjectsByType<Camera>())
        {
            if (camera.gameObject.scene != scene || scene.name == "Title"
                || !camera.CompareTag("MainCamera")) continue;

            if (camera.GetComponent<ScreenEffectsCamera>() == null)
                camera.gameObject.AddComponent<ScreenEffectsCamera>();
        }
    }

    //에디터 메뉴로 미리 붙인 카메라와 런타임 카메라에 동일한 설정 적용
    private void OnEnable()
    {
        Camera camera = GetComponent<Camera>();
        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        camera.allowHDR = true;
        data.renderPostProcessing = true;
        data.volumeLayerMask |= 1;
        camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
    }
}
