using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 유물·스킬 장착은 제단 앞에서만 바꿀 수 있다 (기획).
/// 창은 어디서나 열어 볼 수 있고, 바꾸는 것만 막는다.
///
/// 제단 근처인지는 Shrine.IsPlayerInRange로만 판정한다.
/// Shrine은 관용님 파일이라 읽기만 하고 고치지 않는다.
///
/// 팀원이 제단 없는 씬에서 테스트할 수 있게 에디터에서만 끄는 스위치를 둔다.
/// 메뉴: Tools/UI/제단 제한 무시 (테스트용). 기본은 꺼져 있어서 제한이 걸린 상태다.
/// </summary>
public static class ShrineAccess
{
    public const string LockedMessage = "제단에서만 바꿀 수 있습니다";
    public const string ViewOnlyMessage = "보기만 가능합니다. 바꾸려면 제단에서 열어주세요";

    /// <summary>플레이어가 범위 안에 들어와 있는 제단. 없으면 null.</summary>
    public static Shrine NearbyShrine
    {
        get
        {
            foreach (Shrine shrine in Object.FindObjectsByType<Shrine>())
            {
                if (shrine != null && shrine.IsPlayerInRange) return shrine;
            }

            return null;
        }
    }

    public static bool CanEditLoadout => IgnoreInEditor || NearbyShrine != null;

    /// <summary>막혔을 때 보여줄 문구. 에디터에서는 우회 메뉴도 알려준다.</summary>
    public static string LockedHint
    {
        get
        {
#if UNITY_EDITOR
            return LockedMessage + " (테스트: " + MenuPath + ")";
#else
            return LockedMessage;
#endif
        }
    }

#if UNITY_EDITOR
    private const string IgnoreKey = "Ludens.UI.IgnoreShrineLock";
    private const string MenuPath = "Tools/UI/제단 제한 무시 (테스트용)";

    public static bool IgnoreInEditor => EditorPrefs.GetBool(IgnoreKey, false);

    [MenuItem(MenuPath)]
    private static void ToggleIgnore()
    {
        bool next = !IgnoreInEditor;
        EditorPrefs.SetBool(IgnoreKey, next);
        Debug.Log(next
            ? "[ShrineAccess] 제단 제한 무시: 켬. 어디서나 유물·스킬을 바꿀 수 있습니다."
            : "[ShrineAccess] 제단 제한 무시: 끔. 제단 앞에서만 바꿀 수 있습니다.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleIgnoreValidate()
    {
        Menu.SetChecked(MenuPath, IgnoreInEditor);
        return true;
    }
#else
    public static bool IgnoreInEditor => false;
#endif
}
