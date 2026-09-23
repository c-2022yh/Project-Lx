using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SKL_ScorchedEarth", menuName = "Skills/Awakening/Scorched Earth")]

//폭주 유물이 제공하는 각성 전용 광역 공격 스킬
public class ScorchedEarthSkillData : AttackSkillData
{
    [Header("Scorched Earth Hitbox")]
    [SerializeField] private Vector2 hitboxOffset = new Vector2(1f, 0f);
    [SerializeField] private Vector2 hitboxSize = new Vector2(3f, 2f);

    [Header("Scorched Earth Timing")]
    [SerializeField, Min(0f)] private float windup = 0.3f;
    [SerializeField, Min(0f)] private float recovery = 0.2f;

    [Header("Effect")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField, Min(0f)] private float effectLifetime = 1f;

    //각성 상태와 스킬 시전 가능 상태를 함께 확인한다.
    public override bool CanUse(Player p)
    {
        return p != null && p.Awakening != null && p.Awakening.IsAwakened
            && p.ActionState != null && p.ActionState.CanSkill();
    }

    //버프가 해제되기 전에 기존 공격 스탯과 AttackDamageSpec으로 피해 정보를 만든다.
    public DamageInfo PrepareDamage(Player p)
    {
        return CreateDamageInfo(p);
    }

    //일반 스킬 호출 경로에서도 기존 SkillData 계약을 충족한다.
    public override IEnumerator ProcessSkill(Player p)
    {
        if (p == null) yield break;
        DamageInfo preparedDamage = PrepareDamage(p);
        yield return ProcessPreparedSkill(p, preparedDamage);
    }

    //선딜 뒤 광역 판정을 한 번 수행하고 후딜이 끝나면 반환한다.
    //preparedDamage는 Q를 눌렀을 때의 각성 공격력을 담는다.
    public IEnumerator ProcessPreparedSkill(Player p, DamageInfo preparedDamage)
    {
        if (p == null) yield break;
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (p == null) yield break;

        float direction = p.isFacingRight ? 1f : -1f;
        Vector2 center = (Vector2)p.transform.position
            + new Vector2(hitboxOffset.x * direction, hitboxOffset.y);

        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, center, Quaternion.identity);
            if (direction < 0f) effect.transform.localScale = new Vector3(
                -effect.transform.localScale.x, effect.transform.localScale.y, effect.transform.localScale.z);
            Destroy(effect, effectLifetime);
        }

        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            TryDamageEnemy(hit, hitEnemies, direction, preparedDamage);
        }

        if (recovery > 0f) yield return new WaitForSeconds(recovery);
    }
}
