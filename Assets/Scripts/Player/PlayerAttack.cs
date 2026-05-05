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
    public bool isThrust; //찌르기 모드 여부
    public float thrustDistance;
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
        //0내려베기, 1올려배기, 2찌르기 순서대로 무한 반복
        int index = attackCount % patterns.Length;
        StartCoroutine(AttackRoutine(p, patterns[index]));
        attackCount++;
    }


    IEnumerator AttackRoutine(Player p, AttackPattern data)
    {
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

        p.isAttacking = false;
    }




}