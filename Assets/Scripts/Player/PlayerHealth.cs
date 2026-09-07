using System;
using UnityEngine;

//플레이어 체력을 관리하는 스크립트
public class PlayerHealth : MonoBehaviour
{
    //체력 변화와 사망 이벤트
    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    //체력
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float currentHealth;

    private PlayerHitReaction hitReaction;

    private void Awake()
    {
        hitReaction = GetComponent<PlayerHitReaction>();

        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //데미지 입음
    public void TakeDamage(float damage, Vector2 damageSourcePosition)
    {
        if (damage <= 0f) return;
        if (IsDead) return;

        if (hitReaction != null && !hitReaction.CanTakeDamage())
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

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