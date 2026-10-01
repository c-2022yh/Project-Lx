#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스킬 창을 손으로 확인하기 위한 에디터 전용 도구.
///
/// 이 게임에서 일반 스킬(A/S/D/F)을 주는 유물은 그림자 하나뿐이고,
/// PlayerSkill.GrantSkill이 받는 즉시 빈 칸에 자동 장착한다.
/// 그래서 실제 플레이로는 "보유했지만 장착 안 된 스킬"이 만들어지지 않아
/// 보유 목록이 늘 비어 있고, 드래그·자리 바꾸기·해제를 확인할 수가 없다.
/// Play 중에 메뉴 한 번으로 채운다.
///
/// 【X 스킬은 일부러 건너뛴다】
/// 검 유물은 SetExclusiveSkill로 X칸을 채우는데, 그 메서드는 해당 스킬이
/// 이미 보유 목록에 있으면 false를 돌려준다. 그리고 DevourRelicEffect와
/// ResidualMoonRelicEffect는 그 false에 예외를 던진다. 즉 여기서 X 스킬까지
/// 지급해버리면 나중에 포식·잔월을 장착할 때 게임이 터진다.
/// 유물 효과 에셋을 훑어서 X·각성용으로 예약된 스킬을 찾아 제외한다.
///
/// #if UNITY_EDITOR 안에 있어서 빌드에는 들어가지 않는다.
/// </summary>
public static class SkillDebugTools
{
    /// <summary>지급권의 주인. 회수할 때 같은 것을 넘겨야 해서 static으로 붙들어 둔다.</summary>
    private static readonly object DebugSource = new object();

    [MenuItem("Tools/Skills/Debug - 테스트 스킬 지급")]
    public static void GrantTestSkills()
    {
        if (!TryGetPlayerSkill(out PlayerSkill playerSkill)) return;

        HashSet<SkillData> reserved = CollectRelicReservedSkills();

        List<string> granted = new List<string>();
        List<string> skipped = new List<string>();

        foreach (SkillData skill in LoadAllSkills())
        {
            if (reserved.Contains(skill))
            {
                skipped.Add(skill.name);
                continue;
            }

            if (playerSkill.GrantSkill(skill, DebugSource)) granted.Add(skill.name);
        }

        RefreshOpenPanel();

        if (granted.Count == 0)
        {
            Debug.LogWarning("[SkillDebugTools] 지급한 스킬이 없습니다. " +
                             "이미 지급했거나, 스킬 사용 중이라 거절됐을 수 있습니다.");
            return;
        }

        Debug.Log($"[SkillDebugTools] 스킬 {granted.Count}개 지급: {string.Join(", ", granted)}\n" +
                  $"X·각성 전용이라 건너뛴 것: {(skipped.Count == 0 ? "없음" : string.Join(", ", skipped))}\n" +
                  "앞의 네 개는 A/S/D/F에 자동으로 들어가고 나머지가 보유 목록에 뜹니다. K를 눌러보세요.");
    }

    [MenuItem("Tools/Skills/Debug - 테스트 스킬 회수")]
    public static void RevokeTestSkills()
    {
        if (!TryGetPlayerSkill(out PlayerSkill playerSkill)) return;

        int count = 0;

        foreach (SkillData skill in LoadAllSkills())
        {
            if (playerSkill.RevokeSkill(skill, DebugSource)) count++;
        }

        RefreshOpenPanel();
        Debug.Log($"[SkillDebugTools] 테스트로 지급했던 스킬 {count}개를 회수했습니다. " +
                  "유물이 준 스킬은 그대로 남습니다.");
    }

    private static bool TryGetPlayerSkill(out PlayerSkill playerSkill)
    {
        playerSkill = null;

        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Play 중에만 됩니다",
                "스킬 보유 상태는 실행 중에만 존재합니다.\nPlay 버튼을 누른 뒤 다시 실행해주세요.", "확인");
            return false;
        }

        playerSkill = Object.FindAnyObjectByType<PlayerSkill>();

        if (playerSkill == null)
        {
            EditorUtility.DisplayDialog("플레이어 없음",
                "씬에서 PlayerSkill을 찾지 못했습니다.\n플레이어가 있는 씬인지 확인해주세요.", "확인");
            return false;
        }

        return true;
    }

    private static List<SkillData> LoadAllSkills()
    {
        List<SkillData> result = new List<SkillData>();
        string[] guids = AssetDatabase.FindAssets("t:SkillData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            if (skill != null) result.Add(skill);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    /// <summary>
    /// 유물 효과 에셋 중 X칸(SetExclusiveSkill)이나 각성(RegisterAwakeningSkill)에
    /// 스킬을 쓰는 것을 찾아, 그 효과가 들고 있는 SkillData를 모은다.
    /// 스크립트 본문을 보고 판단하므로 나중에 그런 유물이 추가돼도 따라간다.
    /// </summary>
    private static HashSet<SkillData> CollectRelicReservedSkills()
    {
        HashSet<SkillData> reserved = new HashSet<SkillData>();
        string[] guids = AssetDatabase.FindAssets("t:RelicEffect");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RelicEffect effect = AssetDatabase.LoadAssetAtPath<RelicEffect>(path);

            if (effect == null) continue;

            MonoScript script = MonoScript.FromScriptableObject(effect);

            if (script == null) continue;

            string body = script.text;

            if (body == null) continue;
            if (!body.Contains("SetExclusiveSkill") && !body.Contains("RegisterAwakeningSkill")) continue;

            SerializedObject so = new SerializedObject(effect);
            SerializedProperty p = so.GetIterator();

            while (p.NextVisible(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (p.objectReferenceValue is SkillData skill) reserved.Add(skill);
            }
        }

        return reserved;
    }

    /// <summary>열려 있는 스킬 창이 있으면 즉시 다시 그린다.</summary>
    private static void RefreshOpenPanel()
    {
        SkillPanel panel = Object.FindAnyObjectByType<SkillPanel>(FindObjectsInactive.Include);

        if (panel != null && panel.gameObject.activeInHierarchy) panel.Refresh();
    }
}
#endif
