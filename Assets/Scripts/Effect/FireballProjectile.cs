using UnityEngine;

//화염구가 화염 전용 벽에 닿았을 때 처리
//이동과 적 데미지는 기존 ProjectileHitbox에서 담당
public class FireballProjectile : MonoBehaviour
{
    //벽에 닿아 소멸 처리된 화염구인지 확인
    private bool isConsumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isConsumed) return;

        //화염 공격으로 파괴 가능한 벽 탐색
        FireBreakableWall fireBreakableWall = other.GetComponentInParent<FireBreakableWall>();

        if (fireBreakableWall == null) return;

        //여러 콜라이더에 닿아도 한 번만 처리
        isConsumed = true;

        fireBreakableWall.HitWall();

        //벽이 아직 부서지지 않았어도 화염구는 소멸
        Destroy(gameObject);
    }
}
