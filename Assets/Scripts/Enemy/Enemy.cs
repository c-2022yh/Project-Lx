using UnityEngine;

//적 전체를 정의하는 스크립트
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyAI AI { get; private set; }
    public EnemyHealth Health { get; private set; }
    public EnemyAnimation Animation { get; private set; }

    public EnemyStats Stats { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Sr { get; private set; }

    public bool IsDead => Health != null && Health.IsDead;

    private void Awake()
    {
        AI = GetComponent<EnemyAI>();
        Health = GetComponent<EnemyHealth>();
        Animation = GetComponent<EnemyAnimation>();

        Stats = GetComponent<EnemyStats>();
        Rb = GetComponent<Rigidbody2D>();
        Sr = GetComponent<SpriteRenderer>();
    }

    //외부 공격 시스템에서 Enemy만 공격해도 됨
    public void TakeDamage(DamageInfo damageInfo, Vector2 hitDirection)
    {
        Health.TakeDamage(damageInfo, hitDirection);
    }
}