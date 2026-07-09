using UnityEngine;

//공통 피격 인터페이스
public interface IDamageable
{
    //넉백 방향도 필요하니 Vector2 hitDirection을 추가
    void TakeDamage(DamageInfo damageInfo, Vector2 hitDirection);
}
