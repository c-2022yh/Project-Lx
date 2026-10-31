using System;
using UnityEngine;

//플레이어 체력을 관리하는 스크립트
public class PlayerHealth : MonoBehaviour, IDamageable
{
    //체력 변화와 사망 이벤트
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;
    //무적 검사를 통과하고 실제 체력이 감소했을 때 화면 효과에 알림
    public event Action<float> OnDamaged;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    //체력
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float currentHealth;

    [Header("Audio")]
    [SerializeField] private SoundCue hurtSound = new SoundCue();
    [SerializeField] private SoundCue deathSound = new SoundCue();

    private PlayerStats playerStats;
    private PlayerHitReaction hitReaction;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        hitReaction = GetComponent<PlayerHitReaction>();

        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(DamageInfo damageInfo, Vector2 hitDirection)
    {
        if (IsDead) return;

        if (hitReaction != null && !hitReaction.CanTakeDamage()) return;

        DefensiveStats defensiveStats = playerStats != null ? playerStats.Defense : null;
        float defense = damageInfo.damageType switch
        {
            DamageType.Physical => defensiveStats?.physicalDefense ?? 0f,
            DamageType.Magical => defensiveStats?.magicalDefense ?? 0f,
            _ => 0f
        };

        float damage = DamageCalculator.Calculate(
            damageInfo,
            defense,
            defensiveStats?.durability ?? 0f
        );

        ApplyDamage(damage, damageInfo.attacker.transform.position, damageInfo.impactAudio);
    }

    //데미지 입음
    public void TakeDamage(float damage, Vector2 damageSourcePosition)
    {
        ApplyDamage(damage, damageSourcePosition, null);
    }

    // Confirm impact audio before damage/death callbacks can change the player state.
    private void ApplyDamage(float damage, Vector2 damageSourcePosition, AttackAudioContext impactAudio)
    {
        if (damage <= 0f) return;
        if (IsDead) return;

        if (hitReaction != null && !hitReaction.CanTakeDamage()) return;

        float appliedDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (IsDead) GameAudio.StopOwnedSounds(transform);
        GameAudio.Play(IsDead ? deathSound : hurtSound, transform.position, transform);
        impactAudio?.OnDamageApplied(transform);
        OnDamaged?.Invoke(appliedDamage);

        Debug.Log("Player damaged. HP: " + currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
        {
            OnDied?.Invoke();
            return;
        }

        if (hitReaction != null)
        {
            hitReaction.PlayHitReaction(damageSourcePosition);
        }
    }

    //회복
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        if (IsDead) return;
        if (currentHealth >= maxHealth) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //전체 회복
    public void FullHeal()
    {
        if (IsDead) return;

        currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //리스폰 시 체력 회복
    public void RestoreHealthOnRespawn()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //최대 체력 증가
    public void AddMaxHealth(int amount, bool healAddedAmount = true)
    {
        if (amount <= 0) return;

        maxHealth += amount;

        if (healAddedAmount && !IsDead)
        {
            currentHealth += amount;
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //최대 체력 감소
    public void RemoveMaxHealth(int amount)
    {
        if (amount <= 0) return;

        maxHealth -= amount;

        if (maxHealth < 1f)
        {
            maxHealth = 1f;
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
