using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float decisionTime = 1f;

    [Header("Health")] //체력 설정
    public float maxHp = 3f; //최대 체력
    private float currentHp; //현재 체력

    [Header("Hit Feedback")] //피격 관련 판정 변수 //컬러설정은 삭제예정
    [SerializeField] private Color hitColor = Color.white; 
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float hitStunTime = 0.12f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackUpForce = 1f;

    private Color originalColor;
    private bool isHitStunned = false;
    private Coroutine hitFeedbackCoroutine;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int direction = 1;
    private Coroutine decisionCoroutine;

    public PlayerEnergy playerEnergy;
    public float energyReward = 20f;

    private bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null) originalColor = sr.color;
    }

    //오브젝트 풀링에서 소환될때마다 호출
    void OnEnable()
    {
        isDead = false;
        isHitStunned = false;
        currentHp = maxHp;

        if (sr != null) sr.color = originalColor;

        if (playerEnergy == null)
        {
            playerEnergy = FindAnyObjectByType<PlayerEnergy>();
        }
        currentHp = maxHp;

        rb.linearVelocity = Vector2.zero;

        //코루틴 안전하게 다시 시작
        if (decisionCoroutine != null) StopCoroutine(decisionCoroutine);
        decisionCoroutine = StartCoroutine(DecideAction());
    }

    IEnumerator DecideAction()
    {
        while (true)
        {
            direction = UnityEngine.Random.Range(-1, 2);

            if (direction != 0)
            {
                sr.flipX = (direction == -1); //1이면 오른쪽(false), -1이면 왼쪽(true)
            }

            yield return new WaitForSeconds(decisionTime);
        }
    }

    void FixedUpdate()
    {
        
        if (isDead) return;
        if (isHitStunned) return; //넉백 중에는 안움직이게

        //y축 유지, x축만바꾸기
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    //데미지 입음 -> 색 변환, 넉백
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isDead) return; //죽으면 바로 종료

        currentHp -= damage;
        Debug.Log($"Enemy Hit! HP: {currentHp}");
        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        if (hitFeedbackCoroutine != null) //넉백 중이면 넉백 취소하고 한번 더 밀리게
        {
            StopCoroutine(hitFeedbackCoroutine);
        }

        hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine(hitDirection));


    }

    //넉백 코루틴
    private IEnumerator HitFeedbackRoutine(Vector2 hitDirection)
    {
        isHitStunned = true;

        // 넉백
        if (rb != null)
        {
            float dir = hitDirection.x >= 0f ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * knockbackForce, knockbackUpForce);
        }

        //색 깜빡임 잠깐 기다렸다 기존 색으로 복귀
        if (sr != null) sr.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        if (sr != null) sr.color = originalColor;

        float remainStunTime = hitStunTime - hitFlashTime; //넉백하고 다시 돌아오는 시간
        if (remainStunTime > 0f)
            yield return new WaitForSeconds(remainStunTime);

        isHitStunned = false;
        hitFeedbackCoroutine = null;

    }


    //죽음
    public void Die()
    {

        if (isDead) return;
        isDead = true;

        if (playerEnergy != null)
        {
            playerEnergy.GainEnergy(energyReward);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        gameObject.SetActive(false);

    }
}