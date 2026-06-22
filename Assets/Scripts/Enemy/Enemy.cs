using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float decisionTime = 1f;

    [Header("Health")]
    public float maxHp = 3f;
    private float currentHp;

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
    }

    //오브젝트 풀링에서 소환될때마다 호출
    void OnEnable()
    {
        isDead = false;

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
        //y축 유지, x축만바꾸기
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;

        Debug.Log($"Enemy Hit! HP: {currentHp}");

        if (currentHp <= 0f)
        {
            Die();
        }
    }


    public void Die()
    {
        //(선택 사항) 죽는 이펙트 소환
        //ParticleManager.Instance.Play("DeathEffect", transform.position);

        //(선택 사항) 아이템 드랍이나 점수 추가
        // ScoreManager.Instance.AddScore(100);

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