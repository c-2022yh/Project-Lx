using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillS_Slash", menuName = "SkillS_Slash")]

public class SkillS_Slash : SkillData
{
    public override IEnumerator ProcessSkill(Player p, SkillRangeIndicator rangeindicator, Transform weaponHandle, Collider2D swordCollider)
    {
        p.isSkillActive = true;
        if (swordCollider != null) swordCollider.enabled = true;

        //중력 정지
        p.SetPhysicsFreeze(true);

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

        //중력 재가동
        p.SetPhysicsFreeze(false);

        if (swordCollider != null) swordCollider.enabled = false;

        yield return new WaitForSeconds(0.1f); // 살짝 보여주고 삭제
        if (rangeIndicator != null) rangeIndicator.Hide();

        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        p.isSkillActive = false;

    }
}
