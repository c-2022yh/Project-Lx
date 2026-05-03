using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject weaponHandle;
    [SerializeField] private Collider2D swordCollider;

    [Header("Settings")]
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float defaultAngle = 20f;
    [SerializeField] private float startAngle = -80f;
    [SerializeField] private float endAngle = 40f;

    private bool isAttacking = false;

    void Awake()
    {
        if (swordCollider != null) swordCollider.enabled = false;

        weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 공격 시작: 판정 On
        if (swordCollider != null) swordCollider.enabled = true;

        float elapsed = 0f;

        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / attackDuration;

            // 회전값 계산
            float currentAngle = Mathf.Lerp(startAngle, endAngle, progress);
            weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, currentAngle);

            // 중요: 다음 프레임까지 대기 (이게 없으면 회전 애니메이션이 안 보임)
            yield return null;
        }

        // 공격 종료: 판정 Off 및 각도 복구
        if (swordCollider != null) swordCollider.enabled = false;
        weaponHandle.transform.localRotation = Quaternion.Euler(0, 0, defaultAngle);

        isAttacking = false;
    }
}