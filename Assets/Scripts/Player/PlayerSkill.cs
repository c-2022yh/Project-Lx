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
        if (data == null || isCooldown || (!p.playerActionState.CanSkill())) return;

        // SkillMasterRoutine에 keyType("A", "S" 등)을 전달합니다.
        StartCoroutine(SkillMasterRoutine(p, data, setCooldown, keyType));
    }

    private IEnumerator SkillMasterRoutine(Player p, SkillData data, System.Action<bool> setCooldown, string keyType)
    {
        //상태 업데이트
        p.playerActionState.EnterSkill();


        // F 스킬은 쿨타임 계산 방식이 다르므로 따로 처리합니다.
        if (keyType == "F")
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
        setCooldown(false);

        
    }

    //스킬 물리 로직 (돌진 및 베기)
    private IEnumerator DashSlashRoutine(Player p, SkillData data)
    {
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

        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (p.playerActionState.isSkillActive)
        {
            p.playerActionState.EnterNormal();
        }

    }


    //스킬 물리 로직 (근거리 강력한 공격)
    private IEnumerator HeavySlashRoutine(Player p, SkillData data)
    {
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

        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (p.playerActionState.isSkillActive)
        {
            p.playerActionState.EnterNormal();
        }

    }

    //스킬 물리 로직 (가드)
    private IEnumerator GuardRoutine(Player p, SkillData data)
    {
        if (!p.isGrounded) yield break;

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

        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (p.playerActionState.isSkillActive)
        {
            p.playerActionState.EnterNormal();
        }


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

            //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
            if (p.playerActionState.isSkillActive)
            {
                p.playerActionState.EnterNormal();
            }

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

            //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
            if (p.playerActionState.isSkillActive)
            {
                p.playerActionState.EnterNormal();
            }

            yield return null;

        }

    }


}