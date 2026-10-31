using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 필드 배경 그림 한 장(가로:세로 2:1)을 카메라를 따라 보여준다. 위아래로 높은 맵에서도 끝이 안 보인다.
///
///   카메라가 맵 왼쪽 끝 → 그림 왼쪽 끝, 오른쪽 끝 → 그림 오른쪽 끝
///   카메라가 맵 맨 아래 → 그림 아래(땅), 맨 위 → 그림 위(나무 위·천장)
///   가로와 세로가 같은 빠르기로 천천히 흐르도록 그림 크기를 자동으로 정한다.
///
/// 맵 범위는 카메라 가둠(CinemachineConfiner2D)의 영역에서 읽는다. 빛을 안 받는 머티리얼이라 조명에 어두워지지 않는다.
///
/// [시험 중] 씬 파일은 고치지 않는다. 씬이 열릴 때 아래 표의 씬이면 기존 Background_Forest를 끄고 이 배경을 만든다.
///   에디터: Tools/Test/새 필드 배경 (F_01 밝은 숲, F_02·F_07 청록 숲) 으로 켜고 끈다 (기본 꺼짐, 내 컴퓨터에만 적용).
///   빌드에서는 동작하지 않는다.
/// </summary>
[DefaultExecutionOrder(900)]   // 카메라가 움직인 다음에 자리를 정한다
public class FieldPanoramaBackground : MonoBehaviour
{
    // 씬 이름 → Resources 그림 경로
    private static readonly Dictionary<string, string> SceneArt = new Dictionary<string, string>
    {
        { "F_01", "Presentation/Backgrounds/BG_F01_SunlitForest" },
        { "F_02", "Presentation/Backgrounds/BG_F02_TealForest" },
        { "F_07", "Presentation/Backgrounds/BG_F02_TealForest" },   // F_02와 같은 청록 숲
    };

    [SerializeField] private Sprite sprite;
    [SerializeField] private string sortingLayer = "BackGround";
    [SerializeField] private int sortingOrder = -100;

    private SpriteRenderer sr;
    private Camera cam;
    private Bounds area;
    private bool hasArea;

    // ── 시험용 자동 적용 ────────────────────

#if UNITY_EDITOR
    private const string PrefKey = "Ludens.Test.FieldPanorama";
    private const string MenuPath = "Tools/Test/새 필드 배경 (F_01 밝은 숲, F_02 청록 숲)";
    private static bool TestEnabled => EditorPrefs.GetBool(PrefKey, false);

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        bool next = !TestEnabled;
        EditorPrefs.SetBool(PrefKey, next);
        Debug.Log(next ? "[필드 배경] 켬: F_01·F_02를 Play하면 새 배경이 나옵니다." : "[필드 배경] 끔: 원래 배경으로 돌아갑니다.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, TestEnabled);
        return true;
    }
#else
    private static bool TestEnabled => false;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++) Apply(SceneManager.GetSceneAt(i));
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    private static void Apply(Scene scene)
    {
        if (!TestEnabled || !scene.isLoaded || !SceneArt.TryGetValue(scene.name, out string path)) return;
        Sprite art = Resources.Load<Sprite>(path);
        if (art == null) { Debug.LogWarning($"[필드 배경] 그림을 못 찾았습니다: Resources/{path}"); return; }

        // 기존 배경 끄기
        int hidden = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Background_Forest") && t.gameObject.activeSelf) { t.gameObject.SetActive(false); hidden++; }

        GameObject go = new GameObject("Field_Panorama (Test)");
        SceneManager.MoveGameObjectToScene(go, scene);
        FieldPanoramaBackground bg = go.AddComponent<FieldPanoramaBackground>();
        bg.sprite = art;

        // 다른 배경 그림이 남아 있으면 이름을 알려 준다 (새 배경을 가릴 수 있음)
        List<string> others = new List<string>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(false))
                if (r.sortingLayerName == "BackGround" && r.gameObject != go && others.Count < 8) others.Add(r.name);
        Debug.Log($"[필드 배경] {scene.name}: {art.name} 적용, 기존 Background_Forest {hidden}개 끔."
                  + (others.Count > 0 ? $" 남은 배경 그림: {string.Join(", ", others)}" : ""));
    }

    // ── 배경 ───────────────────────────────

    private void Start()
    {
        if (sprite == null) { enabled = false; return; }
        GameObject g = new GameObject(sprite.name);
        g.transform.SetParent(transform, false);
        sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = BossFxArt.Unlit;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;
        FindArea();
    }

    // 카메라 가둠 영역 = 카메라가 보여줄 수 있는 맵 범위
    private void FindArea()
    {
        foreach (CinemachineConfiner2D c in FindObjectsByType<CinemachineConfiner2D>(FindObjectsInactive.Include))
        {
            if (c.BoundingShape2D == null) continue;
            area = c.BoundingShape2D.bounds;
            hasArea = area.size.x > 0.01f && area.size.y > 0.01f;
            if (hasArea) return;
        }
        Debug.LogWarning("[필드 배경] 카메라 가둠 영역을 못 찾아서 카메라에 고정합니다.");
    }

    private void LateUpdate()
    {
        if (sr == null) return;
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        float viewH = cam.orthographic ? cam.orthographicSize * 2f : 14f;
        float viewW = viewH * cam.aspect;
        float aspect = sprite.bounds.size.x / sprite.bounds.size.y;
        Vector3 c = cam.transform.position;

        // 그림 폭: 화면을 덮는 최소 크기, 그리고 가로·세로가 같은 빠르기로 흐르는 크기 중 큰 것
        float minW = Mathf.Max(viewW, viewH * aspect) + 0.2f;
        float w = minW;
        float kx = 0.5f, ky = 0.5f;
        if (hasArea)
        {
            float tx = area.size.x - viewW;   // 카메라가 가로로 움직이는 거리
            float ty = area.size.y - viewH;   // 세로로 움직이는 거리
            if (tx > 0.01f && ty > 0.01f)
            {
                float den = 1f / tx - 1f / (aspect * ty);
                if (den > 0.0001f) w = Mathf.Max(minW, (viewW / tx - viewH / ty) / den);
            }
            kx = tx > 0.01f ? Mathf.InverseLerp(area.min.x + viewW * 0.5f, area.max.x - viewW * 0.5f, c.x) : 0.5f;
            ky = ty > 0.01f ? Mathf.InverseLerp(area.min.y + viewH * 0.5f, area.max.y - viewH * 0.5f, c.y) : 0.5f;
        }
        float h = w / aspect;

        // 카메라가 왼쪽 아래면 그림 왼쪽 아래가 보이게
        Vector3 center = new Vector3(c.x + (0.5f - kx) * (w - viewW), c.y + (0.5f - ky) * (h - viewH), 0f);
        float s = w / sprite.bounds.size.x;
        sr.transform.localScale = new Vector3(s, s, 1f);
        transform.position = center - sprite.bounds.center * s;
    }
}
