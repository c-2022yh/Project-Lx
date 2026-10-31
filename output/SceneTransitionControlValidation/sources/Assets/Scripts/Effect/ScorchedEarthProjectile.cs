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
    private Action onResolved;
    private SoundHandle flightAudio;
    private ScorchedEarthProjectileVisual visual;

    private void Awake()
    {
        visual = GetComponent<ScorchedEarthProjectileVisual>();
    }

    public void Launch(ScorchedEarthSkillData.BarrageSettings config, LayerMask enemies,
        DamageInfo preparedDamage, Enemy initialTarget, Vector2 outward,
        Action callback)
    {
        settings = config;
        enemyLayer = enemies;
        damage = preparedDamage;
        target = initialTarget;
        direction = outward.sqrMagnitude > 0f ? outward.normalized : Vector2.right;
        onResolved = callback;
        launched = true;
        flightAudio.Stop();
        flightAudio = GameAudio.PlayLoop(settings.flightLoop, transform);
        if (visual != null && visual.IsReady) visual.BeginFlight();
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
        float distance = Mathf.Max(0.1f, settings.projectileSpeed) * Mathf.Min(dt, remaining);
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
        flightAudio.Stop();
        if (ScorchedEarthTargeting.IsValid(directHit))
        {
            directHit.TakeDamage(damage, direction);
            if (visual != null && visual.IsReady) visual.PlayImpact();
            else SpawnEffect(settings.regularHitEffectPrefab);
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

    private void OnDisable() { flightAudio.Stop(); }
    private void OnDestroy() { flightAudio.Stop(); NotifyResolved(); }
}
