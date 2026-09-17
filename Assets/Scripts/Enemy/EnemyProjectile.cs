using System.Collections.Generic;
using UnityEngine;

//적이 발사하는 투사체
public class EnemyProjectile : MonoBehaviour
{
    private Rigidbody2D rb;

    //이동
    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private float rotationSpeed;


    //생성 위치
    private Vector2 startPosition;

    //데미지
    private DamageInfo damageInfo;
    private LayerMask playerLayer;
    private bool destroyOnPlayerHit;


    //관통 투사체일 경우
    //같은 대상을 여러 번 때리는 것 방지
    private readonly HashSet<IDamageable> hitTargets = new();

    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    public void Init(
        Vector2 direction,
        float speed,
        float maxDistance,
        float rotationSpeed,
        DamageInfo damageInfo,
        LayerMask playerLayer,
        bool destroyOnPlayerHit)
    {
        this.direction = direction.normalized;

        this.speed = speed;
        this.maxDistance = maxDistance;
        this.rotationSpeed = rotationSpeed;

        this.damageInfo = damageInfo;

        this.playerLayer = playerLayer;

        this.destroyOnPlayerHit = destroyOnPlayerHit;


        startPosition = transform.position;

        hitTargets.Clear();

        initialized = true;

        //이동 시작
        if (rb != null)
        {
            rb.linearVelocity = this.direction * this.speed;
        }


        //투사체의 오른쪽 방향을
        //실제 발사 방향으로 맞춤
        if (this.direction.sqrMagnitude > 0f)
        {
            float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }


    private void Update()
    {
        if (!initialized) return;

        //최대 이동거리 확인
        Vector2 currentPosition = transform.position;

        float distanceSqr = (currentPosition - startPosition).sqrMagnitude;

        if (distanceSqr >= maxDistance * maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        //투사체 회전
        if (rotationSpeed != 0f)
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;

        //플레이어 레이어인지 확인
        bool isPlayerLayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;

        if (!isPlayerLayer) return;

        //피격 가능한 대상 찾기
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        //같은 투사체가 같은 대상을 여러 번 때리는 것 방지
        if (!hitTargets.Add(target)) return;


        //데미지 전달
        target.TakeDamage(damageInfo, direction);


        //일반 투사체라면 적중 후 제거
        if (destroyOnPlayerHit)
        {
            Destroy(gameObject);
        }
    }
}