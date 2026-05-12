using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillF_Shadow", menuName = "SkillF_Shadow")]

public class SkillF_Shadow : SkillData
{
    public override IEnumerator ProcessSkill(Player p, SkillRangeIndicator rangeindicator, Transform weaponHandle, Collider2D swordCollider)
    {
        // 1. 그림자가 없다면 -> 즉시 소환!
        if (currentShadow == null)
        {
            // 내 현재 위치에 그림자 생성
            currentShadow = Instantiate(shadowPrefab, p.transform.position, Quaternion.identity);

            // 소환 직후에 그림자라는 걸 알 수 있게 살짝 이펙트를 주거나 소리만 나게 해도 충분합니다.
            Debug.Log("그림자 설치");

            yield return null; // 한 프레임만 대기하고 즉시 컨트롤 반환
        }
        // 2. 그림자가 이미 있다면 -> 위치 바꾸기!
        else
        {
            // 위치 스왑
            Vector3 playerPos = p.transform.position;
            Vector3 shadowPos = currentShadow.transform.position;

            p.transform.position = shadowPos;
            currentShadow.transform.position = playerPos;

            Debug.Log("그림자 순간이동 완료");

            // 교체 후 그림자 제거
            Destroy(currentShadow);
            currentShadow = null;

            // 이동 직후 물리 속도 초기화 (공중에서 바꿨을 때 튀는 현상 방지)
            p.rb.linearVelocity = Vector2.zero;

            yield return null;
        }

    }
}
