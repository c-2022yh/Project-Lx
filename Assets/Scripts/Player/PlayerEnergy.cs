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

    private bool hasOrb;

    //외부 접근용 프로퍼티
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyRatio => maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;

    public bool IsFull => currentEnergy >= maxEnergy;
    public bool HasOrb => hasOrb;




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
        hasOrb = true;

        //부모 없이 독립 오브젝트로 생성
        orb = Instantiate(orbPrefab,  transform.position, Quaternion.identity);

        //플레이어를 추적 대상으로 등록
        orb.Initialize(transform);

        //현재 기력 즉시 반영
        orb.SetEnergyRatio(EnergyRatio);
    }

   
    //에너지 획득
    public void GainEnergy(float amount)
    {
        if (!hasOrb) return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        UpdateEnergyVisual();
    }

    //에너지 초기화 (각성 등 사용)
    public void ResetEnergy()
    {
        currentEnergy = 0f;
        UpdateEnergyVisual();
    }

    //비주얼 업데이트
    private void UpdateEnergyVisual()
    {
        if (orb == null || !hasOrb) return;

        orb.SetEnergyRatio(EnergyRatio);
    }
}

