using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ScorchedEarthProjectile : MonoBehaviour
{
    private ScorchedEarthSkillData.BarrageSettings settings;
    private DamageInfo damage;
    private LayerMask enemyLayer;
    private Enemy target;
    private Vector2 direction;
    private float age;
    private bool launched;
    private bool resolved;
    private bool noReplacement;
    private bool finisher;
    private Action onResolved;

    public void Launch(ScorchedEarthSkillData.BarrageSettings config, LayerMask enemies,
        DamageInfo preparedDamage, Enemy initialTarget, Vector2 outward,
        bool isFinisher, Action callback)
    {
        settings = config;
        enemyLayer = enemies;
        damage = preparedDamage;
        target = initialTarget;
        direction = outward.sqrMagnitude > 0f ? outward.normalized : Vector2.right;
        finisher = isFinisher;
        onResolved = callback;
        launched = true;
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = sprite.sharedMaterial;
            trail.time = 0.18f;
            trail.startWidth = finisher ? 0.25f : 0.12f;
            trail.endWidth = 0f;
            trail.startColor = new Color(1f, 0.12f, 0.04f, 0.7f);
            trail.endColor = new Color(1f, 0.02f, 0f, 0f);
            trail.minVertexDistance = 0.04f;
            trail.sortingLayerID = sprite.sortingLayerID;
            trail.sortingOrder = sprite.sortingOrder - 1;
        }
    }

    private void Update()
    {
        if (!launched || resolved) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector2 position = transform.position;
        if (!ScorchedEarthTargeting.IsValid(target) && !noReplacement)
        {
            List<Enemy> candidates = ScorchedEarthTargeting.FindTargets(position,
                settings.retargetSearchRadius, enemyLayer, settings.sightObstacleLayer);
            target = candidates.Count > 0 ? candidates[0] : null;
            noReplacement = target == null;
        }
        if (age >= settings.outwardDuration && ScorchedEarthTargeting.IsValid(target))
        {
            Vector2 desired = ScorchedEarthTargeting.AimPoint(target) - position;
            if (desired.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.MoveTowardsAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                    Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg, settings.turnSpeed * dt) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        float remaining = Mathf.Max(0f, settings.projectileLifetime - age);
        float distance = Mathf.Max(0.1f, settings.projectileSpeed)
            * (finisher ? Mathf.Max(0.1f, settings.finisherSpeedMultiplier) : 1f) * Mathf.Min(dt, remaining);
        LayerMask collisionMask = enemyLayer;
        if (!settings.passThroughTerrain) collisionMask |= settings.sightObstacleLayer;
        // Swept collision prevents fast shards skipping small enemies at low FPS.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(position, Mathf.Max(0.01f, settings.hitRadius),
            direction, distance, collisionMask);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D hit in hits)
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            bool isEnemyLayer = (enemyLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
            if (isEnemyLayer && !ScorchedEarthTargeting.IsValid(enemy)) continue;
            transform.position = hit.centroid;
            Resolve(isEnemyLayer ? enemy : null);
            return;
        }
        transform.position = position + direction * distance;
        transform.right = direction;
        age += dt;
        if (age >= settings.projectileLifetime) Resolve(null);
    }

    private void Resolve(Enemy directHit)
    {
        if (resolved) return;
        resolved = true;
        if (finisher)
        {
            var damaged = new HashSet<Enemy>();
            if (ScorchedEarthTargeting.IsValid(directHit))
            {
                damaged.Add(directHit);
                directHit.TakeDamage(damage, direction);
            }
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position,
                Mathf.Max(0f, settings.finisherExplosionRadius), enemyLayer))
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (ScorchedEarthTargeting.IsValid(enemy) && damaged.Add(enemy))
                    enemy.TakeDamage(damage, direction);
            }
            SpawnEffect(settings.finisherExplosionEffectPrefab);
        }
        else if (ScorchedEarthTargeting.IsValid(directHit))
        {
            directHit.TakeDamage(damage, direction);
            SpawnEffect(settings.regularHitEffectPrefab);
        }
        NotifyResolved();
        Destroy(gameObject);
    }

    private void SpawnEffect(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(effect, Mathf.Max(0.01f, settings.hitEffectLifetime));
    }

    private void NotifyResolved()
    {
        Action callback = onResolved;
        onResolved = null;
        callback?.Invoke();
    }

    private void OnDestroy() { NotifyResolved(); }
}
