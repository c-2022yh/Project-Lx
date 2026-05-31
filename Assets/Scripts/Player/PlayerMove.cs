using System.Collections;
using UnityEngine;

//플레이어 움직임 (이동, 점프, 대쉬) 구현한 스크립트
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
    [SerializeField] private int maxAirJumps = 1;
    private int remainingAirJumps;

    //코요태 타임 관련 세팅
    [Header("Jump Juice")]
    public float coyoteTime = 0.15f;
    public float bufferTime = 0.15f;
    private float lastGroundedTime = -999f;  // 마지막으로 땅에 있었던 시간
    private float lastJumpRequestTime = -999f; // 마지막으로 점프키를 누른 시간
    

    //대쉬관련 세팅
    [Header("Dash Settings")]
    public float dashSpeed = 100f;
    public float dashDuration = 0.02f;
    public float dashCooldown = 0.5f;
    public float lastDashTime; //대쉬 내부쿨 관련 변수

    //공중에서 마찰계수 정하기 ->스무딩 저항값
    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundSmooth = 0.01f;
    [Range(0f, 0.5f)] public float airSmooth = 0.1f;

    private void Flip(Player p)
    {
        if (p.playerActionState.isAttacking ||
            p.playerActionState.isDashing ||
            p.playerActionState.isSkillActive) return; //공격중이면 방향전환x

        p.isFacingRight = !p.isFacingRight;
        Vector3 newScale = p.transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * (p.isFacingRight ? 1f : -1f);
        p.transform.localScale = newScale;
    }

    ///이동 함수
    public void ExecuteMove(Player p, float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        if (!p.playerActionState.CanMove()) return;


        //움직이는 방향 바라보기
        if (p.moveInput.x > 0 && !p.isFacingRight) Flip(p);
        else if (p.moveInput.x < 0 && p.isFacingRight) Flip(p);


        //목표 속도 계산 (보정값 적용)
        float rawTargetSpeedX = p.moveInput.x * (moveSpeed * speedMultiplier);
        //공중 제어 보정
        if (!p.isGrounded) rawTargetSpeedX *= airControlMin;

        //가속/감속 비율 계산
        float decelVar = p.isGrounded ? groundSmooth : airSmooth;
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
    public void RequestJump() { lastJumpRequestTime = Time.time; }
    public void ExecuteJump(Player p)
    {
        //액션 불가능 상태
        if (!p.playerActionState.CanMove()) return;

        //땅에 닿으면 초기화
        if (p.isGrounded && p.rb.linearVelocity.y <= 0.01f)
        {
            lastGroundedTime = Time.time;
            remainingAirJumps = maxAirJumps;
        }

        //필터링: 선입력이 없거나 유효시간이 지나면 끝
        if (Time.time - lastJumpRequestTime > bufferTime) return;
        
        //(지상:코요태 vs 공중)
        bool isCoyoteValid = (Time.time - lastGroundedTime <= coyoteTime);
        bool shouldJump = false;

        //지상 점프 or 코요태 타임 중
        if ((p.isGrounded || isCoyoteValid) && Time.time >= lastJumpTime + jumpCooldown)
        {
            lastGroundedTime = -999f; //코요태 타임 초기화
            shouldJump = true;
        }
        //공중점프
        else if (!p.isGrounded && remainingAirJumps > 0)
        {
            remainingAirJumps--;
            shouldJump = true;
        }


        //최종 점프 실행
        if (shouldJump)
        {
            lastJumpRequestTime = -999f; //선입력 버퍼 초기화
            lastJumpTime = Time.time;

            p.rb.linearVelocity = new Vector2(p.rb.linearVelocity.x, jumpForce );
        }
    }

    /// 대쉬 함수
    public void ExecuteDash(Player p)
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        lastDashTime = Time.time;

        float dir;

        if (Mathf.Abs(p.moveInput.x) > 0.01f)
            dir = Mathf.Sign(p.moveInput.x);
        else
            dir = p.isFacingRight ? 1f : -1f;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(p, dir));
    }

    private IEnumerator DashRoutine(Player p, float dir)
    {
        //상태 업데이트
        p.playerActionState.EnterDash();


        //중력 제거
        p.SetPhysicsFreeze(true);

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
        while (timer < dashDuration)
        {
            p.rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        p.rb.linearVelocity = new Vector2(0f, p.rb.linearVelocity.y);


        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (p.playerActionState.isDashing)
        {
            p.playerActionState.EnterNormal();
        }


        //중력 되돌리기
        p.SetPhysicsFreeze(false);

    }

}


