#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프로젝트 안의 RelicData를 전부 찾아 RelicDatabase 에셋을 채운다.
/// Unity 상단 메뉴 Tools/Relics > Rebuild Relic Database.
///
/// 유물을 새로 만들면 다시 한 번 눌러주면 된다.
/// </summary>
public static class RelicDatabaseBuilder
{
    private const string AssetPath = "Assets/Resources/RelicDatabase.asset";

    [MenuItem("Tools/Relics/Rebuild Relic Database")]
    public static void Rebuild()
    {
        List<RelicData> found = new List<RelicData>();
        HashSet<string> seenIds = new HashSet<string>();
        List<string> problems = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:RelicData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);

            if (relic == null) continue;

            if (string.IsNullOrEmpty(relic.RelicId))
            {
                problems.Add($"relicId 비어 있음: {path}");
                continue;
            }

            if (!seenIds.Add(relic.RelicId))
            {
                problems.Add($"relicId 중복 ({relic.RelicId}): {path}");
                continue;
            }

            found.Add(relic);
        }

        found.Sort((a, b) =>
        {
            int byCategory = a.Category.CompareTo(b.Category);
            return byCategory != 0 ? byCategory : string.CompareOrdinal(a.RelicId, b.RelicId);
        });

        RelicDatabase db = AssetDatabase.LoadAssetAtPath<RelicDatabase>(AssetPath);

        if (db == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            db = ScriptableObject.CreateInstance<RelicDatabase>();
            AssetDatabase.CreateAsset(db, AssetPath);
        }

        db.EditorSetRelics(found);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Selection.activeObject = db;

        string message = $"유물 {found.Count}개를 등록했습니다.\n{AssetPath}";

        if (problems.Count > 0)
        {
            message += "\n\n건너뛴 것:\n- " + string.Join("\n- ", problems);
            foreach (string p in problems) Debug.LogWarning("[RelicDatabase] " + p);
        }

        EditorUtility.DisplayDialog("RelicDatabase 갱신 완료", message, "확인");
    }
}
#endif
