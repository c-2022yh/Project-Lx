using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject swordObject;
    [SerializeField] private float attackDuration = 0.1f;

    private bool isAttacking = false;

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

        if (swordObject != null)
            swordObject.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        if (swordObject != null)
            swordObject.SetActive(false);

        isAttacking = false;
    }
}