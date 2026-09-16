using System;
using System.Collections;
using UnityEngine;

//적의 피격 경직, 넉백, 넉백 중 절벽 방지를 담당하는 스크립트
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float hitStunTime = 0.2f;
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float knockbackUpForce = 0f;

    [Header("Knockback Ground Check")]
    [SerializeField] private Transform backGroundCheck;
    [SerializeField] private float backGroundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;

    private int knockbackDirection;
    private float knockbackEndTime = -999f;

    private float backGroundCheckX;

    //현재 넉백/피격 경직 중인지
    public bool IsKnockbackActive => Time.time < knockbackEndTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (backGroundCheck != null)
        {
            backGroundCheckX = Mathf.Abs(backGroundCheck.localPosition.x);
        }
    }

    private void OnEnable()
    {
        ResetKnockback();
    }

    private void FixedUpdate()
    {
        if (!IsKnockbackActive) return;

        if (rb == null) return;

        bool hasGround = IsGroundBehind();

        if (!hasGround)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            return;
        }

        rb.linearVelocity = new Vector2(knockbackDirection * knockbackForce, rb.linearVelocity.y);
    }


    //넉백 시작
    public void ApplyKnockback(Vector2 hitDirection)
    {
        knockbackDirection = hitDirection.x >= 0f ? 1 : -1;

        knockbackEndTime = Time.time + hitStunTime;

        //BackGroundCheck를 실제 넉백 방향 쪽으로 이동
        if (backGroundCheck != null)
        {
            Vector3 pos = backGroundCheck.localPosition;

            pos.x = backGroundCheckX * knockbackDirection;

            backGroundCheck.localPosition = pos;
        }


        //맞는 순간에도 즉시 넉백 적용
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                knockbackDirection * knockbackForce,
                knockbackUpForce
            );
        }
    }

    private bool IsGroundBehind()
    {
        if (backGroundCheck == null) return true;

        RaycastHit2D hit =
            Physics2D.Raycast(
                backGroundCheck.position,
                Vector2.down,
                backGroundCheckDistance,
                groundLayer
            );

        bool hasGround = hit.collider != null;

        return hasGround;
    }

    public void StopKnockback()
    {
        knockbackEndTime = -999f;
    }


    private void ResetKnockback()
    {
        knockbackDirection = 0;
        knockbackEndTime = -999f;
    }
}