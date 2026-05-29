using UnityEngine;

public class PlayerEnergy : MonoBehaviour
{


    //기력 세팅
    [Header("Energy")]
    public float currentEnergy = 0f;
    public float maxEnergy = 100f;
    public bool IsEnergyFull => currentEnergy >= maxEnergy;

    //보주
    [Header("References")]
    [SerializeField] private EnergyOrb orb;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyRatio => maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;
    public bool IsFull => currentEnergy >= maxEnergy;


    public void GainEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);//최대치 설정 =maxEnergy->100

        float ratio = currentEnergy / maxEnergy;

        if (orb != null)
        {
            orb.SetEnergyRatio(ratio);
        }
    }



}
