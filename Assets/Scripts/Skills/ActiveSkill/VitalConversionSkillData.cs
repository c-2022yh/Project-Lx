using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SKL_VitalConversion", menuName = "Skills/Utility/Vital Conversion")]

//회복
//기력을 소모하여 체력을 회복한다.
public class VitalConversionSkillData : UtilitySkillData
{
    [Header("Vital Conversion Settings")]
    [SerializeField, Min(0f)]
    private float energyCost = 30f;

    [SerializeField, Min(0f)]
    private float healAmount = 2f;


    //스킬 사용 가능 여부
    public override bool CanUse(Player p)
    {
        if (p == null) return false;

        PlayerEnergy playerEnergy = p.GetComponent<PlayerEnergy>();
        PlayerHealth playerHealth = p.GetComponent<PlayerHealth>();

        if (playerEnergy == null || playerHealth == null)
        {
            return false;
        }

        //사망 상태에서는 사용 불가능
        if (playerHealth.CurrentHealth <= 0f)
        {
            return false;
        }

        //체력이 가득 차 있으면 사용 불가능
        if (playerHealth.CurrentHealth >=
            playerHealth.MaxHealth)
        {
            return false;
        }

        //기력이 부족하면 사용 불가능
        if (!playerEnergy.CanSpendEnergy(energyCost))
        {
            return false;
        }

        return true;
    }


    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;

        PlayerEnergy playerEnergy = p.GetComponent<PlayerEnergy>();

        PlayerHealth playerHealth = p.GetComponent<PlayerHealth>();

        if (playerEnergy == null || playerHealth == null)
        {
            yield break;
        }


        //스킬 시전 시작
        //PlayerSkill에서 이미 EnterSkill() 상태로 진입했으므로
        //이 시간 동안 이동, 공격, 다른 스킬 사용이 제한된다.
        Debug.Log(
            $"[VitalConversion] 회복 시전 시작 - " +
            $"선딜 {activeTime}초"
        );

        //현재 수평 이동을 멈춰 무방비 상태를 명확하게 표현
        if (p.rb != null)
        {
            p.rb.linearVelocity = new Vector2(0f, p.rb.linearVelocity.y);
        }


        //선딜
        if (activeTime > 0f)
        {
            yield return new WaitForSeconds(activeTime);
        }


        //선딜 도중 사망했다면 회복하지 않음
        if (playerHealth.CurrentHealth <= 0f)
        {
            yield break;
        }


        //선딜이 끝난 시점에 기력 소비
        if (!playerEnergy.TrySpendEnergy(energyCost))
        {
            yield break;
        }


        //체력 회복
        playerHealth.Heal(healAmount);

    }
}