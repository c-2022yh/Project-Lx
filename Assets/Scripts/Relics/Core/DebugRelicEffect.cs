
using UnityEngine;

[CreateAssetMenu(
    fileName = "RFX_Debug",
    menuName = "Relics/Effects/Debug Effect")]
public class DebugRelicEffect : RelicEffect
{
    [SerializeField] private string message = "테스트 유물";

    public override IRelicRuntime CreateRuntime(Player p)
    {
        return new DebugRelicRuntime(p, message);
    }

    private class DebugRelicRuntime : IRelicRuntime
    {
        private readonly Player player;
        private readonly string message;

        public DebugRelicRuntime(Player player, string message)
        {
            this.player = player;
            this.message = message;
        }

        public void Equip()
        {
            Debug.Log(
                $"[Relic Equip] {message} / Player: {player.name}");
        }

        public void Unequip()
        {
            Debug.Log(
                $"[Relic Unequip] {message} / Player: {player.name}");
        }
    }
}