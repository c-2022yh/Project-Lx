#if UNITY_EDITOR && LUDENS_UI_TOOLS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 기획 문서(구글 docs)의 스킬 아이콘을 프로젝트로 가져와 SkillData에 연결한다.
///
/// 문서에 올라간 그림은 누구나 열 수 있는 주소로 제공되므로 로그인 없이 받아진다.
/// 받은 PNG는 Assets/Sprites/UI/SkillIcon/ 에 저장하고 Sprite로 임포트한 뒤
/// 같은 이름의 SkillData 에셋의 icon 칸에 넣는다.
///
/// 문서의 그림이 바뀌면 주소도 바뀐다. 그때는 아래 표의 주소만 갈아끼우면 된다.
/// 이미 받아둔 파일이 있으면 다시 받지 않는다. 새로 받으려면 파일을 지우고 실행한다.
/// </summary>
public static class SkillIconImporter
{
    private const string IconFolder = "Assets/Sprites/UI/SkillIcon";

    private readonly struct Entry
    {
        public readonly string Label;      // 문서에 적힌 이름
        public readonly string AssetName;  // SkillData 에셋 파일 이름
        public readonly string Url;

        public Entry(string label, string assetName, string url)
        {
            Label = label;
            AssetName = assetName;
            Url = url;
        }
    }

    private static readonly Entry[] Entries =
    {
        new Entry("전진", "ThrustSkillData",
            "https://docs.google.com/docs-images-rt/ALKuztacyUPqUT_vuxSvzngFqi4ALOq41pOPyLoJqb2uJKwsXPq6tpwA2HrW28l_kaERWAPYcvp3tZnL1LtsDRt6Q0dnlPudtjWI9N8py95oofcE5aQUdLCJ_FtMEXRm59ox3zoBRPyvnloNB-NGQegUVtoa62xUi51HySmMyhs=s512"),
        new Entry("패링", "GuardSkillData",
            "https://docs.google.com/docs-images-rt/ALKuztYkh-1Dg6mHac3iSp9R2OUj6RIVHUDN9J3jLpB-om2RHrok7uc-V1rQsd2HaV8ocJ9hF315GkO5-uzbjJfHPEkdBrimo9N_WFlsAq1U3f2ayZL4dmUfBAOekPAROBOVzbBCbxa_UrdRyQPH2ALxRKO6QVafkOL3g1nEtsEW3A=s512"),
        new Entry("회복", "VitalConversionSkillData",
            "https://docs.google.com/docs-images-rt/ALKuztYMCMCe6ch4IyQoVrkzZJBUXjsqAsRC6FGsbn7u1CTMtqFIzVTBlZDMGYKBH1l7UsltX17rilxlRlpjqlchDEtxrczn-cDOGVg28Ro0JLvsQCsDZFLt92IGjPRRjDJC2ANd3_z-gQoTa-PwZ-Mjh1l_pj7ECsTOfIipWHLaKg=s512"),
        new Entry("그림자", "ShadowSwapSkillData",
            "https://docs.google.com/docs-images-rt/ALKuzta6ZsE5X9KRn2YlPfXQyvRTqm2U3pew0p7Wm75OsnmBmTfMKaSy0teQ7lzbWDiBsNL9TxyVVn7XCtCeg5FfGrJT5joH-EtcD-ixQqHzJPtRoBwCmW7wyFnBtdcti4YQ0xSR4lpzpOzvSYV3fmOlRuRJinFAvRjZ1n6pNhQ=s512"),
        new Entry("검기", "ProjectileShotSkillData",
            "https://docs.google.com/docs-images-rt/ALKuztb49o4HsH3Apb-mb_UrGts8xhyZEtuojaEAHYiBdghyjFIxk53jJ4IMDgTYr5cOU9azDQIulWPtPoP2_9zHdj-bd4tl0uQS5SPkMkgsGlPZy_UEn9zq_pntSg0I_WZKQe27usovdaTw3n_IhZuFEJtiuSPo5iKm5GWx5og=s512"),
        new Entry("쇄도", "PiercingDashSkillData",
            "https://docs.google.com/docs-images-rt/ALKuztZxu9T_o-amODb60Mugm114kGY7ZSztJYWq5gts1YS5M2KvRFBGxDy7qENIks9iFEqYEwx5xyd_VE5cfyxXWww21FmwAXTgoAMkRO16NQg2HwCqLOF3ePeuFd3IoWPjzioPijO0YJZxj08XDRVdrbekCrFyi7gae8K5ohh7uQ=s512"),
        new Entry("삼연격", "TripleSlashData",
            "https://docs.google.com/docs-images-rt/ALKuztZW2FRwpMoqKPy7otViheiheJz1VYckQxXm3VUL6ut4kveuiaTSJyIsbRGf66haPhBboj16upjo2MLWcsDWJ_5QG_283xO4ee2UzBWZg7QBz3NSiQVz6UimMtIrpTfTmxMFzmtV7n4VYm7a9WEG3OT5VJtFQ6IFDm5Atrhcjg=s512"),
    };

    [MenuItem("Tools/Skills/문서에서 스킬 아이콘 가져오기")]
    public static void ImportIcons()
    {
        if (!EditorUtility.DisplayDialog("스킬 아이콘 가져오기",
                $"기획 문서의 스킬 아이콘 {Entries.Length}개를 내려받아\n" +
                IconFolder + " 에 저장하고 SkillData에 연결합니다.\n\n진행할까요?",
                "가져오기", "취소"))
        {
            return;
        }

        EnsureFolder();

        List<string> linked = new List<string>();
        List<string> failed = new List<string>();

        try
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                Entry entry = Entries[i];

                EditorUtility.DisplayProgressBar("스킬 아이콘 가져오기",
                    $"{entry.Label} ({i + 1}/{Entries.Length})",
                    (float)i / Entries.Length);

                string path = $"{IconFolder}/Skill_{entry.AssetName}.png";

                if (!DownloadIfMissing(entry, path, failed)) continue;
                if (!LinkToSkill(entry, path, failed)) continue;

                linked.Add($"{entry.Label} → {entry.AssetName}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report = linked.Count > 0
            ? "연결됨:\n  " + string.Join("\n  ", linked)
            : "연결된 것이 없습니다.";

        if (failed.Count > 0) report += "\n\n실패:\n  " + string.Join("\n  ", failed);

        report += "\n\n초토화(각성)는 문서에 아이콘이 없어 그대로 둡니다.";

        EditorUtility.DisplayDialog("완료!", report, "확인");
        Debug.Log("[SkillIconImporter] " + report);
    }

    private static bool DownloadIfMissing(Entry entry, string path, List<string> failed)
    {
        if (System.IO.File.Exists(path)) return true;

        using UnityWebRequest request = UnityWebRequest.Get(entry.Url);

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        // 에디터에서는 코루틴이 없으니 끝날 때까지 기다린다.
        while (!operation.isDone) System.Threading.Thread.Sleep(50);

        if (request.result != UnityWebRequest.Result.Success)
        {
            failed.Add($"{entry.Label}: 내려받기 실패 ({request.error})");
            return false;
        }

        byte[] bytes = request.downloadHandler.data;

        if (bytes == null || bytes.Length < 100)
        {
            failed.Add($"{entry.Label}: 받은 파일이 비어 있습니다");
            return false;
        }

        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return true;
    }

    private static bool LinkToSkill(Entry entry, string path, List<string> failed)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            failed.Add($"{entry.Label}: Sprite로 읽지 못했습니다");
            return false;
        }

        SkillData skill = FindSkill(entry.AssetName);

        if (skill == null)
        {
            failed.Add($"{entry.Label}: {entry.AssetName} 에셋을 찾지 못했습니다");
            return false;
        }

        SerializedObject so = new SerializedObject(skill);
        SerializedProperty icon = so.FindProperty("icon");

        if (icon == null)
        {
            failed.Add($"{entry.Label}: icon 칸을 찾지 못했습니다");
            return false;
        }

        icon.objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skill);
        return true;
    }

    private static SkillData FindSkill(string assetName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:SkillData {assetName}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (System.IO.Path.GetFileNameWithoutExtension(path) != assetName) continue;

            return AssetDatabase.LoadAssetAtPath<SkillData>(path);
        }

        return null;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/UI")) AssetDatabase.CreateFolder("Assets/Sprites", "UI");
        if (!AssetDatabase.IsValidFolder(IconFolder)) AssetDatabase.CreateFolder("Assets/Sprites/UI", "SkillIcon");
    }
}
#endif
