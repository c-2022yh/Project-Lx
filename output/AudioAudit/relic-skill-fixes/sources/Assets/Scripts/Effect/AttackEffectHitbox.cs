using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AttackEffectHitbox : MonoBehaviour
{
    //히트박스가 전달할 피해 정보
    private DamageInfo damageInfo;
    private DamageInfo followUpDamage;
    private float followUpDelay;
    private MonoBehaviour followUpRunner;
    private bool hasFollowUp;

    //공격 방향
    private float attackDirection = 1f;

    //이 공격 이펙트가 이미 타격한 적들
    private readonly HashSet<IDamageable> hitTargets = new();

    //공격 이펙트의 콜라이더 (실제 피격 판정)
    [SerializeField] private Collider2D hitCollider;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject criticalHitEffectPrefab;

    //이 공격이 적중했을 때 실행할 선택적 콜백
    //기본공격이나 포식 X 스킬에서 사용
    private Action<IDamageable, DamageInfo> onHitConfirmed;

    private void Awake()
    {
        //콜라이더 연결
        hitCollider = GetComponent<Collider2D>();
    }

    //공격 정보 설정
    public void SetAttackInfo(DamageInfo damageInfo, float direction, Action<IDamageable, DamageInfo> onHitConfirmed = null)
    {
        this.damageInfo = damageInfo;
        hitTargets.Clear();
        hasFollowUp = false;
        followUpRunner = null;
        attackDirection = direction >= 0f ? 1f : -1f;
        this.onHitConfirmed = onHitConfirmed;
    }

    public void SetFollowUpHit(DamageInfo damage, float delay, MonoBehaviour runner)
    {
        followUpDamage = damage;
        followUpDelay = Mathf.Max(0f, delay);
        followUpRunner = runner;
        hasFollowUp = runner != null;
    }

    //공격 히트박스 해제
    public void DisableHitbox()
    {
        hitCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        //비밀벽 타일 처리
        SecretBreakableWall secretBreakableWall = other.GetComponentInParent<SecretBreakableWall>();
        if (secretBreakableWall != null)
        {
            Vector2 wallHitPoint = other.ClosestPoint(transform.position);
            secretBreakableWall.HitWallAtWorldPosition(wallHitPoint);
            return;
        }

        //파괴 가능한 벽돌 처리
        BreakableBrick breakableBrick = other.GetComponentInParent<BreakableBrick>();

        if (breakableBrick != null)
        {
            breakableBrick.HitBrick();
            return;
        }


        //공통 피격 대상 탐색
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        //같은 대상은 이 히트박스에 한 번만 피격
        if (!hitTargets.Add(target)) return;

        //피격 위치 계산
        Vector2 hitPoint = other.bounds.center;

        //피해 정보 전달
        ApplyHit(target, damageInfo, attackDirection, hitPoint,
            hitEffectPrefab, criticalHitEffectPrefab, onHitConfirmed);

        if (hasFollowUp && followUpRunner != null)
            followUpRunner.StartCoroutine(FollowUpRoutine(target, followUpDamage, attackDirection,
                followUpDelay, hitEffectPrefab, criticalHitEffectPrefab, onHitConfirmed));
    }

    // Runs on the attacker so an already-confirmed hit survives the trail's short lifetime.
    private static IEnumerator FollowUpRoutine(IDamageable target, DamageInfo damage, float direction,
        float delay, GameObject normalEffect, GameObject criticalEffect, Action<IDamageable, DamageInfo> callback)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Component component = target as Component;
        if (component == null || !component.gameObject.activeInHierarchy) yield break;
        if (target is Enemy enemy && enemy.IsDead) yield break;
        if (target is EnemyHealth health && health.IsDead) yield break;

        ApplyHit(target, damage, direction, component.transform.position,
            normalEffect, criticalEffect, callback);
    }

    private static void ApplyHit(IDamageable target, DamageInfo damage, float direction, Vector2 hitPoint,
        GameObject normalEffect, GameObject criticalEffect, Action<IDamageable, DamageInfo> callback)
    {
        target.TakeDamage(damage, new Vector2(direction, 0f));
        CombatFeedback.OnPlayerHit(target, direction, damage.isCritical);
        GameObject effect = damage.isCritical ? criticalEffect : normalEffect;
        if (effect != null) Instantiate(effect, hitPoint, Quaternion.identity);
        callback?.Invoke(target, damage);
    }
}
