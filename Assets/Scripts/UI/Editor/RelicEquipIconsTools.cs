#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/UI/유물 장착 아이콘 다시 연결
/// 유물 아이콘 파일 이름 + "_Equip" 인 그림을 Relic_Equip_Icons 폴더에서 찾아 RelicEquipIcons 표를 다시 채운다.
/// </summary>
public static class RelicEquipIconsTools
{
    private const string EquipFolder = "Assets/Sprites/Relic_Equip_Icons";
    private const string LibraryPath = "Assets/Resources/" + RelicEquipIcons.ResourceName + ".asset";

    [MenuItem("Tools/UI/유물 장착 아이콘 다시 연결")]
    private static void Relink()
    {
        // 이름 → 장착용 스프라이트
        Dictionary<string, Sprite> equipByBase = new Dictionary<string, Sprite>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EquipFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);
            if (!file.EndsWith("_Equip")) continue;

            Sprite sprite = FirstSprite(path);
            if (sprite != null) equipByBase[file.Substring(0, file.Length - "_Equip".Length)] = sprite;
        }

        List<RelicEquipIcons.Entry> entries = new List<RelicEquipIcons.Entry>();
        List<string> missing = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:RelicData"))
        {
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(AssetDatabase.GUIDToAssetPath(guid));
            if (relic == null || relic.Icon == null || string.IsNullOrEmpty(relic.RelicId)) continue;

            string iconFile = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(relic.Icon));

            if (equipByBase.TryGetValue(iconFile, out Sprite equip))
                entries.Add(new RelicEquipIcons.Entry { relicId = relic.RelicId, sprite = equip });
            else
                missing.Add(relic.RelicName);
        }

        RelicEquipIcons library = AssetDatabase.LoadAssetAtPath<RelicEquipIcons>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<RelicEquipIcons>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.SetEntries(entries);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Debug.Log($"[RelicEquipIcons] {entries.Count}개 연결됨." +
                  (missing.Count > 0 ? " 장착용 그림이 없는 유물: " + string.Join(", ", missing) : ""));
    }

    private static Sprite FirstSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite) return sprite;
        }
        return null;
    }
}
#endif
