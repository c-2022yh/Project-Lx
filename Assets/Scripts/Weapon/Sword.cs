using System;
using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    //검에 닿았을 때 발생하는 이벤트
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //닿은 대상의 태그가 "Enemy"라면
        if (collision.CompareTag("Enemy"))
        {
            // 몹 스크립트를 가져와서 죽게 함
            // (EnemyMove나 Enemy 스크립트에 Die 함수가 있다고 가정)
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die();

                // 나중에 여기서 "팍!" 하는 이펙트나 소리를 넣으면 좋습니다.
                Debug.Log("몹 처치!");
            }
        }
    }
}