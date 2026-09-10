using System;
using System.Collections;
using UnityEngine;

// 적 스프라이트 애니메이션 관리
public class EnemyAnimation : MonoBehaviour
{
    [Header("Walk Animation")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float walkFrameTime = 0.12f;

    [Header("Death Animation")]
    [SerializeField] private Sprite[] deathSprites;
    [SerializeField] private float deathFrameTime = 0.12f;
    [SerializeField] private float deathEndHoldTime = 0.08f;

    private SpriteRenderer sr;
    private EnemyAI ai;
    private EnemyHealth health;

    private Coroutine walkCoroutine;
    private Coroutine deathCoroutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ai = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        ResetAnimation();

        walkCoroutine = StartCoroutine(WalkAnimationRoutine());
    }

    private void OnDisable()
    {
        walkCoroutine = null;
        deathCoroutine = null;
    }

    private void ResetAnimation()
    {
        if (sr == null) return;

        sr.enabled = true;
        sr.flipX = false;

        if (walkSprites != null && walkSprites.Length > 0)
        {
            sr.sprite = walkSprites[0];
        }
    }

    //방향 설정
    public void SetDirection(int direction)
    {
        if (direction == 0) return;
        sr.flipX = direction > 0;
    }

    //걷는 애니메이션 재생
    private IEnumerator WalkAnimationRoutine()
    {
        if (walkSprites == null || walkSprites.Length == 0)
        {
            yield break;
        }

        int frameIndex = 0;

        while (!health.IsDead)
        {
            if (sr == null) yield break;

            if (ai.Direction != 0 && !health.IsHitStunned)
            {
                sr.sprite = walkSprites[frameIndex];

                frameIndex++;

                if (frameIndex >= walkSprites.Length)
                {
                    frameIndex = 0;
                }
            }
            else if (ai.Direction == 0)
            {
                sr.sprite = walkSprites[0];
                frameIndex = 0;
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, walkFrameTime));
        }
    }

    //사망했을 때
    public void PlayDeath(System.Action onComplete)
    {
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