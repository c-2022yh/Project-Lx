using System.Collections;
using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    //인스펙터 연결
    [Header("Skill Slots")]
    public SkillData skillA, skillS, skillD, skillF;

    [Header("Dependencies")]
    [SerializeField] private GameObject weaponHandle;
    [SerializeField] private Collider2D swordCollider;
    [SerializeField] private SkillRangeIndicator rangeIndicator;
    [SerializeField] private float defaultAngle = 20f;
    [SerializeField] private GameObject shadowPrefab;
    private GameObject currentShadow; //현재 생성된 그림자 저장

    // 쿨타임 상태 관리
    private bool isACooldown, isSCooldown, isDCooldown, isFCooldown;

    public void ExecuteSkillA(Player p) => StartCoroutine(HandleSkill(p, skillA, v => isACooldown = v, isACooldown, "A"));
    public void ExecuteSkillS(Player p) => StartCoroutine(HandleSkill(p, skillS, v => isSCooldown = v, isSCooldown, "S"));
    public void ExecuteSkillD(Player p) => StartCoroutine(HandleSkill(p, skillD, v => isDCooldown = v, isDCooldown, "D"));
    public void ExecuteSkillF(Player p) => StartCoroutine(HandleSkill(p, skillF, v => isFCooldown = v, isFCooldown, "F"));

    private IEnumerator HandleSkill(Player p, SkillData data, System.Action<bool> setCd, bool onCd, string key)
    {
        if (data == null || onCd || p.isAttacking || p.isSkillActive) yield break;
        if ((key == "D" || key == "S") && !p.isGrounded) yield break;

        //F 스킬 특수 처리 (매니저가 그림자 객체를 들고 있어야 함)
        if (key == "F")
        {
            if (currentShadow == null)
            {
                currentShadow = Instantiate(shadowPrefab, p.transform.position, Quaternion.identity);
            }
            else
            {
                setCd(true);
                Vector3 temp = p.transform.position;
                p.transform.position = currentShadow.transform.position;
                currentShadow.transform.position = temp;
                Destroy(currentShadow);
                currentShadow = null;
                p.rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(data.cooldown);
                setCd(false);
            }
            yield break;
        }

        // 일반 스킬 실행
        setCd(true);
        yield return StartCoroutine(data.ProcessSkill(p, rangeIndicator, weaponHandle, swordCollider, defaultAngle));
        yield return new WaitForSeconds(data.cooldown);
        setCd(false);
    }


}