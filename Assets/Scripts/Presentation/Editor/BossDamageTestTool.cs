using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스 연출 확인용: Play 중에 보스(골렘·갈라진 조각)에게 최대 체력의 35% 피해를 준다.
/// 두 번 누르면 2페이즈(갈라짐), 몇 번 더 누르면 격파 연출까지 빠르게 볼 수 있다.
/// </summary>
public static class BossDamageTestTool
{
    private const string MenuPath = "Tools/Test/보스에게 큰 피해 (체력 35%)";

    [MenuItem(MenuPath)]
    private static void Hit()
    {
        Player p = Object.FindAnyObjectByType<Player>();
        int count = 0;
        foreach (EnemyHealth e in Object.FindObjectsByType<EnemyHealth>())
        {
            if (e == null || e.IsDead || !e.isActiveAndEnabled) continue;
            bool boss = e.GetComponent<GolemBossController>() != null || e.GetComponent<BossMarker>() != null
                        || e.name.Contains("Golem");
            if (!boss) continue;
            DamageInfo info = new DamageInfo(e.MaxHp * 0.35f, DamageType.Physical, 1f, false, 0f, p != null ? p.gameObject : null);
            e.TakeDamage(info, Vector2.right);
            count++;
            Debug.Log($"[보스 시험] {e.name}: 체력 {e.CurrentHp:0}/{e.MaxHp:0}");
        }
        if (count == 0) Debug.LogWarning("[보스 시험] 지금 싸울 수 있는 보스가 없습니다 (보스방에 들어가 보스가 나온 뒤 눌러 주세요).");
    }

    [MenuItem(MenuPath, true)]
    private static bool Validate() => Application.isPlaying;
}
