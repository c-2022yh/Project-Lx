#if UNITY_EDITOR && LUDENS_UI_TOOLS
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using K = UIBuildKit;

/// <summary>
/// Tools/UI/글꼴 통일 (제주명조)
/// 10.19 결정: 게임 안 모든 글자를 제주명조로 맞춘다.
///   1) 제주명조 TMP 폰트 에셋이 없으면 만든다.
///   2) TMP 기본 폰트를 제주명조로 바꾼다. (코드로 만드는 글자, 새로 만드는 글자도 제주명조가 된다)
///   3) Prefabs/UI 안 모든 프리팹과 Title 씬의 글자 폰트를 제주명조로 바꾼다.
/// 다른 폴더(적·플레이어 등)의 프리팹은 건드리지 않는다. 지금은 거기에 글자가 없다.
/// </summary>
public static class FontUnifyTools
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private static readonly string[] Scenes = { "Assets/Scenes/Title.unity" };

    [MenuItem("Tools/UI/글꼴 통일 (제주명조)")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        TMP_FontAsset font = K.KoreanFont;
        if (font == null)
        {
            EditorUtility.DisplayDialog("글꼴 통일", "제주명조 폰트를 만들지 못했습니다:\n" + K.KoreanFontSourcePath, "확인");
            return;
        }

        SetDefaultFont(font);

        var log = new List<string>();
        int texts = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int n = Replace(root, font);
            if (n > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add($"{System.IO.Path.GetFileName(path)} ({n})");
                texts += n;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (string path in Scenes)
        {
            Scene already = SceneManager.GetSceneByPath(path);
            bool wasOpen = already.IsValid() && already.isLoaded;
            Scene scene = wasOpen ? already : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            int n = 0;
            foreach (GameObject go in scene.GetRootGameObjects()) n += Replace(go, font);

            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Add($"{System.IO.Path.GetFileName(path)} ({n})");
                texts += n;
            }

            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("글꼴 통일 완료",
            $"글자 {texts}개를 제주명조로 바꿨습니다.\n\n" + string.Join("\n", log) +
            "\n\nTMP 기본 폰트도 제주명조로 바꿨습니다.", "확인");
    }

    private static int Replace(GameObject root, TMP_FontAsset font)
    {
        int n = 0;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == font) continue;
            text.font = font;
            text.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(text);
            n++;
        }
        return n;
    }

    private static void SetDefaultFont(TMP_FontAsset font)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null) return;

        SerializedObject so = new SerializedObject(settings);
        SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null)
        {
            prop.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }
}
#endif
