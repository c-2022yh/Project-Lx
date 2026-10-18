using System.Collections;
using UnityEngine;

//원거리 적의 투사체 공격을 담당
public class EnemyRangedAttack : MonoBehaviour
{
    [Header("Attack")]

    [SerializeField] private AttackDamageSpec damageSpec;

    //공격 전 선딜
    [SerializeField] private float attackDelay = 0.3f;

    //공격 간격
    [SerializeField] private float attackCooldown = 1.5f;


    [Header("Projectile")]

    [SerializeField] private GameObject projectilePrefab;

    //적 위치 기준 투사체 생성 위치
    [SerializeField]
    private Vector2 spawnOffset = new Vector2(0.8f, 0f);

    [SerializeField] private float projectileSpeed = 8f;

    //투사체가 이동 가능한 최대 거리
    [SerializeField] private float maxDistance = 8f;

    //투사체 회전 속도
    [SerializeField] private float rotationSpeed = 0f;

    //플레이어 적중 시 투사체 제거 여부
    [SerializeField] private bool destroyOnPlayerHit = true;

    //플레이어 레이어
    [SerializeField] private LayerMask playerLayer;


    private EnemyStats enemyStats;
    private EnemyHealth enemyHealth;
    private EnemyAI ai;
    private EnemyRangedAI rangedAI;
    private EnemyAnimation enemyAnimation;
    private EnemyKnockback knockback;

    private bool isAttacking;
    private float nextAttackTime;


    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        enemyHealth = GetComponent<EnemyHealth>();
        ai = GetComponent<EnemyAI>();
        rangedAI = GetComponent<EnemyRangedAI>();
        enemyAnimation = GetComponent<EnemyAnimation>();
        knockback = GetComponent<EnemyKnockback>();
    }


    private void OnEnable()
    {
        isAttacking = false;
        nextAttackTime = 0f;
    }

    private bool CanAttack()
    {
        return ai != null && rangedAI != null && enemyStats != null && projectilePrefab != null &&
            (enemyHealth == null || !enemyHealth.IsDead) &&
            (knockback == null || !knockback.IsKnockbackActive) && rangedAI.CanAttackPlayer();
    }

    private void Update()
    {
        if (isAttacking || Time.time < nextAttackTime || !CanAttack()) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        ai.StopMovement();
        enemyAnimation?.PlayShoot();

        float delay = Mathf.Max(0f, attackDelay);
        float animationDuration = enemyAnimation != null ? enemyAnimation.ShootDuration : 0f;
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!CanAttack())
            {
                FinishAttack();
                yield break;
            }
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!CanAttack())
        {
            FinishAttack();
            yield break;
        }

        FireProjectile();
        nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);

        // Finish the shooting pose before returning to idle or starting another shot.
        while (elapsed < animationDuration)
        {
            if (!CanAttack()) break;
            yield return null;
            elapsed += Time.deltaTime;
        }
        FinishAttack();
    }

    private void FinishAttack()
    {
        enemyAnimation?.StopShoot();
        isAttacking = false;
    }


    private void FireProjectile()
    {
        if (projectilePrefab == null) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        if (ai.Player == null) return;

        //현재 바라보는 방향
        float facingDirection = ai.Direction;

        //투사체 생성 위치
        Vector2 spawnPosition =
            (Vector2)transform.position +
            new Vector2(
                spawnOffset.x * facingDirection,
                spawnOffset.y
            );


        // 발사 순간 플레이어 위치를 향하는 방향
        Vector2 direction = ((Vector2)ai.Player.position - spawnPosition).normalized;

        if (direction.sqrMagnitude <= 0.0001f) return;

        //데미지 정보 생성
        DamageInfo damageInfo =
            DamageInfo.Create(
                enemyStats.Offense,
                damageSpec,
                gameObject
            );

        //투사체 생성
        GameObject projectileObj =
            Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();

        //EnemyProjectile이 없는 잘못된 프리팹
        if (projectile == null)
        {
            Destroy(projectileObj);
            return;
        }


        //투사체에 필요한 정보 전달
        projectile.Init(
            direction,
            projectileSpeed,
            maxDistance,
            rotationSpeed,
            damageInfo,
            playerLayer,
            destroyOnPlayerHit
        );
    }


    private void OnDisable()
    {
        StopAllCoroutines();

        FinishAttack();
    }
}