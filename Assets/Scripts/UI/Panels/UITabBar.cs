using UnityEngine;

/// <summary>
/// 유물창과 스킬창 위쪽의 탭 줄. 버튼이 눌리면 UIManager에 알린다.
///
/// 버튼이 UIManager를 직접 가리킬 수는 없다. UIManager는 UI_Root 프리팹에 있고
/// 이 탭 줄은 Popup_Canvas 프리팹에 있어서, 프리팹이 다르면 참조가 저장되지 않는다.
/// (예전에 조작법 버튼이 안 눌리던 것이 같은 이유였다.)
/// 그래서 같은 프리팹 안에 있는 이 컴포넌트가 중계한다.
/// </summary>
public class UITabBar : MonoBehaviour
{
    /// <summary>탭 번호. UIManager.ShowWindowTab에 그대로 넘긴다.</summary>
    public const int RelicTab = 0;
    public const int SkillTab = 1;

    public void OnRelicTab()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowWindowTab(RelicTab);
    }

    public void OnSkillTab()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowWindowTab(SkillTab);
    }
}
