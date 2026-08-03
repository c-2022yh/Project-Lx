using System.Collections.Generic;
using UnityEngine;

//투사체에 들어갈 스크립트
public class ProjectileHitbox : MonoBehaviour
{
    private float dir;              //방향
    private float speed;            //속도   
    private float maxDistance;      //사거리
    private float rotationSpeed;    //회전 속도
    private DamageInfo damageInfo; //데미지  
    private LayerMask enemyLayer;   //적을 식별할 레이어마스크
    private bool destroyOnEnemyHit; //적이 맞았는지 체크

    private Vector2 startPos;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private bool initialized;

    //이 공격 이펙트가 이미 타격한 적들
    private readonly HashSet<IDamageable> hitTargets = new();

    //값을 받아오는 함수
    public void Init(float dir, float speed, float maxDistance, float rotationSpeed,
        DamageInfo damageInfo, LayerMask enemyLayer, bool destroyOnEnemyHit)
    {
        this.dir = dir;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.rotationSpeed = rotationSpeed;
        this.damageInfo = damageInfo;
        this.enemyLayer = enemyLayer;
        this.destroyOnEnemyHit = destroyOnEnemyHit;

        startPos = transform.position;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) spriteRenderer.flipX = dir < 0f;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dir * speed, 0f);
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        //Rigidbody2D가 없을 경우를 대비
        if (rb == null)  transform.position += Vector3.right * dir * speed * Time.deltaTime;
        
        //투사체 회전
        transform.Rotate(0f, 0f, rotationSpeed * dir * Time.deltaTime);

        //일정 거리 이상 날아가면 삭제
        float distance = Vector2.Distance(startPos, transform.position);
        if (distance >= maxDistance) Destroy(gameObject);
        

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;

        //비밀벽 타일 처리
        SecretBreakableWall secretBreakableWall = other.GetComponentInParent<SecretBreakableWall>();

        if (secretBreakableWall != null)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            secretBreakableWall.HitWallAtWorldPosition(hitPoint);
            Destroy(gameObject);
            return;
        }

        //공통 피격 대상 탐색
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        //같은 대상은 이 히트박스에 한 번만 피격
        if (!hitTargets.Add(target)) return;

        //피해 정보 전달
        target.TakeDamage(damageInfo, new Vector2(dir, 0f));


        //맞으면 삭제
        if (destroyOnEnemyHit)
        {
            Destroy(gameObject);
        }


    }
}