using System.Collections;
using UnityEngine;

//적의 이동과 행동 판단을 제어하는 스크립트
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float decisionTime = 1f;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private EnemyAnimation enemyAnimation;

    //방향
    private int direction = 1;

    private Coroutine decisionCoroutine;

    public int Direction => direction;
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimation = GetComponent<EnemyAnimation>();
    }

    private void OnEnable()
    {
        direction = 1;

        enemyAnimation?.SetDirection(direction);

        decisionCoroutine =
            StartCoroutine(DecideAction());
    }

    private void OnDisable()
    {
        decisionCoroutine = null;
    }

    private void FixedUpdate()
    {
        if (health == null)
            return;

        if (health.IsDead)
            return;

        if (health.IsHitStunned)
            return;

        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(
            direction * moveSpeed,
            rb.linearVelocity.y
        );
    }

    // 현재는 랜덤하게 좌/정지/우 결정
    private IEnumerator DecideAction()
    {
        while (!health.IsDead)
        {
            SetDirection(
                Random.Range(-1, 2)
            );

            yield return new WaitForSeconds(
                decisionTime
            );
        }
    }

    public void SetDirection(int newDirection)
    {
        direction =
            Mathf.Clamp(newDirection, -1, 1);

        enemyAnimation?.SetDirection(direction);
    }

    public void StopMovement()
    {
        direction = 0;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
        }
    }

    // 사망 시 AI 정지
    public void StopAI()
    {
        if (decisionCoroutine != null)
        {
            StopCoroutine(decisionCoroutine);
            decisionCoroutine = null;
        }

        StopMovement();
    }
}