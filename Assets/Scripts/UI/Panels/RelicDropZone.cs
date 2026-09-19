using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>드롭을 받는 영역의 종류.</summary>
public enum RelicDropTargetKind
{
    /// <summary>보관함. 장착 중인 유물을 여기에 놓으면 해제된다.</summary>
    Storage,

    /// <summary>검 장착 줄.</summary>
    SwordRow,

    /// <summary>보주 장착 줄.</summary>
    OrbRow,

    /// <summary>신체 장착 줄.</summary>
    BodyRow
}

/// <summary>
/// 칸과 칸 사이의 빈 공간도 드롭을 받게 해주는 판.
///
/// 칸에 정확히 올려놓아야만 반응하면 쓰기가 답답하다. 줄(또는 보관함) 어디에
/// 놓아도 되게 컨테이너에 이걸 붙인다. 칸 위에 놓으면 RelicSlotView가 먼저
/// 처리하고, 빈 공간이면 이벤트가 부모인 여기로 올라온다.
///
/// 투명하더라도 레이캐스트를 받으려면 alpha가 0이 아닌 Image가 필요하다.
/// 빌더가 alpha 1/255짜리 Image를 같이 붙인다.
/// </summary>
public class RelicDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private RelicDropTargetKind kind = RelicDropTargetKind.Storage;

    public RelicDropTargetKind Kind => kind;

    /// <summary>검/보주/신체 줄이면 그 카테고리. 보관함이면 의미 없음.</summary>
    public RelicCategory Category
    {
        get
        {
            return kind switch
            {
                RelicDropTargetKind.SwordRow => RelicCategory.Sword,
                RelicDropTargetKind.OrbRow => RelicCategory.Orb,
                _ => RelicCategory.Body
            };
        }
    }

    public bool IsStorage => kind == RelicDropTargetKind.Storage;

    public void OnDrop(PointerEventData eventData)
    {
        // 인터페이스 변수에 담고 나면 Unity의 == null 오버로드가 안 먹으므로
        // 먼저 구체 타입으로 받아서 검사한다.
        RelicInventoryPanel panel = GetComponentInParent<RelicInventoryPanel>();
        if (panel == null) return;

        panel.OnZoneDrop(this, eventData);
    }
}
