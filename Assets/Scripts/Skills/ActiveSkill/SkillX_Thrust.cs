using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "SkillX_Thrust", menuName = "Skills/SkillX_Thrust")]
public class SkillX_Thrust : AttackSkillData
{

    [Header("Thrust Timing")]
    public float startupTime = 0.1f;   //찌르기 전 준비 시간
    public float recoveryTime = 0.08f; //찌른 후 후딜

    [Header("Thrust Effect")] //찌르기 이펙트 표시
    public GameObject thrustEffectPrefab;
    public Vector2 effectOffset = new Vector2(1.2f, 0f);
    public Vector3 effectScale = Vector3.one;
    public float effectRotationZ = 0f;
    public float effectLifeTime = 0.18f;


    public override IEnumerator ProcessSkill(Player p)
    {
        //공격 방향 설정
        float dir = p.isFacingRight ? 1f : -1f;

        //중력 정지
        p.SetPhysicsFreeze(true);

        //선딜: 찌르기 준비
        float startupTimer = 0f;
        while (startupTimer < startupTime)
        {
            p.rb.linearVelocity = Vector2.zero;

            startupTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //찌르기 이펙트 생성
        SpawnThrustEffect(p, dir);

        //실제 동작
        float timer = 0f;
        while (timer < activeTime)
        {
            p.rb.linearVelocity = Vector2.zero;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //후딜
        float recoveryTimer = 0f;
        while (recoveryTimer < recoveryTime)
        {
            p.rb.linearVelocity = Vector2.zero;

            recoveryTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        //중력 재가동
        p.SetPhysicsFreeze(false);


    }

    //찌르기 이펙트 소환하기
    private void SpawnThrustEffect(Player p, float dir)
    {
        if (thrustEffectPrefab == null) return;

        Vector3 spawnPos = p.transform.position + new Vector3(effectOffset.x * dir, effectOffset.y, 0f);

        Quaternion rotation = Quaternion.Euler(0f, 0f, effectRotationZ * dir);

        GameObject effectObj = Instantiate(thrustEffectPrefab, spawnPos, rotation);

        effectObj.transform.localScale = effectScale;

        //플레이어가 바라보는 방향으로
        SpriteRenderer sr = effectObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = dir < 0f;

        //히트박스 생성
        AttackEffectHitbox hitbox = effectObj.GetComponentInChildren<AttackEffectHitbox>();
        if (hitbox != null) hitbox.SetAttackInfo(damageMultiplier, dir);
        
        //이펙트 삭제
        Destroy(effectObj, effectLifeTime);


    }
}
