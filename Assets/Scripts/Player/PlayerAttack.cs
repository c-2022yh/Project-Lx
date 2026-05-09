using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[System.Serializable]
public class AttackPattern
{
    public string attackName; //인스펙터 확인용
    public float startAngle; //시작 각도
    public float endAngle; //종료 각도
    public float duration; //공격 속도
    public bool isThrust; //찌르기 공격인가?
    public float thrustDistance; //찌르기 거리
    public float moveSpeedMultiplier = 0.2f; //공격시 이동속도 보정값
}

public class PlayerAttack : MonoBehaviour
{
    //인스펙터에서 연결
    [SerializeField] private GameObject weaponHandle;
    [SerializeField] private GameObject swordVisual;
    [SerializeField] private Collider2D swordCollider;

    [Header("Attack Sequences")]
    [SerializeField] private AttackPattern[] patterns;
    [SerializeField] private float defaultAngle = 20f;

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 0.5f; //이 시간이 지나면 콤보 초기화 
    private float lastAttackTime; //마지막 공격이 종료된 시간

    private int attackCount = 0;
    private Vector3 originLocalPos;


    void Awake()
    {
        //콜라이더, 오브젝트 연결
        if (swordCollider != null) swordCollider.enabled = false;
        if (swordVisual != null) originLocalPos = swordVisual.transform.localPosition;
        weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);
    }

    public void ExecuteAttack(Player p)
    {
        if (patterns.Length == 0) return;

        //마지막 공격 종료 후 일정 시간이 지났는지 체크
        if (Time.time > lastAttackTime + comboResetTime)
        {
            attackCount = 0; //콤보 초기화
        }

        //인덱스 계산 0,1,2
        int index = attackCount % patterns.Length;
        StartCoroutine(AttackRoutine(p, patterns[index]));
        attackCount++;
    }


    IEnumerator AttackRoutine(Player p, AttackPattern data)
    {
        p.currentAttackPattern = data;
        p.isAttacking = true;
        if (swordCollider != null) swordCollider.enabled = true;

        float elapsed = 0f;
        while (elapsed < data.duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / data.duration;

            if (data.isThrust) //찌르기
            {
                float moveAmount = Mathf.Sin(progress * Mathf.PI) * data.thrustDistance;
                swordVisual.transform.localPosition = originLocalPos + new Vector3(moveAmount, 0, 0);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, data.startAngle);
            }
            else //베기
            {
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
            yield return null;
        }

        if (swordCollider != null) swordCollider.enabled = false;
        weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);
        if (swordVisual != null) swordVisual.transform.localPosition = originLocalPos; 
        
        p.currentAttackPattern = null;
        p.isAttacking = false;

        lastAttackTime = Time.time;
    }




}