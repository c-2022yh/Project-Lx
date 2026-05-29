using UnityEngine;

public class PlayerEnergy : MonoBehaviour
{


    //기력 세팅
    [Header("Energy")]
    public float currentEnergy = 0f;
    public float maxEnergy = 100f;
    public bool IsEnergyFull => currentEnergy >= maxEnergy;






}
