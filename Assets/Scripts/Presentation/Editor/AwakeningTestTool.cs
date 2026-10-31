using UnityEditor;
using UnityEngine;

/// <summary>
/// 각성 연출 시험용. Play 중에 메뉴를 누르면 보주를 주고 기력을 가득 채운다. 그다음 Q로 각성.
/// 메뉴: Tools/Test/각성 준비 (보주 + 기력 가득)
/// </summary>
public static class AwakeningTestTool
{
    private const string MenuPath = "Tools/Test/각성 준비 (보주 + 기력 가득)";

    [MenuItem(MenuPath)]
    private static void Prepare()
    {
        Player p = Object.FindAnyObjectByType<Player>();
        PlayerEnergy energy = p != null ? p.GetComponent<PlayerEnergy>() : null;
        if (energy == null)
        {
            Debug.LogWarning("[각성 시험] 플레이어(PlayerEnergy)를 못 찾았습니다.");
            return;
        }

        if (!energy.HasOrb) energy.AcquireOrb();
        energy.GainEnergy(energy.MaxEnergy);
        Debug.Log($"[각성 시험] 보주 {(energy.HasOrb ? "있음" : "없음")}, 기력 {energy.CurrentEnergy}/{energy.MaxEnergy}. Q를 누르면 각성합니다.");
    }

    [MenuItem(MenuPath, true)]
    private static bool PrepareValidate() => Application.isPlaying;
}
