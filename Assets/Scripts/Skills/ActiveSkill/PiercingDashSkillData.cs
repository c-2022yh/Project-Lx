using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skills/PiercingDash", menuName = "Skills/PiercingDash")]

//질풍참 류 적을 관통하면서 대쉬베기 스킬
public class PiercingDashSkillData : AttackSkillData
{
    [Header("Dash Attack")]
    public float dashDistance = 3.5f;
    public Vector2 dashHitBoxSize = new Vector2(1.4f, 1.2f);
    public Vector2 dashHitBoxOffset = new Vector2(0.7f, 0f);

    [Header("Dash Effect")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private Vector2 dashEffectOffset = Vector2.zero;

    [Tooltip("기본 Dash Distance 기준으로 제작된 이펙트의 X 스케일")]
    [SerializeField] private float dashEffectBaseScaleX = 1f;

    [Tooltip("이 거리보다 짧으면 쇄도 메인 이펙트를 생성하지 않음")]
    [SerializeField] private float minDashEffectDistance = 0.2f;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject criticalHitEffectPrefab;

    [SerializeField] private Vector3 hitEffectOffset = Vector3.zero;

    //대쉬 후 적과의 피격무적 판정 시간
    [SerializeField] private float contactIgnoreAfterDash = 0.2f;


    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;

        float dir = p.isFacingRight ? 1f : -1f;

        //벽 체크 및 실제 이동 거리 계산
        RaycastHit2D hit = Physics2D.Raycast(
            p.transform.position,
            Vector2.right * dir,
            dashDistance,
            LayerMask.GetMask("Ground")
        );

        float actualDist = hit.collider ? hit.distance : dashDistance;

        PlayerHitReaction hitReaction = p.GetComponent<PlayerHitReaction>();
        hitReaction?.BeginEnemyContactIgnore();

        //해시셋 (한번 충돌한 적은 다시 판정하면 안되므로 해시셋으로 관리)
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

        p.SetPhysicsFreeze(true);

        //공격 정보 생성 
        DamageInfo damageInfo = DamageInfo.Create(
            p.Stats.Offense,
            damageSpec,
            p.gameObject
        );

        //실제 이동 거리에 맞춰 쇄도 이펙트 생성
        SpawnDashEffect(p, dir, actualDist);

        float timer = 0f;
        float speed = actualDist / activeTime;

        while (timer < activeTime)
        {
            //이동 전 현재 위치 판정
            DamageEnemiesDuringDash(
                p,
                dir,
                hitEnemies,
                damageInfo
            );

            //돌진 이동
            p.rb.linearVelocity = new Vector2(
                dir * speed,
                0f
            );

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();

            //이동 후 위치 판정
            DamageEnemiesDuringDash(
                p,
                dir,
                hitEnemies,
                damageInfo
            );
        }

        p.rb.linearVelocity = Vector2.zero;

        //마지막 프레임 판정 보정
        DamageEnemiesDuringDash(
            p,
            dir,
            hitEnemies,
            damageInfo
        );

        p.SetPhysicsFreeze(false);

        //무적 판정 적용
        hitReaction?.EndEnemyContactIgnoreAfterDelay(
            contactIgnoreAfterDash
        );
    }


    //실제 쇄도 이동거리에 맞춰 메인 궤적 이펙트 생성
    private void SpawnDashEffect(Player p, float dir, float actualDist)
    {
        if (dashEffectPrefab == null) return;
        if (actualDist < minDashEffectDistance) return;

        //기본 이동거리 대비 실제 이동거리 비율
        float distanceRatio =
            Mathf.Clamp01(actualDist / dashDistance);

        //쇄도 시작점과 종료점의 중간 위치 계산
        Vector3 startPosition =
            p.transform.position +
            new Vector3(
                dashEffectOffset.x * dir,
                dashEffectOffset.y,
                0f
            );

        Vector3 effectPosition =
            startPosition +
            Vector3.right *
            dir *
            actualDist *
            0.5f;

        GameObject effectObj = Instantiate(
            dashEffectPrefab,
            effectPosition,
            Quaternion.identity
        );

        //벽에 막힌 실제 이동거리에 맞춰 X축 길이 조절
        Vector3 effectScale = effectObj.transform.localScale;

        effectScale.x =
            dashEffectBaseScaleX *
            distanceRatio;

        effectObj.transform.localScale = effectScale;

        //플레이어 방향에 맞춰 이펙트 반전
        SpriteRenderer sr =
            effectObj.GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            sr.flipX = dir < 0f;
        }
    }


    //대쉬 중 적 피격 판정
    private void DamageEnemiesDuringDash(
        Player p,
        float dir,
        HashSet<Enemy> hitEnemies,
        DamageInfo damageInfo)
    {
        //중심부 설정
        Vector2 center =
            (Vector2)p.transform.position +
            new Vector2(
                dashHitBoxOffset.x * dir,
                dashHitBoxOffset.y
            );

        //범위 안에 있는 콜라이더 찾기
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            dashHitBoxSize,
            0f,
            enemyLayer
        );

        //찾은 적마다 콜라이더 돌면서 충돌했는지 검사
        foreach (Collider2D hit in hits)
        {
            //Enemy 스크립트 찾기
            Enemy enemy = hit.GetComponent<Enemy>();

            if (enemy == null)
            {
                enemy = hit.GetComponentInParent<Enemy>();
            }

            //Enemy가 아니면 무시
            if (enemy == null) continue;

            //이미 맞은 적이면 무시
            if (hitEnemies.Contains(enemy)) continue;

            //처음 맞은 적이면 목록에 추가
            hitEnemies.Add(enemy);

            //피격 위치 계산
            Vector2 hitPoint =
                hit.ClosestPoint(center);

            //최종 히트 판정 데미지 적용
            enemy.TakeDamage(
                damageInfo,
                new Vector2(dir, 0f)
            );

            //쇄도 전용 관통 피격 이펙트 생성
            SpawnHitEffect(
                hitPoint,
                damageInfo.isCritical,
                dir
            );

            Debug.Log($"Skill Hit: {enemy.name}");
        }
    }


    //쇄도 관통 피격 이펙트 생성
    private void SpawnHitEffect(
        Vector2 hitPoint,
        bool isCritical,
        float dir)
    {
        GameObject effectPrefab =
            isCritical ?
            criticalHitEffectPrefab :
            hitEffectPrefab;

        if (effectPrefab == null) return;

        Vector3 spawnPosition =
            (Vector3)hitPoint +
            new Vector3(
                hitEffectOffset.x * dir,
                hitEffectOffset.y,
                hitEffectOffset.z
            );

        GameObject effectObj = Instantiate(
            effectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        SpriteRenderer sr =
            effectObj.GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            sr.flipX = dir < 0f;
        }
    }
}