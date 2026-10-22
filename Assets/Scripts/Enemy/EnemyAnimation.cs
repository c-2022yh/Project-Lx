using System;
using System.Collections;
using UnityEngine;

// 적 스프라이트 애니메이션 관리
public class EnemyAnimation : MonoBehaviour
{
    [Header("Sprite Direction")]
    [Tooltip("Enable if the original, unflipped sprites face right.")]
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Idle Animation")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField, Min(0.01f)] private float idleFrameTime = 0.12f;

    [Header("Shoot Animation")]
    [SerializeField] private Sprite[] shootSprites;
    [SerializeField, Min(0.01f)] private float shootFrameTime = 0.12f;

    [Header("Walk Animation")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float walkFrameTime = 0.12f;

    [Header("Flying Charge Animation")]
    [SerializeField] private Sprite prepareSprite;
    [SerializeField] private Sprite[] dashSprites;
    [SerializeField, Min(0.01f)] private float dashFrameTime = 0.1f;

    [Header("Death Animation")]
    [SerializeField] private Sprite[] deathSprites;
    [SerializeField] private float deathFrameTime = 0.12f;
    [SerializeField] private float deathEndHoldTime = 0.08f;

    private SpriteRenderer sr;
    private EnemyAI ai;
    private EnemyHealth health;
    private EnemyKnockback knockback;

    private Coroutine walkCoroutine;
    private Coroutine deathCoroutine;
    private Coroutine shootCoroutine;
    private Coroutine dashCoroutine;
    private bool isCharging;
    private bool isShooting;
    private bool usesIdleAnimation;

    public float ShootDuration => shootSprites == null
        ? 0f : shootSprites.Length * Mathf.Max(0.01f, shootFrameTime);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
        knockback = GetComponent<EnemyKnockback>();
        usesIdleAnimation = ai == null || GetComponent<EnemyRangedAI>() != null;
    }

    private void OnEnable()
    {
        ResetAnimation();

        walkCoroutine = StartCoroutine(WalkAnimationRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        walkCoroutine = null;
        deathCoroutine = null;
        shootCoroutine = null;
        isShooting = false;
        dashCoroutine = null;
        isCharging = false;
    }

    private void ResetAnimation()
    {
        if (sr == null) return;

        sr.enabled = true;
        sr.flipX = false;
        if (ai != null) SetDirection(-1);

        Sprite[] frames = GetBaseSprites();
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    //방향 설정
    public void SetDirection(int direction)
    {
        if (direction == 0) return;

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
        } 

        sr.flipX = spriteFacesRight ? direction < 0 : direction > 0;
    }

    //걷는 애니메이션 재생
    private Sprite[] GetBaseSprites()
    {
        return usesIdleAnimation && idleSprites != null && idleSprites.Length > 0
            ? idleSprites : walkSprites;
    }

    private IEnumerator WalkAnimationRoutine()
    {
        Sprite[] frames = GetBaseSprites();
        if (frames == null || frames.Length == 0) yield break;

        float frameTime = usesIdleAnimation && idleSprites != null && idleSprites.Length > 0
            ? idleFrameTime : walkFrameTime;
        int frameIndex = 0;
        while (health == null || !health.IsDead)
        {
            if (sr == null) yield break;
            if (isShooting || isCharging)
            {
                yield return null;
                continue;
            }

            if ((usesIdleAnimation || ai == null || ai.Direction != 0) &&
                (knockback == null || !knockback.IsKnockbackActive))
            {
                sr.sprite = frames[frameIndex];
                frameIndex = (frameIndex + 1) % frames.Length;
            }
            else if (ai != null && ai.Direction == 0)
            {
                sr.sprite = frames[0];
                frameIndex = 0;
            }
            yield return new WaitForSeconds(Mathf.Max(0.01f, frameTime));
        }
    }

    public void PlayPrepare()
    {
        StopCharge();
        if (!CanPlayCharge() || prepareSprite == null) return;
        isCharging = true;
        sr.sprite = prepareSprite;
    }

    public void PlayDash()
    {
        StopCharge();
        if (!CanPlayCharge() || dashSprites == null || dashSprites.Length == 0) return;
        isCharging = true;
        dashCoroutine = StartCoroutine(DashAnimationRoutine());
    }

    private bool CanPlayCharge()
    {
        return isActiveAndEnabled && sr != null &&
            (health == null || !health.IsDead) && deathCoroutine == null;
    }

    public void StopCharge()
    {
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = null;
        bool wasCharging = isCharging;
        isCharging = false;
        if (wasCharging && CanPlayCharge())
        {
            Sprite[] frames = GetBaseSprites();
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
        }
    }

    private IEnumerator DashAnimationRoutine()
    {
        int index = 0;
        while (CanPlayCharge())
        {
            sr.sprite = dashSprites[index];
            index = (index + 1) % dashSprites.Length;
            yield return new WaitForSeconds(Mathf.Max(0.01f, dashFrameTime));
        }
        dashCoroutine = null;
        isCharging = false;
    }

    public void PlayShoot()
    {
        if (!isActiveAndEnabled || sr == null || (health != null && health.IsDead) ||
            deathCoroutine != null || shootSprites == null || shootSprites.Length == 0) return;
        StopShoot();
        shootCoroutine = StartCoroutine(ShootAnimationRoutine());
    }

    public void StopShoot()
    {
        if (shootCoroutine != null) StopCoroutine(shootCoroutine);
        shootCoroutine = null;
        isShooting = false;
    }

    private IEnumerator ShootAnimationRoutine()
    {
        isShooting = true;
        foreach (Sprite frame in shootSprites)
        {
            if (sr == null || (health != null && health.IsDead)) break;
            sr.sprite = frame;
            yield return new WaitForSeconds(Mathf.Max(0.01f, shootFrameTime));
        }
        shootCoroutine = null;
        // Keep the last shooting frame until the attack releases it with StopShoot().
    }

    public void PlayDeath(System.Action onComplete)
    {
        StopCharge();
        StopShoot();
        if (walkCoroutine != null)
        {
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
        }

        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        deathCoroutine = StartCoroutine(DeathAnimationRoutine(onComplete));
    }

    //사망 애니메이션 재생
    private IEnumerator DeathAnimationRoutine(Action onComplete)
    {
        if (deathSprites == null || deathSprites.Length == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        float frameTime = Math.Max(0.01f, deathFrameTime);

        foreach (Sprite deathSprite in deathSprites)
        {
            if (sr == null) break;

            sr.sprite = deathSprite;

            yield return new WaitForSeconds(frameTime);
        }
        
        //바로 사망해서 없어지는게 아닌 사망 애니메이션 재생 후 없어지기
        //이때 물리판정은 없음
        if (deathEndHoldTime > 0f)
        {
            yield return new WaitForSeconds(deathEndHoldTime);
        }

        deathCoroutine = null;

        onComplete?.Invoke();
    }
}