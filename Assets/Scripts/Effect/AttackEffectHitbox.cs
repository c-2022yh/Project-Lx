using UnityEngine;

public class AttackEffectHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    public void SetDamage(float value)
    {
        damage = value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);
    }
}