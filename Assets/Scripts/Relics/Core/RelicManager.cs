using System.Collections.Generic;
using UnityEngine;

//플레이어가 장착한 유물과 유물 효과를 관리
public class RelicManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Starting Relics - Test")]
    [SerializeField] private List<RelicData> startingRelics = new();

    //현재 장착된 유물
    private readonly List<RelicData> equippedRelics = new();

    //각 유물이 생성한 Runtime 목록
    private readonly Dictionary<RelicData, List<IRelicRuntime>> runtimesByRelic = new();

    public IReadOnlyList<RelicData> EquippedRelics => equippedRelics;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();

        if (player == null)
        {
            Debug.LogError("[RelicManager] Player를 찾을 수 없습니다.",this);
        }
    }

    private void Start()
    {
        Debug.Log($"[RelicManager] 시작 유물 개수: {startingRelics.Count}");

        // 실제 장착 UI가 만들어지기 전 테스트용
        foreach (RelicData relic in startingRelics)
        {
            if (relic == null)
            {
                Debug.LogWarning(
                    "[RelicManager] Starting Relics에 빈 슬롯이 있습니다.",
                    this);

                continue;
            }

            Debug.Log($"[RelicManager] 장착 시도: {relic.RelicName}");

            EquipRelic(relic);
        }
    }

    // 유물 장착
    public bool EquipRelic(RelicData relic)
    {
        if (relic == null)
            return false;

        if (player == null)
        {
            Debug.LogWarning(
                "[RelicManager] Player가 없어 유물을 장착할 수 없습니다.",
                this);

            return false;
        }

        // 동일 유물 중복 장착 방지
        if (equippedRelics.Contains(relic))
        {
            Debug.LogWarning(
                $"[RelicManager] 이미 장착된 유물: {relic.RelicName}",
                this);

            return false;
        }

        // 이 변수는 EquipRelic 함수 내부에서만 사용
        List<IRelicRuntime> createdRuntimes = new();

        // 유물에 등록된 Effect들을 Runtime으로 생성
        foreach (RelicEffect effect in relic.Effects)
        {
            if (effect == null)
                continue;

            IRelicRuntime runtime = effect.CreateRuntime(player);

            if (runtime == null)
            {
                Debug.LogWarning(
                    $"[RelicManager] {effect.name}이 Runtime을 생성하지 못했습니다.",
                    effect);

                continue;
            }

            createdRuntimes.Add(runtime);
        }

        if (createdRuntimes.Count == 0)
        {
            Debug.LogWarning(
                $"[RelicManager] {relic.RelicName}에 실행 가능한 효과가 없습니다.",
                relic);

            return false;
        }

        equippedRelics.Add(relic);
        runtimesByRelic.Add(relic, createdRuntimes);

        // 생성된 Runtime 활성화
        foreach (IRelicRuntime runtime in createdRuntimes)
        {
            Debug.Log($"[RelicManager] Runtime 장착: {runtime.GetType().Name}");

            runtime.Equip();
        }

        Debug.Log($"유물 장착 완료: {relic.RelicName}");

        return true;
    }

    // 유물 해제
    public bool UnequipRelic(RelicData relic)
    {
        if (relic == null)
            return false;

        if (!runtimesByRelic.TryGetValue(
                relic,
                out List<IRelicRuntime> runtimes))
        {
            return false;
        }

        foreach (IRelicRuntime runtime in runtimes)
        {
            if (runtime != null)
                runtime.Unequip();
        }

        runtimesByRelic.Remove(relic);
        equippedRelics.Remove(relic);

        Debug.Log($"유물 해제: {relic.RelicName}");

        return true;
    }

    // 모든 유물 해제
    public void UnequipAllRelics()
    {
        RelicData[] relics = equippedRelics.ToArray();

        foreach (RelicData relic in relics)
        {
            UnequipRelic(relic);
        }
    }

    private void OnDestroy()
    {
        UnequipAllRelics();
    }
}