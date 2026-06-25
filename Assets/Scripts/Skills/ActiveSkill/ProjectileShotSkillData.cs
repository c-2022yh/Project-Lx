using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Skills/ProjectileShot", menuName = "Skills/ProjectileShot")]

//투사체 날리는 스킬 기본형
public class ProjectileShotSkillData : AttackSkillData
{
    [Header("Projectile")]
    public GameObject projectilePrefab;

    //기본 설정
    public Vector2 spawnOffset = new Vector2(0.8f, 0f);
    public float projectileSpeed = 10f;
    public float maxDistance = 6f;
    public float rotationSpeed = 720f;
    public bool destroyOnEnemyHit = true;
    
    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        if (projectilePrefab == null) yield break;

        float dir = p.isFacingRight ? 1f : -1f;

        //소환 위치
        Vector2 spawnPos = (Vector2)p.transform.position + new Vector2(spawnOffset.x * dir, spawnOffset.y);

        //투사체 생성
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        ProjectileHitbox projectile = projectileObj.GetComponent<ProjectileHitbox>();

        if (projectile == null) //투사체가 없으면 종료
        {
            Destroy(projectileObj);
            yield break;
        }

        //투사체 생성 방향, 속도, 데미지 등 값을 넘겨줌
        projectile.Init(dir, projectileSpeed, maxDistance,
            rotationSpeed, damageMultiplier, enemyLayer, destroyOnEnemyHit);

        yield return null;
    }


}