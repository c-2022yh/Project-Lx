using UnityEngine;
using System.Collections;

public class EnemyMove : MonoBehaviour
{
    public float moveSpeed = 3f; //이동 속도
    public float decisionTime = 1f; //방향 전환 주기

    private Rigidbody2D rb;

    private int direction = 1;
    // 1 오른쪽
    // 0 정지
    // -1 왼쪽
    
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //2초마다 방향을 변경하는 코루틴 시작
        StartCoroutine(DecideAction());
    }

    IEnumerator DecideAction()
    {
        while (true)
        {
            //-1, 0, 1 랜덤 선택
            direction = Random.Range(-1, 2);

            yield return new WaitForSeconds(decisionTime);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    void Update()
    {
        
    }
}
