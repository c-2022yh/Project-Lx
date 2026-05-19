using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    //이동속도 세팅
    [Header("Movement Settings")]
    public float moveSpeed = 10f;

    //점프 세팅
    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float jumpCooldown = 0.4f;
    private float lastJumpTime = -999f;
    private bool canAirJump = true;

    // ★ 복잡한 타이머 대신 딱 3개만 선언 (기록용 시간 변수)
    [Header("Jump Juice")]
    public float coyoteTime = 0.15f;
    public float bufferTime = 0.15f;
    private float lastGroundedTime = -999f;  // 마지막으로 땅에 있었던 시간
    private float lastJumpPressTime = -999f; // 마지막으로 점프키를 누른 시간
    

    //대쉬관련 세팅
    [Header("Dash Settings")]
    public float dashForce = 2f;
    public float dashCooldown = 0.5f;
    public float waitSecond = 0.01f;
    public float lastDashTime; //대쉬 내부쿨 관련 변수

    //공중에서 마찰계수 정하기
    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundDecel = 0.01f;
    [Range(0f, 0.5f)] public float airDecel = 0.1f;

    private void Flip(Player p)
    {
        if (p.isAttacking || p.isDashing || p.isSkillActive) return; //공격중이면 방향전환x

        p.isFacingRight = !p.isFacingRight;
        Vector3 newScale = p.transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * (p.isFacingRight ? 1f : -1f);
        p.transform.localScale = newScale;
    }

    public void ResetAirJump()
    {
        canAirJump = true;
    }


    ///이동 함수
    public void ExecuteMove(Player p, float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        if (p.isSkillActive || p.isDashing) return;
        
        //움직이는 방향 바라보기
        if (p.moveInput.x > 0 && !p.isFacingRight) Flip(p);
        else if (p.moveInput.x < 0 && p.isFacingRight) Flip(p);

        //공격 중일 때의 추가 속도 보정값 계산
        float attackSpeedFactor = 1f;
        if (p.isAttacking && p.currentAttackPattern != null)
        {
            attackSpeedFactor = p.currentAttackPattern.moveSpeedMultiplier;
        }

        //목표 속도 계산 (보정값 적용)
        float rawTargetSpeedX = p.moveInput.x * (moveSpeed * speedMultiplier) * attackSpeedFactor;
        //공중 제어 보정
        if (!p.isGrounded) rawTargetSpeedX *= airControlMin;

        //가속/감속 비율 계산
        float decelVar = p.isGrounded ? groundDecel : airDecel;
        //가속도에도 보정값이 필요하다면 적용 (1f - decelVar가 클수록 빠릿하게 반응)
        float lerpFactor = (1f - decelVar) * accelMultiplier;
        //플레이어가 부드럽게 움직이도록 하는 과정
        float calculatedX = Mathf.Lerp(p.rb.linearVelocity.x, rawTargetSpeedX, lerpFactor);
        //일정량의 작은 미끄러짐은 0으로 보정
        if (p.moveInput.x == 0 && Mathf.Abs(calculatedX) < 0.1f) calculatedX = 0f;

        //움직임 실행
        p.rb.linearVelocity = new Vector2(calculatedX, p.rb.linearVelocity.y);
    }

    ///점프 함수
    public void ExecuteJump(Player p, float jumpMultiplier = 1f)
    {
        if (p.isSkillActive || p.isDashing) return;

        float calculatedY = jumpForce * jumpMultiplier;

        lastJumpPressTime = Time.time;

        //지상 점프
        bool isCoyoteValid = (Time.time - lastGroundedTime <= coyoteTime);
        if ((p.isGrounded || isCoyoteValid) && Time.time >= lastJumpTime + jumpCooldown)
        {
            lastJumpPressTime = -999f; // 지상 점프를 했으니 버퍼 초기화
            lastGroundedTime = -999f;  // 코요테 타임 소모

            lastJumpTime = Time.time;
            p.rb.linearVelocity = new Vector2(p.rb.linearVelocity.x, calculatedY);
        }
        //공중 점프
        else if (!p.isGrounded && canAirJump)
        {
            canAirJump = false; //발동 즉시 트리거 해제, 연속 공중점프 차단
            lastJumpTime = Time.time;
            p.rb.linearVelocity = new Vector2(p.rb.linearVelocity.x, calculatedY);
        }


    }


    public void ExecuteDash(Player p)
    {
        if (Time.time < lastDashTime + dashCooldown) return;
        if(p.moveInput.x == 0)
        {
            Debug.Log("<color=yellow>[Dash]</color> 방향키 입력 없어 대쉬 취소");
            return;
        }

        float dashDir = p.moveInput.x > 0 ? 1f : -1f;
        lastDashTime = Time.time;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(p, dashDir));
    }

    private IEnumerator DashRoutine(Player p, float dir)
    {
        p.isDashing = true;

        float originalGravity = p.rb.gravityScale; //중력 값
        float originalDrag = p.rb.linearDamping; //공기 저항(마찰)

        p.rb.gravityScale = 0f; //중력 0
        p.rb.linearDamping = 0f; //공기저항 0
        //대쉬 처음부터 끝까지 같은 속도로 날아가게 함

        //대쉬 시작 지점에 잔상
        GameObject ghost = GhostPooler.Instance.GetGhost();
        if (ghost != null)
        {
            ghost.SetActive(true);
            ghost.GetComponent<GhostEffect>().Init(
                p.spriteRenderer.sprite,
                p.transform.position,
                p.transform.rotation,
                p.transform.localScale
            );
        }

        //고정 이동 루프
        float timer = 0f;
        while (timer < waitSecond)
        {
            p.rb.linearVelocity = new Vector2(dir * dashForce / waitSecond, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        p.rb.linearVelocity = Vector2.zero;
        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        p.isDashing = false;

    }

}


