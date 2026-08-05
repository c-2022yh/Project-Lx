using System;
using System.Collections;
using TMPro;
using UnityEngine;

//적 피격 시 나타나는 데미지 숫자
public class DamagePopup : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text damageText;

    [Header("Animation")]
    [SerializeField] private float lifeTime = 0.7f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float randomXOffset = 0.15f;


    //데미지 숫자 표시
    public void Show(float damage)
    {
        if (damageText == null)
        {
            Destroy(gameObject);
            return;
        }

        //소수점 둘째 자리까지 표시
        //1은 1, 1.25는 1.25로 표시
        damageText.text = damage.ToString("0.##");

        //숫자가 겹치지 않도록 약간의 위치 차이
        transform.position += new Vector3(UnityEngine.Random.Range(-randomXOffset, randomXOffset), 0f, 0f);

        StartCoroutine(PopupRoutine());
    }


    private IEnumerator PopupRoutine()
    {
        float timer = 0f;

        Color originalColor = damageText.color;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;

            //위로 이동
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            //점점 투명해짐
            float alpha = 1f - timer / lifeTime;

            damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}