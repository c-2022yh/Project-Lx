using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRelicManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Starting Owned Relics - Test")]
    [SerializeField] private List<RelicData> startingOwnedRelics = new();

    [Header("Starting Relics - Test")]
    [SerializeField] private List<RelicData> startingRelics = new();

    private const int MaxBodyCostValue = 5;
    private const int MaxBodyCountValue = 5;

    private readonly List<RelicData> ownedRelics = new();
    private readonly List<RelicData> equippedRelics = new();
    private readonly Dictionary<RelicData, List<IRelicRuntime>> runtimesByRelic = new();

    private PlayerSkill playerSkill;
    private bool isChangingRelic;
    private bool isInitializing;
    private bool isDestroying;

    public IReadOnlyList<RelicData> OwnedRelics => ownedRelics.AsReadOnly();
    public IReadOnlyList<RelicData> EquippedRelics => equippedRelics.AsReadOnly();
    public RelicData EquippedSwordRelic => GetEquippedRelic(RelicCategory.Sword);
    public RelicData EquippedOrbRelic => GetEquippedRelic(RelicCategory.Orb);
    public int MaxBodyCost => MaxBodyCostValue;
    public int MaxBodyCount => MaxBodyCountValue;
    public int CurrentBodyCost => GetBodyCost();
    public int CurrentBodyCount => GetBodyCount();

    public event Action OnRelicsChanged;

    //초기 데이터와 필요한 컴포넌트 연결
    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (player != null) playerSkill = player.GetComponent<PlayerSkill>();
        else Debug.LogError("[PlayerRelicManager] Player를 찾을 수 없습니다.", this);
    }

    //시작 보유 목록을 등록하고 시작 장착 목록에 동일한 장착 규칙 적용
    private void Start()
    {
        isInitializing = true;
        foreach (RelicData relic in startingOwnedRelics) AcquireRelic(relic);
        foreach (RelicData relic in startingRelics)
        {
            if (relic == null) continue;
            AcquireRelic(relic);
            EquipRelic(relic);
        }
        isInitializing = false;
        NotifyRelicsChanged();
    }

    //유물을 보유 목록에 추가. 중복 획득은 false이며 자동 장착하지 않음
    public bool AcquireRelic(RelicData relic)
    {
        if (relic == null || isChangingRelic || isDestroying || HasRelic(relic)) return false;
        ownedRelics.Add(relic);
        NotifyRelicsChanged();
        return true;
    }

    //유물 보유 여부 확인
    public bool HasRelic(RelicData relic) => relic != null && ownedRelics.Contains(relic);

    //유물 장착 여부 확인
    public bool IsRelicEquipped(RelicData relic) => relic != null && equippedRelics.Contains(relic);

    //해당 종류의 첫 장착 유물 조회. 신체 전체는 EquippedRelics에서 조회
    public RelicData GetEquippedRelic(RelicCategory category)
    {
        foreach (RelicData relic in equippedRelics)
        {
            if (relic != null && relic.Category == category) return relic;
        }
        return null;
    }

    //현재 신체 유물의 총 코스트 계산
    private int GetBodyCost()
    {
        int cost = 0;
        foreach (RelicData relic in equippedRelics)
        {
            if (relic != null && relic.Category == RelicCategory.Body) cost += Mathf.Max(0, relic.Cost);
        }
        return cost;
    }

    //현재 장착한 신체 유물 개수 계산
    private int GetBodyCount()
    {
        int count = 0;
        foreach (RelicData relic in equippedRelics)
        {
            if (relic != null && relic.Category == RelicCategory.Body) count++;
        }
        return count;
    }

    //장착 조건과 신체 최대 5코스트·5개 제한을 검사하고 실패 이유 반환
    public bool CanEquipRelic(RelicData relic, out string reason)
    {
        reason = null;
        if (relic == null) reason = "유물이 없습니다.";
        else if (player == null) reason = "Player가 없습니다.";
        else if (isChangingRelic || isDestroying) reason = "유물 변경 처리 중입니다.";
        else if (!HasRelic(relic)) reason = "보유하지 않은 유물입니다.";
        else if (IsRelicEquipped(relic)) reason = "이미 장착한 유물입니다.";
        else if (!CanChangeEquipment()) reason = "행동을 마친 뒤 유물을 변경할 수 있습니다.";
        else if (relic.Category == RelicCategory.Body)
        {
            if (relic.Cost < 0) reason = "유물 코스트는 음수일 수 없습니다.";
            else if (CurrentBodyCount >= MaxBodyCount) reason = "신체 유물은 최대 5개까지 장착할 수 있습니다.";
            else if (relic.Cost > MaxBodyCost - CurrentBodyCost) reason = "신체 유물의 최대 코스트는 5입니다.";
        }
        else if (relic.Category != RelicCategory.Sword && relic.Category != RelicCategory.Orb)
        {
            reason = "지원하지 않는 유물 종류입니다.";
        }
        return reason == null;
    }

    //제단 조건 없이 스킬·공격·대쉬 등 현재 행동을 마쳤는지 확인
    private bool CanChangeEquipment()
    {
        if (player == null) return false;
        if (playerSkill != null && playerSkill.IsUsingSkill) return false;
        return player.ActionState == null || player.ActionState.isNormal;
    }

    //유물 장착. 검·보주는 기존 유물과 교체하며 새 효과 적용 실패 시 이전 장착 복구 시도
    public bool EquipRelic(RelicData relic)
    {
        if (!CanEquipRelic(relic, out string reason))
        {
            Debug.LogWarning($"[PlayerRelicManager] {reason}");
            return false;
        }

        List<IRelicRuntime> newRuntimes;
        try
        {
            newRuntimes = CreateRuntimes(relic);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
        if (newRuntimes.Count == 0) return false;

        RelicData previousRelic = relic.Category == RelicCategory.Body ? null : GetEquippedRelic(relic.Category);
        List<IRelicRuntime> previousRuntimes = previousRelic != null ? runtimesByRelic[previousRelic] : null;
        SkillData[] previousSlots = playerSkill != null ? playerSkill.CaptureSlots() : null;
        bool success = false;
        bool previousReleased = false;
        isChangingRelic = true;
        if (playerSkill != null) playerSkill.BeginEquipmentChange();

        try
        {
            if (previousRelic != null)
            {
                bool released = ReleaseRuntimes(previousRuntimes);
                runtimesByRelic.Remove(previousRelic);
                equippedRelics.Remove(previousRelic);
                previousReleased = true;
                if (!released) return false;
            }

            if (!ActivateRuntimes(newRuntimes))
            {
                if (previousReleased && ActivateRuntimes(previousRuntimes))
                {
                    runtimesByRelic.Add(previousRelic, previousRuntimes);
                    equippedRelics.Add(previousRelic);
                    if (playerSkill != null) playerSkill.RestoreSlots(previousSlots);
                }
                return false;
            }

            runtimesByRelic.Add(relic, newRuntimes);
            equippedRelics.Add(relic);
            success = true;
        }
        finally
        {
            try
            {
                if (playerSkill != null) playerSkill.EndEquipmentChange();
            }
            finally
            {
                isChangingRelic = false;
                NotifyRelicsChanged();
            }
        }
        return success;
    }

    //유물 Effect로부터 플레이어에게 적용할 Runtime 목록 생성
    private List<IRelicRuntime> CreateRuntimes(RelicData relic)
    {
        List<IRelicRuntime> runtimes = new();
        foreach (RelicEffect effect in relic.Effects)
        {
            if (effect == null) continue;
            IRelicRuntime runtime = effect.CreateRuntime(player);
            if (runtime != null) runtimes.Add(runtime);
        }
        return runtimes;
    }

    //효과를 적용하고 예외가 발생하면 적용을 시작한 효과를 정리
    private bool ActivateRuntimes(List<IRelicRuntime> runtimes)
    {
        List<IRelicRuntime> startedRuntimes = new();
        try
        {
            foreach (IRelicRuntime runtime in runtimes)
            {
                startedRuntimes.Add(runtime);
                runtime.Equip();
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            ReleaseRuntimes(startedRuntimes);
            return false;
        }
    }

    //보유는 유지하고 유물 효과와 지급한 스킬만 회수
    public bool UnequipRelic(RelicData relic)
    {
        if (relic == null || isChangingRelic || isDestroying || !CanChangeEquipment()) return false;
        if (!runtimesByRelic.TryGetValue(relic, out List<IRelicRuntime> runtimes)) return false;
        isChangingRelic = true;
        if (playerSkill != null) playerSkill.BeginEquipmentChange();
        bool success;
        try
        {
            success = ReleaseRuntimes(runtimes);
            runtimesByRelic.Remove(relic);
            equippedRelics.Remove(relic);
        }
        finally
        {
            try
            {
                if (playerSkill != null) playerSkill.EndEquipmentChange();
            }
            finally
            {
                isChangingRelic = false;
                NotifyRelicsChanged();
            }
        }
        return success;
    }

    //효과를 역순으로 해제하고 각 Runtime의 지급권을 회수. 해제 예외는 로그로 알림
    private bool ReleaseRuntimes(List<IRelicRuntime> runtimes)
    {
        bool success = true;
        for (int i = runtimes.Count - 1; i >= 0; i--)
        {
            try
            {
                runtimes[i]?.Unequip();
            }
            catch (Exception exception)
            {
                success = false;
                Debug.LogException(exception, this);
            }
            finally
            {
                if (playerSkill != null) playerSkill.RevokeSource(runtimes[i]);
            }
        }
        return success;
    }

    //현재 장착된 모든 유물을 해제하고 보유는 유지
    public void UnequipAllRelics()
    {
        if (isChangingRelic || isDestroying || !CanChangeEquipment()) return;
        RelicData[] snapshot = equippedRelics.ToArray();
        foreach (RelicData relic in snapshot) UnequipRelic(relic);
    }

    //보유·장착 변경 완료를 UI에 알림
    private void NotifyRelicsChanged()
    {
        if (!isInitializing && !isDestroying) OnRelicsChanged?.Invoke();
    }

    //관리자 파괴 시 유물 효과와 이벤트 구독 정리
    private void OnDestroy()
    {
        isDestroying = true;
        foreach (List<IRelicRuntime> runtimes in runtimesByRelic.Values) ReleaseRuntimes(runtimes);
        runtimesByRelic.Clear();
        equippedRelics.Clear();
    }
}
