using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;

private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("HealthHUD: PlayerHealth를 찾을 수 없습니다.");
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

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthFill == null)
        {
            Debug.LogError("HealthHUD: HealthBar_Fill Image가 연결되지 않았습니다.");
            return;
        }

        healthFill.fillAmount = (float)currentHealth / maxHealth;
    }


}
