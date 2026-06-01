using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillA_Dash", menuName = "Skills/SkillA_Dash")]

public class SkillA_Dash : SkillData
{
    public override IEnumerator ProcessSkill(
       Player p,
       SkillRangeIndicator indicator,
       GameObject weaponHandle,
       Collider2D swordCollider,
       float defaultAngle)
    {
        if (swordCollider != null) swordCollider.enabled = true;

        float dir = Mathf.Sign(p.transform.localScale.x);

        //벽 체크 및 거리 계산
        RaycastHit2D hit = Physics2D.Raycast(p.transform.position, Vector2.right * dir, dashDistance, LayerMask.GetMask("Ground"));
        float actualDist = hit.collider ? hit.distance : dashDistance;

        //인디케이터 표시
        if (indicator != null)
        {
            indicator.transform.SetParent(null);
            indicator.SetAndShow(new Vector2(actualDist, hitBoxSize.y), indicatorColor, p.transform.position, dir);
        }

        if (swordCollider != null) swordCollider.enabled = true;
        
        p.SetPhysicsFreeze(true);


        // 3. 이동 및 회전 루프
        float timer = 0f;
        float speed = actualDist / duration * 0.5f;
        while (timer < duration)
        {
            p.rb.linearVelocity = new Vector2(dir * speed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 4. 복구
        p.SetPhysicsFreeze(false);

        if (swordCollider != null) swordCollider.enabled = false;

        yield return new WaitForSeconds(0.1f);
        if (indicator != null) indicator.Hide();
        

    }
}