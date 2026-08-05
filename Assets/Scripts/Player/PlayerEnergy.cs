
using System.Diagnostics;
using UnityEngine;

//플레이어 기력 관리 스크립트
public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private float currentEnergy = 0f;
    [SerializeField] private float maxEnergy = 100f;

    [Header("Orb")]
    [SerializeField] private EnergyOrb orbPrefab;

    //테스트 중에는 true
    [SerializeField] private bool startWithOrb = true;

    //실제 게임 중 생성된 보주 인스턴스
    private EnergyOrb orb;

    //기력 획득 차단 여부
    private bool isEnergyGainBlocked;

    //보주 보유 여부
    private bool hasOrb;


    //외부 접근용 프로퍼티
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    public float EnergyRatio => maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;

    public bool IsFull => currentEnergy >= maxEnergy;
    public bool HasOrb => hasOrb;
    public bool IsEnergyGainBlocked => isEnergyGainBlocked;

    private void Start()
    {
        if (startWithOrb)
        {
            AcquireOrb();
        }
    }


    //보주 획득
    public void AcquireOrb()
    {
        if (hasOrb) return;
        if (orbPrefab == null) return;
        

        hasOrb = true;

        //부모 없이 독립 오브젝트로 생성
        orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);

        //플레이어를 추적 대상으로 등록
        orb.Initialize(transform);

        //현재 기력 즉시 반영
        UpdateEnergyVisual();
    }


    //에너지 획득
    public void GainEnergy(float amount)
    {
        if (!hasOrb) return;
        
        //그믐 장착 중에는 기력을 얻을 수 없음
        if (isEnergyGainBlocked)
        {
            currentEnergy = 0f;
            UpdateEnergyVisual();
            return;
        }

        if (amount <= 0f) return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);

        UpdateEnergyVisual();
    }


    //특정 기력 이상 보유했는지 확인
    public bool HasEnergy(float requiredEnergy)
    {
        return currentEnergy >= requiredEnergy;
    }


    //최대 기력 직접 설정
    public void SetMaxEnergy(float newMaxEnergy)
    {
        maxEnergy = Mathf.Max(0f, newMaxEnergy);

        //최대 기력이 줄었을 때 현재 기력도 함께 제한
        currentEnergy = Mathf.Clamp(
            currentEnergy,
            0f,
            maxEnergy
        );

        UpdateEnergyVisual();

    }


    //최대 기력 증감
    public void ModifyMaxEnergy(float amount)
    {
        SetMaxEnergy(maxEnergy + amount);
    }


    //에너지 초기화
    public void ResetEnergy()
    {
        currentEnergy = 0f;
        UpdateEnergyVisual();
    }


    //비주얼 업데이트
    private void UpdateEnergyVisual()
    {
        if (orb == null || !hasOrb) return;

        //현재 EnergyOrb 코드와 호환되도록 비율 전달
        orb.SetEnergy(currentEnergy);
    }

    //기력 획득 차단 여부 설정
    public void SetEnergyGainBlocked(bool blocked)
    {
        isEnergyGainBlocked = blocked;

        //기력 획득을 막을 때 현재 기력도 즉시 초기화
        if (isEnergyGainBlocked)
        {
            ResetEnergy();
        }

    }


}