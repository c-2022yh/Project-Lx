using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns only waiting shards. Launched projectiles are independent of this runner.
public sealed class ScorchedEarthBarrage : MonoBehaviour
{
    private Player owner;
    private ScorchedEarthSkillData.BarrageSettings settings;
    private ScorchedEarthSkillData.PreparedDamage damage;
    private LayerMask enemyLayer;
    private readonly List<ScorchedEarthProjectile> waiting = new List<ScorchedEarthProjectile>();
    private List<Enemy> targets;
    private GameObject preparationEffect;
    private float facing;
    private bool completed;

    public void Initialize(Player player, ScorchedEarthSkillData.BarrageSettings config,
        LayerMask enemies, ScorchedEarthSkillData.PreparedDamage preparedDamage)
    {
        owner = player;
        settings = config;
        damage = preparedDamage;
        enemyLayer = enemies;
        facing = owner.isFacingRight ? 1f : -1f;
        targets = ScorchedEarthTargeting.FindTargets(owner.transform.position,
            settings.targetSearchRadius, enemyLayer, settings.sightObstacleLayer);

        int count = Mathf.Max(2, settings.projectileCount);
        for (int i = 0; i < count; i++)
        {
            bool finisher = i == count - 1;
            GameObject prefab = finisher ? settings.finisherProjectilePrefab : settings.regularProjectilePrefab;
            GameObject shard = Instantiate(prefab);
            ScorchedEarthProjectile projectile = shard.GetComponent<ScorchedEarthProjectile>();
            if (projectile == null) projectile = shard.AddComponent<ScorchedEarthProjectile>();
            waiting.Add(projectile);
        }
        UpdateFormation();
        if (settings.preparationEffectPrefab != null)
            preparationEffect = Instantiate(settings.preparationEffectPrefab, owner.transform);
    }

    private void Update()
    {
        if (completed) return;
        if (owner == null || !owner.isActiveAndEnabled || owner.Health.IsDead
            || owner.Awakening == null || !owner.Awakening.IsAwakened
            || !owner.ActionState.isSkillActive)
        {
            // Includes death, hit stun and external awakening termination.
            Cancel();
            return;
        }
        UpdateFormation();
    }

    private Vector2 FormationDirection(int index)
    {
        // Launch from alternating ends of an arc behind the player.
        int count = waiting.Count;
        int slot = index % 2 == 0 ? index / 2 : count - 1 - index / 2;
        float angle = Mathf.Lerp(-settings.formationArc * 0.5f,
            settings.formationArc * 0.5f, slot / (float)(count - 1)) * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Cos(angle) * facing, Mathf.Sin(angle));
    }

    private void UpdateFormation()
    {
        facing = owner.isFacingRight ? 1f : -1f;
        Vector2 center = (Vector2)owner.transform.position
            + new Vector2(settings.formationOffset.x * facing, settings.formationOffset.y);
        for (int i = 0; i < waiting.Count; i++)
        {
            if (waiting[i] == null) continue;
            Vector2 outward = FormationDirection(i);
            waiting[i].transform.position = center + outward * settings.formationRadius;
            waiting[i].transform.right = outward;
        }
    }

    public IEnumerator Process()
    {
        if (settings.preparationDuration > 0f)
            yield return new WaitForSeconds(settings.preparationDuration);
        for (int i = 0; i < waiting.Count && !completed; i++)
        {
            bool finisher = i == waiting.Count - 1;
            if (finisher && settings.finisherDelay > 0f)
                yield return new WaitForSeconds(settings.finisherDelay);
            if (completed) yield break;
            Enemy target = targets.Count > 0 ? targets[i % targets.Count] : null;
            ScorchedEarthProjectile projectile = waiting[i];
            projectile.Launch(settings, enemyLayer, finisher ? damage.finisher : damage.regular,
                target, FormationDirection(i), finisher, finisher ? Finish : (System.Action)null);
            waiting[i] = null;
            if (!finisher) yield return new WaitForSeconds(Mathf.Max(0.01f, settings.launchInterval));
        }
        ClearPreparation();
        while (!completed) yield return null;
    }

    private void ClearPreparation()
    {
        foreach (ScorchedEarthProjectile shard in waiting)
            if (shard != null) Destroy(shard.gameObject);
        if (preparationEffect != null) Destroy(preparationEffect);
    }

    private void Finish()
    {
        if (completed) return;
        completed = true;
        ClearPreparation();
        if (owner != null) owner.Awakening?.EndAwakening();
        Destroy(gameObject);
    }

    public void Cancel() { Finish(); }
    private void OnDisable() { Finish(); }
}
