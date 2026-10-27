using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Skills/ShadowSwap", menuName = "Skills/ShadowSwap")]

//그림자 교체 스킬
public class ShadowSwapSkillData : UtilitySkillData
{
    [Header("Shadow Settings")]
    public GameObject shadowPrefab;

    private GameObject currentShadow;

    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        if (shadowPrefab == null) yield break;
        PlayReleaseSound(p, p.transform.position);
        

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