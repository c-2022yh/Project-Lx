
using System.Collections;
using TMPro;
using UnityEngine;

//적 피격 시 나타나는 데미지 숫자
public class DamagePopup : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text damageText;

    [Header("Damage Colors")]
    [SerializeField] private Color physicalDamageColor = new Color(1f, 0.94f, 0.8f, 1f);
    [SerializeField] private Color magicalDamageColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("Critical Hit")]
    [Tooltip("Critical text size relative to normal damage text.")]
    [SerializeField, Min(1f)] private float criticalSizeMultiplier = 1.3f;
    [Tooltip("Extra peak scale during the critical pop animation. Set to 1 to disable.")]
    [SerializeField, Min(1f)] private float criticalPopMultiplier = 1.2f;
    [Tooltip("Seconds for the critical text to grow and return. Set to 0 to disable.")]
    [SerializeField, Min(0f)] private float criticalPopDuration = 0.15f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float lifeTime = 0.7f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float randomXOffset = 0.15f;

    private Vector3 originalTextScale;

    private void Awake()
    {
        if (damageText != null)
        {
            originalTextScale = damageText.transform.localScale;
        }
    }


    //데미지 숫자 표시
    public void Show(float damage, DamageType damageType, bool isCritical)
    {
        if (damageText == null)
        {
            Destroy(gameObject);
            return;
        }

        // Round the displayed number only; midpoint values round up for positive damage.
        damageText.text = System.Math.Round(damage, System.MidpointRounding.AwayFromZero).ToString("0");
        damageText.color = damageType == DamageType.Magical
            ? magicalDamageColor
            : physicalDamageColor;

        Vector3 targetScale = originalTextScale * (isCritical ? criticalSizeMultiplier : 1f);
        damageText.transform.localScale = targetScale;

        //숫자가 겹치지 않도록 약간의 위치 차이
        transform.position += new Vector3(UnityEngine.Random.Range(-randomXOffset, randomXOffset), 0f, 0f);

        StartCoroutine(PopupRoutine(isCritical, targetScale));
    }


    private IEnumerator PopupRoutine(bool isCritical, Vector3 targetScale)
    {
        float timer = 0f;

        Color originalColor = damageText.color;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;

            //위로 이동
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            //점점 투명해짐
            float alpha = originalColor.a * Mathf.Clamp01(1f - timer / lifeTime);

            damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // Keep the damage type color while emphasizing critical hits with scale.
            if (isCritical)
            {
                float popScale = 1f;
                if (criticalPopDuration > 0f && timer < criticalPopDuration)
                {
                    float progress = timer / criticalPopDuration;
                    popScale += (criticalPopMultiplier - 1f) * Mathf.Sin(progress * Mathf.PI);
                }

                damageText.transform.localScale = targetScale * popScale;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}