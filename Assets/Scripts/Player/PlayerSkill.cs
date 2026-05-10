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

    [Header("F Skill (Shadow)")]
    [SerializeField] private GameObject shadowPrefab;
    private GameObject currentShadow; //현재 생성된 그림자 저장

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
        // F 스킬은 쿨타임 계산 방식이 다르므로 따로 처리합니다.
        if (keyType == "F")
        {
            // 1. 현재 그림자가 있는지 미리 체크 (있다면 이번 클릭은 '교체'임)
            bool isSwapping = (currentShadow != null);

            // 2. 스킬 로직 실행 (소환 혹은 교체)
            yield return StartCoroutine(ShadowSkillRoutine(p, data));

            // 3. '교체'가 일어났을 때만 쿨타임을 돌립니다.
            if (isSwapping)
            {
                setCooldown(true);
                yield return new WaitForSeconds(data.cooldown);
                setCooldown(false);
            }

            // 소환만 했을 때는 여기서 루틴이 종료되어 쿨타임 없이 바로 다음 입력이 가능해집니다.
            yield break;
        }

        // --- A, S, D 스킬 (기존 로직 유지) ---
        setCooldown(true);

        if (keyType == "A")
        {
            Debug.Log("A 스킬 시전!");
            yield return StartCoroutine(DashSlashRoutine(p, data));
        }
        else if (keyType == "S")
        {
            Debug.Log("S 스킬 시전!");
            yield return StartCoroutine(HeavySlashRoutine(p, data));
        }
        else if (keyType == "D" && p.isGrounded) //지상에서만 사용 가능
        {
            Debug.Log("D 스킬 시전!");
            yield return StartCoroutine(GuardRoutine(p, data));
        }
        else
        {
            setCooldown(false);
            yield break;
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

        // 마찰력을 0으로 만들어 돌진 방해 요소를 제거
        p.rb.gravityScale = 0f;
        p.rb.linearDamping = 0f;

        float dir = Mathf.Sign(p.transform.localScale.x);

        // 1. 벽 체크 (실제 갈 수 있는 거리 계산)
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, Vector2.right * dir, data.dashDistance, LayerMask.GetMask("Ground"));
        float actualDashDistance = hit.collider != null ? hit.distance : data.dashDistance;

        // 2. 인디케이터 표시 (실제 거리만큼만)
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.SetParent(null);
            rangeIndicator.SetAndShow(
                new Vector2(actualDashDistance, data.hitBoxSize.y),
                data.indicatorColor, p.transform.position, dir);
        }

        // 3. 물리 로직
        // 오차를 줄이기 위해 actualDashDistance를 사용하여 속도를 다시 잡습니다.
        float dashSpeed = actualDashDistance / data.duration * 0.5f;
        float timer = 0f;

        while (timer < data.duration)
        {
            if (weaponHandle != null)
            {
                float progress = timer / data.duration;
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }

            // 돌진 중 속도 고정
            p.rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- [핵심 수정 부분] ---
        // 루프가 끝나면 즉시 속도를 (0, 0)으로 만들고 물리 연산을 한 프레임 쉽니다.
        p.rb.linearVelocity = Vector2.zero;

        // 중력과 마찰력을 돌려주기 전에 완전히 멈췄는지 확인
        yield return new WaitForFixedUpdate();
        p.rb.linearVelocity = Vector2.zero;
        // -----------------------

        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        if (swordCollider != null) swordCollider.enabled = false;

        // 잔상 효과
        yield return new WaitForSeconds(0.2f);

        if (rangeIndicator != null)
        {
            rangeIndicator.Hide();
            rangeIndicator.transform.SetParent(p.transform);
        }

        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        p.isSkillActive = false;
    }


    //스킬 물리 로직 (근거리 강력한 공격)
    private IEnumerator HeavySlashRoutine(Player p, SkillData data)
    {
        p.isSkillActive = true;
        if (swordCollider != null) swordCollider.enabled = true;

        float originalGravity = p.rb.gravityScale;
        float originalDrag = p.rb.linearDamping;
        p.rb.gravityScale = 0f;
        p.rb.linearDamping = 0f;
        p.rb.linearVelocity = Vector2.zero;

        if (rangeIndicator != null)
        {
            rangeIndicator.transform.SetParent(p.transform);
            rangeIndicator.transform.localPosition = Vector3.zero;

            rangeIndicator.SetAndShow(
                data.hitBoxSize,
                data.indicatorColor,
                p.transform.position,
                Mathf.Sign(p.transform.localScale.x)
            );
        }

        float timer = 0f;
        while (timer < data.duration)
        {
            if (weaponHandle != null)
            {
                float progress = timer / data.duration;
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        if (swordCollider != null) swordCollider.enabled = false;

        yield return new WaitForSeconds(0.1f); // 살짝 보여주고 삭제
        if (rangeIndicator != null) rangeIndicator.Hide();

        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        p.isSkillActive = false;
    }

    //스킬 물리 로직 (가드)
    private IEnumerator GuardRoutine(Player p, SkillData data)
    {
        if (!p.isGrounded) yield break;

        p.isSkillActive = true;

        // 1. 물리 상태 저장 및 고정 (시전 중 미끄러짐 방지)
        float originalGravity = p.rb.gravityScale;
        float originalDrag = p.rb.linearDamping;

        p.rb.gravityScale = 0f;
        p.rb.linearDamping = 0f;
        p.rb.linearVelocity = Vector2.zero; // 즉시 정지

        // 2. 가드 범위 표시 (파란색 영역)
        if (rangeIndicator != null)
        {
            // 플레이어 자식으로 붙여서 위치 유지
            rangeIndicator.transform.SetParent(p.transform);
            rangeIndicator.transform.localPosition = data.indicatorOffset;

            rangeIndicator.SetAndShow(
                data.hitBoxSize,
                data.indicatorColor,
                p.transform.position,
                Mathf.Sign(p.transform.localScale.x)
            );
        }

        // 3. 가드 유지 시간 (검 들고 버티기)
        float timer = 0f;
        while (timer < data.duration)
        {
            // 검 각도 유지 (SkillData에서 설정한 Start/End Angle 사용)
            if (weaponHandle != null)
            {
                float progress = timer / data.duration;
                float currentAngle = Mathf.Lerp(data.startAngle, data.endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }

            // 혹시 모를 밀림 방지
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 4. 물리 상태 복구
        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        if (rangeIndicator != null) rangeIndicator.Hide();

        // 검 위치 원래대로
        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        p.isSkillActive = false;
    }

    //스킬 물리 로직 (그림자 텔포)
    private IEnumerator ShadowSkillRoutine(Player p, SkillData data)
    {
        // 1. 그림자가 없다면 -> 즉시 소환!
        if (currentShadow == null)
        {
            // 내 현재 위치에 그림자 생성
            currentShadow = Instantiate(shadowPrefab, p.transform.position, Quaternion.identity);

            // 소환 직후에 그림자라는 걸 알 수 있게 살짝 이펙트를 주거나 소리만 나게 해도 충분합니다.
            Debug.Log("그림자 설치");

            yield return null; // 한 프레임만 대기하고 즉시 컨트롤 반환
        }
        // 2. 그림자가 이미 있다면 -> 위치 바꾸기!
        else
        {
            // 위치 스왑
            Vector3 playerPos = p.transform.position;
            Vector3 shadowPos = currentShadow.transform.position;

            p.transform.position = shadowPos;
            currentShadow.transform.position = playerPos;
            
            Debug.Log("그림자 순간이동 완료");

            // 교체 후 그림자 제거
            Destroy(currentShadow);
            currentShadow = null;

            // 이동 직후 물리 속도 초기화 (공중에서 바꿨을 때 튀는 현상 방지)
            p.rb.linearVelocity = Vector2.zero;

            yield return null;
        }
    }


}