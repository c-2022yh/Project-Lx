using System.Collections;
using UnityEngine;

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
    [SerializeField] private SkillRangeIndicator rangeIndicator;

    [Header("Settings")]
    [SerializeField] private float defaultAngle = 20f;

    // 쿨타임 상태 관리
    private bool isACooldown;
    private bool isSCooldown;
    private bool isDCooldown;
    private bool isFCooldown;

    #region Input Execution
    // 여기서 어떤 키인지 "A", "S" 등을 같이 넘겨줍니다.
    public void ExecuteSkillA(Player p) => UseSkill(p, skillA, isACooldown, (v) => isACooldown = v, "A");
    public void ExecuteSkillS(Player p) => UseSkill(p, skillS, isSCooldown, (v) => isSCooldown = v, "S");
    public void ExecuteSkillD(Player p) => UseSkill(p, skillD, isDCooldown, (v) => isDCooldown = v, "D");
    public void ExecuteSkillF(Player p) => UseSkill(p, skillF, isFCooldown, (v) => isFCooldown = v, "F");
    #endregion

    private void UseSkill(Player p, SkillData data, bool isCooldown, System.Action<bool> setCooldown, string keyType)
    {
        if (data == null || isCooldown || p.isAttacking || p.isSkillActive) return;

        // SkillMasterRoutine에 keyType("A", "S" 등)을 전달합니다.
        StartCoroutine(SkillMasterRoutine(p, data, setCooldown, keyType));
    }

    private IEnumerator SkillMasterRoutine(Player p, SkillData data, System.Action<bool> setCooldown, string keyType)
    {
        setCooldown(true);
        if (keyType == "A")
        {
            Debug.Log("A 스킬 시전!");
            yield return StartCoroutine(DashSlashRoutine(p, data));
        }
        else if (keyType == "S")
        {
            // S키에 맞는 새로운 루틴이 생기면 여기에 연결 (지금은 예시)
            Debug.Log("S 스킬 시전!");
            //yield return StartCoroutine(DashSlashRoutine(p, data));
        }
        else if (keyType == "D")
        {
            Debug.Log("D 스킬 시전!");
            //yield return StartCoroutine(DashSlashRoutine(p, data));
        }
        else if (keyType == "F")
        {
            Debug.Log("F 스킬 시전!");
            //yield return StartCoroutine(DashSlashRoutine(p, data));
        }

        yield return new WaitForSeconds(data.cooldown);
        setCooldown(false);
    }

    //스킬 물리 로직 (돌진 및 베기)
    private IEnumerator DashSlashRoutine(Player p, SkillData data)
    {
        p.isSkillActive = true;
        if (swordCollider != null) swordCollider.enabled = true;

        float originalGravity = p.rb.gravityScale;
        float originalDrag = p.rb.linearDamping;

        p.rb.gravityScale = 0f;
        p.rb.linearDamping = 0f;

        float dir = Mathf.Sign(p.transform.localScale.x);
        float dashSpeed = data.dashDistance / data.duration;

        //범위 표시
        if (rangeIndicator != null)
            rangeIndicator.SetAndShow(data.hitBoxSize, data.indicatorColor, data.indicatorOffset);

        float timer = 0f;
        while (timer < data.duration)
        {
            if (weaponHandle != null)
            {
                float progress = timer / data.duration;
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }

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

        if (rangeIndicator != null) rangeIndicator.Hide();

        p.isSkillActive = false;
    }
}