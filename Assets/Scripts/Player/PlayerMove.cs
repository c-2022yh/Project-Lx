using System.Collections;
using UnityEngine;

//플레이어 움직임 (이동, 점프, 대쉬) 구현한 스크립트
public class PlayerMove : MonoBehaviour
{
    private Player player;
    private Rigidbody2D rb;

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
    private float lastGroundedTime = -999f;    //마지막으로 땅에 있었던 시간
    private float lastJumpRequestTime = -999f; //마지막으로 점프키를 누른 시간
    

    //대쉬관련 세팅
    [Header("Dash Settings")]
    public float dashSpeed = 100f;
    public float dashDuration = 0.02f;
    public float dashCooldown = 0.5f;
    public float lastDashTime; //대쉬 내부쿨 관련 변수

    //대쉬 잠금 여부 (유물 장착 시 해제)
    [SerializeField] private bool isDashUnlocked = false;

    public bool IsDashUnlocked => isDashUnlocked;

    //공중에서 마찰계수 정하기 ->스무딩 저항값
    [Header("Friction (Lerp)")]
    [Range(0, 1)] public float airControlMin = 0.8f;
    [Range(0f, 0.3f)] public float groundSmooth = 0.01f;
    [Range(0f, 0.5f)] public float airSmooth = 0.1f;

    private void Awake()
    {
        //컴포넌트 연결
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Flip()
    {
        if (player.ActionState.isAttacking ||
            player.ActionState.isDashing ||
            player.ActionState.isSkillActive) return; //공격중이면 방향전환x

        player.isFacingRight = !player.isFacingRight;
        Vector3 newScale = player.transform.localScale;
        newScale.x = Mathf.Abs(newScale.x) * (player.isFacingRight ? 1f : -1f);
        player.transform.localScale = newScale;
    }

    ///이동 함수
    public void ExecuteMove(float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        if (!player.ActionState.CanMove()) return;


        //움직이는 방향 바라보기
        if (player.moveInput.x > 0 && !player.isFacingRight) Flip();
        else if (player.moveInput.x < 0 && player.isFacingRight) Flip();


        //목표 속도 계산 (보정값 적용)
        float rawTargetSpeedX = player.moveInput.x * (moveSpeed * speedMultiplier);
        //공중 제어 보정
        if (!player.isGrounded) rawTargetSpeedX *= airControlMin;

        //가속/감속 비율 계산
        float decelVar = player.isGrounded ? groundSmooth : airSmooth;
        //가속도에도 보정값이 필요하다면 적용 (1f - decelVar가 클수록 빠릿하게 반응)
        float lerpFactor = (1f - decelVar) * accelMultiplier;
        //플레이어가 부드럽게 움직이도록 하는 과정
        float calculatedX = Mathf.Lerp(player.rb.linearVelocity.x, rawTargetSpeedX, lerpFactor);
        //일정량의 작은 미끄러짐은 0으로 보정
        if (player.moveInput.x == 0 && Mathf.Abs(calculatedX) < 0.1f) calculatedX = 0f;

        //움직임 실행
        player.rb.linearVelocity = new Vector2(calculatedX, player.rb.linearVelocity.y);
    }



    ///점프 함수
    public void RequestJump() { lastJumpRequestTime = Time.time; }
    public void ExecuteJump()
    {
        //액션 불가능 상태
        if (!player.ActionState.CanMove()) return;

        //땅에 닿으면 초기화
        if (player.isGrounded && player.rb.linearVelocity.y <= 0.01f)
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
        if ((player.isGrounded || isCoyoteValid) && Time.time >= lastJumpTime + jumpCooldown)
        {
            lastGroundedTime = -999f; //코요태 타임 초기화
            shouldJump = true;
        }
        //공중점프
        else if (!player.isGrounded && remainingAirJumps > 0)
        {
            remainingAirJumps--;
            shouldJump = true;
        }


        //최종 점프 실행
        if (shouldJump)
        {
            lastJumpRequestTime = -999f; //선입력 버퍼 초기화
            lastJumpTime = Time.time;

            player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, jumpForce );
        }
    }

    //현재 최대 공중 점프 횟수
    public int MaxAirJumps => maxAirJumps;

    //최대 공중 점프 횟수 변경
    public void ModifyMaxAirJumps(int amount)
    {
        maxAirJumps += amount;

        //공중 점프 횟수는 음수가 되지 않도록 제한
        maxAirJumps = Mathf.Max(0, maxAirJumps);

        //장착 중이거나 해제되었을 때
        //현재 남은 공중 점프 횟수도 같이 보정
        remainingAirJumps = Mathf.Clamp(remainingAirJumps + amount, 0, maxAirJumps);

        UnityEngine.Debug.Log(
            $"Max Air Jumps: {maxAirJumps}, " +
            $"Remaining Air Jumps: {remainingAirJumps}"
        );
    }


    /// 대쉬 함수
    public void ExecuteDash()
    {
        if (!isDashUnlocked) return;
        if (Time.time < lastDashTime + dashCooldown) return;

        lastDashTime = Time.time;

        float dir;

        if (Mathf.Abs(player.moveInput.x) > 0.01f)
            dir = Mathf.Sign(player.moveInput.x);
        else
            dir = player.isFacingRight ? 1f : -1f;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(dir));
    }

    private IEnumerator DashRoutine(float dir)
    {
        //상태 업데이트
        player.ActionState.EnterDash(); 

        //중력 제거
        player.SetPhysicsFreeze(true);

        //대쉬 시작 지점에 잔상
        GameObject ghost = GhostPooler.Instance.GetGhost();
        if (ghost != null)
        {
            ghost.SetActive(true);
            ghost.GetComponent<GhostEffect>().Init(
                player.sr.sprite,
                player.transform.position,
                player.transform.rotation,
                player.transform.localScale
            );
        }

        //고정 이동 루프
        float timer = 0f;
        while (timer < dashDuration)
        {
            player.rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        player.rb.linearVelocity = new Vector2(0f, player.rb.linearVelocity.y);

        //상태 되돌리기 but,본인이 바꾼 상태일때만 노말로 교체->남이 바꾼 State 참견 금지
        if (player.ActionState.isDashing)
        {
            player.ActionState.EnterNormal();
        }


        //중력 되돌리기
        player.SetPhysicsFreeze(false);

    }

    //대쉬 해금 여부 설정
    public void SetDashUnlocked(bool unlocked)
    {
        isDashUnlocked = unlocked;
    }

}


