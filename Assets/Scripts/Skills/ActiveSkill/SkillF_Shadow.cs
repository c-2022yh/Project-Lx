using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillF_Shadow", menuName = "Skills/SkillF_Shadow")]
public class SkillF_Shadow : SkillData
{
    [Header("Shadow Settings")]
    public GameObject shadowPrefab;

    private GameObject currentShadow;

    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        if (shadowPrefab == null) yield break;
        

        if (currentShadow == null)
        {
            currentShadow = Instantiate(shadowPrefab, p.transform.position, Quaternion.identity);
            Debug.Log("그림자 설치");
            yield break;
        }

        Vector3 playerPos = p.transform.position;
        Vector3 shadowPos = currentShadow.transform.position;

        p.transform.position = shadowPos;

        Destroy(currentShadow);
        currentShadow = null;

        if (p.rb != null)
        {
            p.rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("그림자 순간이동 완료");

        yield break;
    }
}