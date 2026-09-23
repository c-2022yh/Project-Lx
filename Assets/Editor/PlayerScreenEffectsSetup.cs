using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//에디터 메뉴에서 화면 효과에 필요한 에셋과 연결을 설정하는 스크립트
//메뉴를 직접 실행할 때만 변경하며, 게임 빌드에는 포함되지 않음
public static class PlayerScreenEffectsSetup
{
    //설정 에셋을 생성할 폴더와 효과를 연결할 플레이어 프리팹 경로
    private const string Folder = "Assets/Settings/ScreenFX";
    private const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";

    //비네팅 프로필, 왜곡 머티리얼, 렌더러 기능과 플레이어 연결 설정
    [MenuItem("Project Lx/Screen Effects/1. Set Up Player and Renderer")]
    public static void SetUp()
    {
        //플레이 중에는 프로젝트 설정 에셋을 변경하지 않음
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        //설정 대상 에셋과 이름으로 등록된 왜곡 셰이더 확인
        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Shader edgeShader = Shader.Find("ProjectLx/ScreenAwakeningEdge");
        //필수 에셋이 없으면 이후 설정을 진행하지 않음
        if (renderer == null || prefab == null || edgeShader == null)
        {
            Debug.LogError("Player Screen FX: expected renderer, player prefab or shaders are missing. Check import errors first.");
            return;
        }
        //이전 4종 효과 코드와 중복 적용되는 것을 방지
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript PlayerVisualEffects"))
        {
            if (Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == "PlayerVisualEffects")
            {
                Debug.LogError("Old four-effect patch detected. Use a clean original project; see SCREEN_FX_GUIDE.md.");
                return;
            }
        }
        //생성 폴더를 준비하고 새 에셋을 Unity에 반영
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Material edge = GetMaterial("AwakeningEdge", edgeShader);
        string profilePath = Folder + "/PlayerVignette.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        //기존 프로필이 있으면 재사용하고 없을 때만 새로 생성
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        //비네팅 컴포넌트를 프로필의 서브에셋으로 저장
        if (!profile.TryGet<Vignette>(out var vignette))
        {
            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.45f);
            AssetDatabase.AddObjectToAsset(vignette, profile);
            EditorUtility.SetDirty(profile);
        }

        //같은 왜곡 기능이 중복 추가되지 않도록 기존 기능 검색
        ScreenAwakeningEdgeFeature feature = null;
        foreach (var item in renderer.rendererFeatures)
            if (item is ScreenAwakeningEdgeFeature found) { feature = found; break; }
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<ScreenAwakeningEdgeFeature>();
            feature.name = "Player Awakening Edge";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
        }
        //왜곡을 후처리 전에 실행하여 이후 비네팅 적용
        feature.passMaterial = edge;
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        //왜곡에 사용할 현재 화면 색을 복사하고 불필요한 깊이 입력은 요구하지 않음
        feature.fetchColorBuffer = true;
        feature.requirements = ScriptableRenderPassInput.None;
        feature.passIndex = 0;
        feature.SetActive(true);
        feature.Create();
        //기존 렌더러 기능을 포함하여 서브에셋 참조 목록을 실제 기능 목록과 맞춤
        var rendererData = new SerializedObject(renderer);
        var featureMap = rendererData.FindProperty("m_RendererFeatureMap");
        featureMap.arraySize = renderer.rendererFeatures.Count;
        for (int i = 0; i < renderer.rendererFeatures.Count; i++)
        {
            long localId = 0;
            if (renderer.rendererFeatures[i] != null)
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string guid, out localId);
            featureMap.GetArrayElementAtIndex(i).longValue = localId;
        }
        rendererData.ApplyModifiedPropertiesWithoutUndo();
        renderer.SetDirty();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(renderer);

        //플레이어 프리팹 내용을 열어 효과 제어 컴포넌트 연결
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var visuals = root.GetComponent<PlayerScreenEffects>();
            if (visuals == null) visuals = root.AddComponent<PlayerScreenEffects>();
            Assign(visuals, "volumeTemplate", profile);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        //설정 도중 문제가 발생해도 임시로 연 프리팹 내용 정리
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("Player Screen FX assets configured. Run menu 2 in each gameplay scene, then save the scene.");
    }

    //열린 게임 씬의 실제 메인 카메라에 효과 적용 설정
    [MenuItem("Project Lx/Screen Effects/2. Configure Gameplay Cameras in Open Scenes")]
    public static void ConfigureCameras()
    {
        //플레이 중에는 프로젝트 설정 에셋을 변경하지 않음
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int count = 0;
        //비활성 오브젝트까지 포함하여 열린 씬의 카메라 확인
        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            //로드된 씬의 MainCamera만 대상으로 지정
            if (!camera.gameObject.scene.IsValid() || !camera.gameObject.scene.isLoaded || !camera.CompareTag("MainCamera")) continue;
            //타이틀 화면의 카메라는 설정 대상에서 제외
            if (camera.gameObject.scene.name == "Title") continue;
            //효과 적용 마커가 없을 때만 추가
            if (camera.GetComponent<ScreenEffectsCamera>() == null)
                Undo.AddComponent<ScreenEffectsCamera>(camera.gameObject);
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            Undo.RecordObject(data, "Enable Player Screen FX Post Processing");
            //비네팅 후처리를 켜고 매 프레임 Volume 상태 갱신
            data.renderPostProcessing = true;
            camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
            data.volumeLayerMask |= 1; //실행 중 생성하는 비네팅 Volume의 Default 레이어 포함
            //프리팹 인스턴스 변경을 기록하고 씬을 저장 필요 상태로 표시
            PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            count++;
        }
        Debug.Log($"Player Screen FX: configured {count} MainCamera(s). Save the open scenes to keep these settings.");
    }

    //같은 경로의 머티리얼이 있으면 재사용하고 없으면 생성
    private static Material GetMaterial(string name, Shader shader)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    //직렬화된 참조 필드에 에셋을 연결
    private static void Assign(Object target, string property, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(property).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
