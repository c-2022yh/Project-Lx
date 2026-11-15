using UnityEngine;

/// <summary>
/// 이 적을 보스로 취급한다. 보스를 때리면 히트스탑·흔들림이 더 세지고,
/// BossRoom이 이름 카드와 HP바에 이 정보를 쓴다.
///
/// 보스(또는 테스트용 대역) 오브젝트에 EnemyHealth와 같이 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class BossMarker : MonoBehaviour
{
    [SerializeField] private string bossName = "보스";
    [Tooltip("이름 위에 작게 나오는 칭호. 비워 두면 안 나온다.")]
    [SerializeField] private string bossTitle = "";

    public string BossName => bossName;
    public string BossTitle => bossTitle;
}
