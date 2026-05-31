using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillS_Slash", menuName = "SkillS_Slash")]

public class SkillS_Slash : SkillData
{
    public override IEnumerator ProcessSkill(
       Player p,
       SkillRangeIndicator indicator,
       GameObject weaponHandle,
       Collider2D swordCollider,
       float defaultAngle)
    {
        if (swordCollider != null) swordCollider.enabled = true;

        //중력 정지
        p.SetPhysicsFreeze(true);

        if (indicator != null)
        {
            indicator.transform.SetParent(p.transform);
            indicator.transform.localPosition = Vector3.zero;

            indicator.SetAndShow(
                hitBoxSize,
                indicatorColor,
                p.transform.position,
                Mathf.Sign(p.transform.localScale.x)
            );
        }

        float timer = 0f;
        while (timer < duration)
        {
            if (weaponHandle != null)
            {
                float progress = timer / duration;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
                weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //중력 재가동
        p.SetPhysicsFreeze(false);

        if (swordCollider != null) swordCollider.enabled = false;

        yield return new WaitForSeconds(0.1f); // 살짝 보여주고 삭제
        if (indicator != null) indicator.Hide();

        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);


    }
}
