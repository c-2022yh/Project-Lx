using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillD_Guard", menuName = "SkillD_Guard")]

public class SkillD_Guard : SkillData
{
    public override IEnumerator ProcessSkill(Player p, SkillRangeIndicator rangeindicator, Transform weaponHandle, Collider2D swordCollider)
    {
        if (!p.isGrounded) yield break;

        p.isSkillActive = true;

        //중력 정지
        p.SetPhysicsFreeze(true);

        //가드 범위 표시 (파란색 영역)
        if (rangeIndicator != null)
        {
            //플레이어 자식으로 붙여서 위치 유지
            rangeIndicator.transform.SetParent(p.transform);
            rangeIndicator.transform.localPosition = data.indicatorOffset;

            rangeIndicator.SetAndShow(
                data.hitBoxSize,
                data.indicatorColor,
                p.transform.position,
                Mathf.Sign(p.transform.localScale.x)
            );
        }

        //가드 유지 시간 (검 들고 버티기)
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

            //혹시 모를 밀림 방지
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //중력 재가동
        p.SetPhysicsFreeze(false);

        if (rangeIndicator != null) rangeIndicator.Hide();

        //검 위치 원래대로
        if (weaponHandle != null)
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        p.isSkillActive = false;

    }

}
