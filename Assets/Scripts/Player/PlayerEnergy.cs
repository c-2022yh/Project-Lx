using System;
using UnityEngine;

//플레이어의 기력과 보주 표시를 관리하는 스크립트
public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField, Min(0f)] private float currentEnergy;
    [SerializeField, Min(0f)] private float maxEnergy = 100f;

    [Header("Orb")]
    [SerializeField] private EnergyOrb orbPrefab;
    [SerializeField] private bool startWithOrb = true;

    private EnergyOrb orb;
    private bool hasOrb;
    private bool isEnergyGainBlocked;
    private float energyGainMultiplier = 1f;

    //현재 기력 또는 최대 기력이 바뀌면 유물 효과에 알림
    public event Action OnEnergyChanged;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyRatio => maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;
    public bool IsFull => maxEnergy > 0f && currentEnergy >= maxEnergy;
    public bool HasOrb => hasOrb;
    public bool IsEnergyGainBlocked => isEnergyGainBlocked;

    //테스트용 시작 보주가 설정되어 있으면 생성
    private void Start()
    {
        if (startWithOrb) AcquireOrb();
    }

    //보주를 한 번만 생성하고 현재 기력에 맞춰 외형을 갱신
    public void AcquireOrb()
    {
        if (hasOrb || orbPrefab == null) return;
        hasOrb = true;
        orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
        orb.Initialize(transform);

        NotifyEnergyChanged();
    }

    //획득 배율을 적용한 뒤 기력을 최대치 안에서 증가
    public void GainEnergy(float amount)
    {
        if (!hasOrb || isEnergyGainBlocked || amount <= 0f) return;
        float next = Mathf.Clamp(currentEnergy + amount * Mathf.Max(0f, energyGainMultiplier), 0f, maxEnergy);
        if (Mathf.Approximately(currentEnergy, next)) return;
        currentEnergy = next;

        NotifyEnergyChanged();
    }

    //유물이 장착되거나 해제될 때 획득 배율의 증감분을 반영
    //예: 50% 추가 획득은 +0.5, 해제할 때는 -0.5.
    public void ModifyEnergyGainMultiplier(float bonus)
    {
        energyGainMultiplier = Mathf.Max(0f, energyGainMultiplier + bonus);
    }

    //현재 기력이 각성이나 스킬에 필요한 양 이상인지 확인
    public bool HasEnergy(float requiredEnergy) => currentEnergy >= requiredEnergy;

    //최대 기력을 바꾸고 현재 기력이 새 최대치를 넘으면 조정
    public void SetMaxEnergy(float newMaxEnergy)
    {
        maxEnergy = Mathf.Max(0f, newMaxEnergy);
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        NotifyEnergyChanged();
    }

    //최대 기력을 유물의 보너스 연산
    public void ModifyMaxEnergy(float amount) => SetMaxEnergy(maxEnergy + amount);

    //각성이 끝난 뒤 기력을 비우고 유물에 변경 사실을 알림
    public void ResetEnergy()
    {
        if (currentEnergy <= 0f) return;
        currentEnergy = 0f;

        NotifyEnergyChanged();
    }

    //기력 획득을 막는다. 기존 그믐 데이터를 사용하는 다른 코드의 호환성을 유지
    public void SetEnergyGainBlocked(bool blocked)
    {
        isEnergyGainBlocked = blocked;

        if (blocked) ResetEnergy();
    }

    //보주를 보유하고 있고 기력이 충분한지 확인
    public bool CanSpendEnergy(float amount)
    {
        if (amount <= 0f) return true;

        return hasOrb && currentEnergy >= amount;
    }

    //기력을 실제로 소모하고 성공 여부를 돌려줌
    public bool TrySpendEnergy(float amount)
    {
        if (!CanSpendEnergy(amount)) return false;
        if (amount <= 0f) return true;

        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, maxEnergy);

        NotifyEnergyChanged();

        return true;
    }

    //기력 UI/보주와 만월의 충전 판정에 변경 사실을 동시에 전달
    private void NotifyEnergyChanged()
    {
        if (hasOrb && orb != null) orb.SetEnergy(currentEnergy);

        OnEnergyChanged?.Invoke();
    }
}
