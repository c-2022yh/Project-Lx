using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillD_Guard", menuName = "Skills/SkillD_Guard")]

public class SkillD_Guard : SkillData
{
    public override IEnumerator ProcessSkill(Player p)
    {
        if (!p.isGrounded) yield break;

        //중력 정지
        p.SetPhysicsFreeze(true);

        //가드 유지 시간 (검 들고 버티기)
        float timer = 0f;
        while (timer < duration)
        {
            
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //중력 재가동
        p.SetPhysicsFreeze(false);


    }

}
