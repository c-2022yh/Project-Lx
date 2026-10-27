#if UNITY_EDITOR && LUDENS_UI_TOOLS
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools/UI/일시정지에 설정 버튼 넣기
/// System_Canvas 프리팹의 일시정지 창에 [설정] 버튼을 넣고, 같은 프리팹 안에 설정창을 만들어 연결한다.
/// 여러 번 눌러도 된다. 설정창은 지우고 새로 만들고, 버튼은 이미 있으면 그대로 쓴다.
/// 버튼 순서: 계속 / 조작법 / 설정 / 나가기
/// </summary>
public static class PauseSettingsTools
{
    private const string PrefabPath = "Assets/Prefabs/UI/System_Canvas/System_Canvas.prefab";
    private const float TopY = 50f;
    private const float Gap = 90f;

    [MenuItem("Tools/UI/일시정지에 설정 버튼 넣기")]
    public static void Run()
    {
        if (!UIBuildKit.EnsureKoreanFont()) return;

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            PausePanel pause = root.GetComponentInChildren<PausePanel>(true);
            if (pause == null)
            {
                EditorUtility.DisplayDialog("일시정지에 설정 버튼 넣기", "System_Canvas에서 PausePanel을 찾지 못했습니다.", "확인");
                return;
            }

            Button resume = FindButton(pause, "OnResumeButton");
            Button guide = FindButton(pause, "OnControlGuideButton");
            Button quit = FindButton(pause, "OnQuitButton");
            Button settings = FindButton(pause, "OnSettingsButton");

            if (settings == null)
            {
                Button template = guide != null ? guide : resume;
                if (template == null)
                {
                    EditorUtility.DisplayDialog("일시정지에 설정 버튼 넣기", "본뜰 버튼(조작법/계속)을 찾지 못했습니다.", "확인");
                    return;
                }

                GameObject copy = Object.Instantiate(template.gameObject, template.transform.parent);
                copy.name = "SettingsButton";
                settings = copy.GetComponent<Button>();

                for (int i = settings.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(settings.onClick, i);
                UnityEventTools.AddPersistentListener(settings.onClick, pause.OnSettingsButton);

                TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "설정";
            }

            // 계속 / 조작법 / 설정 / 나가기 순서로 세로 배치
            Button[] order = { resume, guide, settings, quit };
            float y = TopY;
            foreach (Button b in order)
            {
                if (b == null) continue;
                RectTransform rt = (RectTransform)b.transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                y -= Gap;
            }

            // 설정창: 이전 것을 지우고 새로 만든다. 일시정지 창보다 위에 그린다.
            Transform old = root.transform.Find("SettingsPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            SettingsPanel panel = SettingsUIBuilder.Build(root.transform);
            panel.transform.SetAsLastSibling();
            panel.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(pause);
            so.FindProperty("settingsPanel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorUtility.DisplayDialog("완료!",
            "일시정지 창에 [설정] 버튼을 넣고 설정창을 연결했습니다.\n\n" +
            "게임 중 Esc → 설정. 설정창에서 Esc나 뒤로가기를 누르면 일시정지로 돌아옵니다.", "확인");
    }

    private static Button FindButton(PausePanel pause, string method)
    {
        foreach (Button b in pause.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == method) return b;
        }
        return null;
    }
}
#endif
