using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkill : MonoBehaviour
{
    [Header("Skill Slots")]
    public SkillData skillA;
    public SkillData skillS;
    public SkillData skillD;
    public SkillData skillF;

    [Header("Dependencies")]
    [SerializeField] private GameObject weaponHandle;
    [SerializeField] private GameObject swordVisual;
    [SerializeField] private Collider2D swordCollider;

    [Header("Skill Data")]
    [SerializeField] private float defaultAngle = 20f;

    private Vector3 originLocalPos;


    // 각 스킬의 마지막 사용 시간 (쿨타임 체크용)
    private bool isSkillACooldown = false;
    private bool isSkillSCooldown = false;
    private bool isSkillDCooldown = false;
    private bool isSkillFCooldown = false;

    public void ExecuteSkillA(Player p)
    {
        if (isSkillACooldown || p.isAttacking) return;
        Debug.Log("SkillA 시전!");
        StartCoroutine(SkillACooldownRoutine(p, skillA));
    }
    public void ExecuteSkillS(Player p)
    {
        Debug.Log("SkillS");
        //StartCoroutine(SkillRoutine(p, skillS));
    }
    public void ExecuteSkillD(Player p)
    {
        Debug.Log("SkillD");
        //StartCoroutine(SkillRoutine(p, skillD));
    }
    public void ExecuteSkillF(Player p)
    {
        Debug.Log("SkillF");
        //StartCoroutine(SkillRoutine(p, skillF));
    }

    //스킬 쿨타임 관리 코루틴
    IEnumerator SkillACooldownRoutine(Player p, SkillData data)
    {
        isSkillACooldown = true;
        yield return StartCoroutine(DashSlashRoutine(p, data));
        yield return new WaitForSeconds(data.cooldown);
        isSkillACooldown = false;
    }


    //이동 돌진 베기 코루틴
    IEnumerator DashSlashRoutine(Player p, SkillData data)
    {
        p.isSkillActive = true;
        if (swordCollider != null) swordCollider.enabled = true;

        float originalGravity = p.rb.gravityScale; //중력 값
        float originalDrag = p.rb.linearDamping; //공기 저항(마찰)

        p.rb.gravityScale = 0f; //중력 0
        p.rb.linearDamping = 0f; //공기저항 0
        //대쉬 처음부터 끝까지 같은 속도로 날아가게 함

        //입력이 없으면 보는 방향, 있으면 입력 방향
        float dir = Mathf.Sign(p.transform.localScale.x);

        float dashSpeed = data.dashDistance / data.duration;

        //고정 이동 루프
        float timer = 0f;
        while (timer < data.duration)
        {
            //무기 회전 로직
            if (weaponHandle != null)
            {
                float progress = timer / data.duration;
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }

            // 물리 속도 고정
            p.rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        p.rb.linearVelocity = Vector2.zero;
        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        if (swordCollider != null) swordCollider.enabled = false;
        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);
        
        p.isSkillActive = false;
    }

}