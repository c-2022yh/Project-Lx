using System.Collections;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{

    private void Flip(Player p)
    {
        p.isFacingRight = !p.isFacingRight;
        Vector3 newScale = p.transform.localScale;
        newScale.x *= -1;
        p.transform.localScale = newScale;
    }

    public void DoMove(Player p, float speedMultiplier = 1f, float accelMultiplier = 1f)
    {
        //움직이는 방향 바라보기
        if (p.moveInput.x > 0 && !p.isFacingRight) Flip(p);
        else if (p.moveInput.x < 0 && p.isFacingRight) Flip(p);

        //목표 속도 계산 (보정값 적용)
        float targetSpeedX = p.moveInput.x * (p.moveSpeed * speedMultiplier);

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

    public void ExecuteJump(Player p, float jumpMultiplier = 1f)
    {
        p.rb.linearVelocity = new Vector2(p.rb.linearVelocity.x, p.jumpForce * jumpMultiplier);
    }

    public void ExecuteDash(Player p)
    {
        if (Time.time < p.lastDashTime + p.dashCooldown) return;
        if(p.moveInput.x == 0)
        {
            Debug.Log("<color=yellow>[Dash]</color> 방향키 입력 없어 대쉬 취소");
            return;
        }

        float dashDir = p.moveInput.x > 0 ? 1f : -1f;
        p.lastDashTime = Time.time;

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
        while (timer < p.waitSecond)
        {
            p.rb.linearVelocity = new Vector2(dir * p.dashForce, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        p.rb.linearVelocity = Vector2.zero;
        p.rb.gravityScale = originalGravity;
        p.rb.linearDamping = originalDrag;

        p.isDashing = false;

    }

}


