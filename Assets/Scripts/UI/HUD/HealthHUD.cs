
using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    private global::ProjectLx.PlayerRuntime.PlayerHealthComponent playerHealth;

    [SerializeField] private Image healthFill;

    private void Start()
    {
        playerHealth = FindAnyObjectByType<global::ProjectLx.PlayerRuntime.PlayerHealthComponent>();

        if (playerHealth == null)
        {
            Debug.LogError("HealthHUD: PlayerHealth를 찾을 수 없습니다.", this);
            return;
        }

        playerHealth.OnHealthChanged += UpdateHealthBar;

        UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthFill == null) return;
        

        healthFill.fillAmount = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    }
}