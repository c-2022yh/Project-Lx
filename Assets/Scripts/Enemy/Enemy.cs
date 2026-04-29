using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float decisionTime = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int direction = 1;
    private Coroutine decisionCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    //오브젝트 풀링에서 소환될때마다 호출
    void OnEnable()
    {
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

    public void Die()
    {
        //(선택 사항) 죽는 이펙트 소환
        //ParticleManager.Instance.Play("DeathEffect", transform.position);

        //(선택 사항) 아이템 드랍이나 점수 추가
        // ScoreManager.Instance.AddScore(100);

        //상태 초기화
        //다시 풀에서 꺼내질 때를 대비해 물리 속도를 0으로 만듭니다.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        gameObject.SetActive(false);

    }
}