using System.Collections.Generic;
using UnityEngine;

public class PlayerRelicController : MonoBehaviour
{
    private Player player;

    //현재 장착된 유물
    private readonly List<RelicData> equippedRelics = new();

    //실제 실행 중인 유물 효과
    private readonly List<IRelicRuntime> activeRuntimes = new();

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    //유물 장착
    public void EquipRelic(RelicData relic)
    {
        if (player == null) return;

        if (relic == null) return;

        //같은 유물 중복 장착 방지
        if (equippedRelics.Contains(relic)) return;

        equippedRelics.Add(relic);

        foreach (RelicEffect effect in relic.Effects)
        {
            if (effect == null) continue;
            
            //주인 전달
            IRelicRuntime runtime = effect.CreateRuntime(player);

            if (runtime == null) continue;

            runtime.Equip();
            activeRuntimes.Add(runtime);
        }

        Debug.Log($"유물 장착: {relic.RelicName}");
    }

    //유물 해제
    private void OnDestroy()
    {
        foreach (IRelicRuntime runtime in activeRuntimes)
        {
            runtime.Unequip();
        }

        activeRuntimes.Clear();
    }
}