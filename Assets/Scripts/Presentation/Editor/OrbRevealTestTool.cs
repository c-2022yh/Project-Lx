using UnityEditor;
using UnityEngine;

/// <summary>
/// 보스를 잡지 않고 "보주 등장" 연출만 바로 보는 시험 메뉴 (Play 중에만).
///   격파 빛살 → 보주가 빛나며 나타나 둥실 떠 있음 → 2초 뒤 플레이어에게 날아가 스며듦
/// 보이기만 하는 연출이라 보주 개수·기력은 바뀌지 않는다.
/// </summary>
public static class OrbRevealTestTool
{
    private const string MenuPath = "Tools/Test/보주 등장 연출 보기";

    [MenuItem(MenuPath)]
    private static void Play()
    {
        Player p = Object.FindAnyObjectByType<Player>();
        if (p == null) { Debug.LogWarning("[보주 등장] 플레이어를 못 찾았습니다."); return; }
        float dir = p.sr != null && p.sr.flipX ? -1f : 1f;
        Vector3 at = p.transform.position + new Vector3(4f * dir, 2.5f, 0f);
        p.StartCoroutine(Run(at, p.transform));
    }

    private static System.Collections.IEnumerator Run(Vector3 at, Transform player)
    {
        BossFx.DefeatBurst(at);
        yield return new WaitForSecondsRealtime(1.2f);
        BossOrbReveal orb = BossOrbReveal.Play(at, player, null);
        yield return new WaitForSecondsRealtime(2f);
        if (orb != null) orb.FlyToPlayer();
        Debug.Log("[보주 등장] 연출을 보여줬습니다 (보주 개수는 그대로).");
    }

    [MenuItem(MenuPath, true)]
    private static bool Validate() => Application.isPlaying;
}
