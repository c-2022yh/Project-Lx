using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    private void Flip(Player p)
    {
        if (p.isAttacking || p.isDashing || p.isSkillActive) return; //공격중이면 방향전환x

        p.isFacingRight = !p.isFacingRight;
        Vector3 newScale = p.transform.localScale;
        newScale.x *= -1;
        p.transform.localScale = newScale;
    }

    public void DoMove(Player p, float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        if (p.isSkillActive || p.isDashing)  return;


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
        float targetSpeedX = p.moveInput.x * (p.moveSpeed * speedMultiplier) * attackSpeedFactor;

        //공중 제어 보정
        if (!p.isGrounded) targetSpeedX *= p.airControlMin;

        //가속/감속 비율 계산
        float decelVar = p.isGrounded ? p.groundDecel : p.airDecel;
        //가속도에도 보정값이 필요하다면 적용 (1f - decelVar가 클수록 빠릿하게 반응)
        float lerpFactor = (1f - decelVar) * accelMultiplier;
        //플레이어가 부드럽게 움직이도록 하는 과정
        float newSpeedX = Mathf.Lerp(p.rb.linearVelocity.x, targetSpeedX, lerpFactor);
        //일정량의 작은 미끄러짐은 0으로 보정
        if (p.moveInput.x == 0 && Mathf.Abs(newSpeedX) < 0.1f) newSpeedX = 0f;

        //최종 이동속도 적용
        p.rb.linearVelocity = new Vector2(newSpeedX, p.rb.linearVelocity.y);
    }

    //점프 실행
    public void ExecuteJump(Player p, float jumpMultiplier = 1f)
    {
        p.rb.linearVelocity = new Vector2(p.rb.linearVelocity.x, p.jumpForce * jumpMultiplier);
    }

    public void ExecuteDash(Player p)
    {
        if (Time.time < p.lastDashTime + p.dashCooldown) return;

        float dashDir = Mathf.Sign(p.transform.localScale.x);
        p.lastDashTime = Time.time;

        //대쉬 중 중력 잠시 끄기
        StartCoroutine(DashRoutine(p, dashDir));
    }

    private IEnumerator DashRoutine(Player p, float dir)
    {
        p.isDashing = true;

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
        while (timer < p.waitSecond)
        {
            p.rb.linearVelocity = new Vector2(dir * p.dashForce / p.waitSecond, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //중력 재가동
        p.SetPhysicsFreeze(false);

        p.isDashing = false;

    }

}


