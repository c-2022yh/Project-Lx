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

    // 2. 이 코루틴의 이름을 HandleSkill로 변경 (중복 회피)
    private IEnumerator HandleSkill(Player p, SkillData data, System.Action<bool> setCd, bool onCd, string key)
    {
        // 공통 차단 조건
        if (data == null || onCd || p.isAttacking || p.isSkillActive) yield break;
        if (key == "D" && !p.isGrounded) yield break;

        // F 스킬 특수 로직
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

        // 일반 스킬 실행 (데이터 안에 있는 ProcessSkill 호출)
        setCd(true);
        // 여기서 data.ProcessSkill에 필요한 5개 인자를 정확히 전달합니다.
        yield return StartCoroutine(data.ProcessSkill(p, rangeIndicator, weaponHandle, swordCollider, defaultAngle));

        yield return new WaitForSeconds(data.cooldown);
        setCd(false);
    }


}