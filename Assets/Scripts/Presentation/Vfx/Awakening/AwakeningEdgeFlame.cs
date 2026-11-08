using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 각성 화면 가장자리 불꽃을 일렁이는 불꽃 혀 모양으로 바꾼다 (기존: 일자 띠).
/// ScreenFXController가 켜고 끄는 가장자리 이미지(awakeningEdgeFX)의 머티리얼만 실행 중에 바꿔 끼운다.
/// ScreenFX 프리팹·ScreenFXController·기존 셰이더 그래프 파일은 고치지 않는다.
/// 셰이더: Resources/Presentation/Awakening/AwakeningEdgeFlame. 씬이 열릴 때 스스로 찾아서 바꾼다.
/// </summary>
public class AwakeningEdgeFlame : MonoBehaviour
{
    private const string ShaderPath = "Presentation/Awakening/AwakeningEdgeFlame";
    private static AwakeningEdgeFlame instance;
    private static readonly FieldInfo EdgeField =
        typeof(ScreenFXController).GetField("awakeningEdgeFX", BindingFlags.Instance | BindingFlags.NonPublic);

    private Material material;
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("AwakeningEdgeFlame (Runtime)") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AwakeningEdgeFlame>();
    }

    private void Start()
    {
        Shader shader = Resources.Load<Shader>(ShaderPath);
        if (shader == null || EdgeField == null)
        {
            Debug.LogWarning($"[각성 가장자리] 꺼짐: 셰이더 {(shader != null ? "있음" : "없음")}, ScreenFXController.awakeningEdgeFX {(EdgeField != null ? "있음" : "없음")}");
            enabled = false;
            return;
        }
        material = new Material(shader) { hideFlags = HideFlags.DontSave };
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (material != null) Destroy(material);
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m) => nextScan = 0f;

    // UI가 늦게 생길 수 있어서 가끔 다시 찾는다 (1초에 한 번, 가볍게)
    private static readonly int FlameColorId = Shader.PropertyToID("_FlameColor");

    private void Update()
    {
        if (material != null) material.SetColor(FlameColorId, AwakeningColors.Main());   // 보주마다 원래 색
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1f;

        foreach (ScreenFXController c in FindObjectsByType<ScreenFXController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject edge = EdgeField.GetValue(c) as GameObject;
            if (edge == null) continue;
            Graphic g = edge.GetComponent<Graphic>();
            if (g == null || g.material == material) continue;
            g.material = material;
            g.color = Color.white;
            Debug.Log("[각성 가장자리] 화면 가장자리 불꽃을 일렁이는 모양으로 바꿨습니다.");
        }
    }
}
