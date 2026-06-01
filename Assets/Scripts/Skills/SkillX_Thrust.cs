using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillX_Thrust", menuName = "Skills/SkillX_Thrust")]
public class SkillX_Thrust : SkillData
{
    [Header("Thrust Settings")]
    public float thrustDistance = 1.8f;
    public float weaponThrustOffset = 0.7f;


    public override IEnumerator ProcessSkill(
       Player p,
       SkillRangeIndicator indicator,
       GameObject weaponHandle,
       Collider2D swordCollider,
       float defaultAngle)
    {
        if (swordCollider != null) swordCollider.enabled = true;

        //공격 방향 설정
        float dir = p.isFacingRight ? 1f : -1f;

        //시작 지점, 착지 지점 설정
        Vector2 startPos = p.rb.position;
        Vector2 targetPos = startPos + new Vector2(dir * thrustDistance, 0f);
        
        //중력 정지
        p.SetPhysicsFreeze(true);

        //인디케이터 표시
        if (indicator != null)
        {
            indicator.transform.SetParent(p.transform);
            indicator.transform.localPosition = Vector3.zero;

            indicator.SetAndShow(
                hitBoxSize,
                indicatorColor,
                p.transform.position,
                dir
            );
        }

        //실제 동작
        float timer = 0f;
        while (timer < duration)
        {
            float progress = timer / duration;

            Vector2 nextPos = Vector2.Lerp(startPos, targetPos, progress);
            p.rb.MovePosition(nextPos);
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        p.rb.MovePosition(targetPos);

        //중력 재가동
        p.SetPhysicsFreeze(false);


        if (swordCollider != null) swordCollider.enabled = false;

        yield return new WaitForSeconds(0.1f); // 살짝 보여주고 삭제
        if (indicator != null) indicator.Hide();

        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);


    }
}
