using UnityEngine;

/// <summary>
/// 튜토리얼 슬라임이 보는 방향을 정한다 (-1 = 왼쪽, 1 = 오른쪽).
/// 적 코드(EnemyAnimation)는 고치지 않고, 실행 중에만 SetDirection으로 방향을 덮어쓴다.
/// 붙이기·바꾸기: Tools/Map/튜토리얼 슬라임 방향 바꾸기 (누를 때마다 왼쪽 ↔ 오른쪽) → Ctrl+S
/// 인스펙터에서 Direction 값을 직접 바꿔도 된다.
/// 슬라임에는 AI가 없어서 다른 곳에서 방향을 바꾸지 않는다.
/// </summary>
public class TutorialSlimeFacing : MonoBehaviour
{
    [Tooltip("-1 = 왼쪽, 1 = 오른쪽")]
    public int direction = -1;

    private EnemyAnimation anim;

    private void Awake() => anim = GetComponent<EnemyAnimation>();

    // EnemyAnimation이 켜질 때 방향을 되돌리므로 매 프레임 끝에 다시 맞춘다 (3마리라 부담 없음)
    private void LateUpdate()
    {
        if (anim != null) anim.SetDirection(direction >= 0 ? 1 : -1);
    }

    private void OnValidate()
    {
        // 씬 화면에서도 바로 보이게 (에디터에서 값을 바꿨을 때)
        EnemyAnimation a = GetComponent<EnemyAnimation>();
        if (a != null) a.SetDirection(direction >= 0 ? 1 : -1);
    }
}
