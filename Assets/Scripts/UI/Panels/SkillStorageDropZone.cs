using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 보유 스킬 격자의 빈 곳에 놓아도 해제되도록 받아주는 판.
/// 칸과 칸 사이, 또는 목록이 비어 있을 때를 위한 것이다.
///
/// 칸 위에 놓으면 그 칸이 먼저 받고 여기까지 올라오지 않는다.
/// </summary>
public class SkillStorageDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 인터페이스 변수에 담고 나면 Unity의 == null 오버로드가 안 먹으므로
        // 먼저 구체 타입으로 받아서 검사한다.
        SkillPanel panel = GetComponentInParent<SkillPanel>();
        if (panel == null) return;

        panel.OnStorageDrop(eventData);
    }
}
