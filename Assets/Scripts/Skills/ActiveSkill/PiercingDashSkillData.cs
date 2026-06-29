using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Skills/PiercingDash", menuName = "Skills/PiercingDash")]

//질풍참 류 적을 관통하면서 대쉬베기 스킬
public class PiercingDashSkillData : AttackSkillData
{
    [Header("Dash Attack")]
    public float dashDistance = 3.5f;
    public Vector2 dashHitBoxSize = new Vector2(1.4f, 1.2f);
    public Vector2 dashHitBoxOffset = new Vector2(0.7f, 0f);

    //대쉬 후 적과의 피격무적 판정 시간
    [SerializeField] private float contactIgnoreAfterDash = 0.2f;

    public override IEnumerator ProcessSkill(Player p)
    {

        if (p == null) yield break;
        
        PlayerHealth playerHealth = p.GetComponent<PlayerHealth>();
        playerHealth?.BeginEnemyContactIgnore();

        float dir = p.isFacingRight ? 1f : -1f;

        //벽 체크 및 거리 계산
        RaycastHit2D hit = Physics2D.Raycast(
            p.transform.position,
            Vector2.right * dir,
            dashDistance,
            LayerMask.GetMask("Ground")
        );
        float actualDist = hit.collider ? hit.distance : dashDistance;

        //해시셋 (한번 충돌한 적은 다시 판정하면 안되므로 해시셋으로 관리)
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

        //레이어마스크 생성
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        bool canIgnoreCollision = playerLayer != -1 && enemyLayerIndex != -1;
        
        p.SetPhysicsFreeze(true);

        float timer = 0f;
        float speed = actualDist / activeTime;

        while (timer < activeTime)
        {
            //이동 전 현재 위치 판정
            DamageEnemiesDuringDash(p, dir, hitEnemies);

            //돌진 이동
            p.rb.linearVelocity = new Vector2(dir * speed, 0f);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();

            //이동 후 위치 판정
            DamageEnemiesDuringDash(p, dir, hitEnemies);
        }

        p.rb.linearVelocity = Vector2.zero;

        //마지막 프레임 판정 보정
        DamageEnemiesDuringDash(p, dir, hitEnemies);

        p.SetPhysicsFreeze(false);

        //무적 판정 적용
        playerHealth?.EndEnemyContactIgnoreAfterDelay(contactIgnoreAfterDash);


    }

    //데미지 함수
    private void DamageEnemiesDuringDash(Player p, float dir, HashSet<Enemy> hitEnemies) //HashSet<Enemy> hitEnemies 이미 맞은 적 목록
    { 
        //중심부 설정
        Vector2 center = (Vector2)p.transform.position + new Vector2(dashHitBoxOffset.x * dir, dashHitBoxOffset.y);

        //범위 안에 있는 콜라이더 찾기
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, dashHitBoxSize, 0f, enemyLayer);

        //찾은 적마다 콜라이더 돌면서 충돌했는지 검사
        foreach (Collider2D hit in hits)
        {
            //Enemy 스크립트 찾기
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) enemy = hit.GetComponentInParent<Enemy>(); //못찾았으면 혹시 부모한테서도 찾아봄

            //Enemy가 아니면 무시
            if (enemy == null) continue;
            
            //이미 맞은 적이면 무시
            if (hitEnemies.Contains(enemy)) continue;

            //처음 맞은 적이면 목록에 추가
            hitEnemies.Add(enemy);

            //최종 히트 판정 데미지 계산
            enemy.TakeDamage(damageMultiplier, new Vector2(dir, 0f));
            
            Debug.Log($"A Skill Hit: {enemy.name}");

        }
    }

}