using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillS_Slash", menuName = "Skills/SkillS_Slash")]

public class SkillS_Slash : AttackSkillData
{
    [Header("Slash Effect")] //공격 이펙트
    public GameObject slashEffectPrefab;
    public Vector2 effectOffset = new Vector2(0.9f, 0f);
    public Vector3 effectScale = Vector3.one;
    public float effectLifeTime = 0.12f;

    [Header("Multi Slash")] //이펙트 출력 관련
    public float[] slashRotations = new float[] { 35f, -25f, -60f };
    public float slashInterval = 0.08f;
    public float damagePerSlash = 1f;


    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;

        float dir = p.isFacingRight ? 1f : -1f;

        //시전 중 제자리 고정
        p.SetPhysicsFreeze(true);

        //샤샤샥 연속 베기
        for (int i = 0; i < slashRotations.Length; i++)
        {
            SpawnSlashEffect(p, dir, slashRotations[i]);
            yield return new WaitForSeconds(slashInterval);
        }

        //스킬 전체 후딜 느낌
        float remainTime = activeTime - (slashRotations.Length * slashInterval);
        if (remainTime > 0f)
        {
            yield return new WaitForSeconds(remainTime);
        }

        //중력 재적용
        p.SetPhysicsFreeze(false);

    }

    //베기 이펙트 소환하기
    private void SpawnSlashEffect(Player p, float dir, float rotationZ)
    {
        if (slashEffectPrefab == null) return;

        Vector3 spawnPos = p.transform.position + new Vector3(effectOffset.x * dir, effectOffset.y, 0f);

        Quaternion rotation = Quaternion.Euler(0f, 0f, rotationZ * dir);

        GameObject effectObj = Instantiate(slashEffectPrefab, spawnPos, rotation);

        effectObj.transform.localScale = effectScale;

        //플레이어가 바라보는 방향으로
        SpriteRenderer sr = effectObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        //히트박스 생성
        AttackEffectHitbox hitbox = effectObj.GetComponentInChildren<AttackEffectHitbox>();
        if (hitbox != null) hitbox.SetAttackInfo(damagePerSlash, dir);

        //이펙트 삭제
        Destroy(effectObj, effectLifeTime);
    }

}
