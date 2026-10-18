using UnityEngine;

// Stationary ranged enemy: detect, face the player, and let EnemyRangedAttack fire.
public class EnemyRangedAI : MonoBehaviour
{
    [Header("Ranged Distance")]
    [SerializeField, Min(0f)] private float attackRange = 6f;

    [Header("Line Of Sight")]
    [Tooltip("Terrain layers that block sight. Nothing uses distance detection only.")]
    [SerializeField] private LayerMask sightObstacleLayer;

    private EnemyAI ai;

    public bool IsInAttackRange { get; private set; }

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }

    private void OnEnable()
    {
        IsInAttackRange = false;
    }

    private void OnDisable()
    {
        IsInAttackRange = false;
    }

    private void FixedUpdate()
    {
        IsInAttackRange = false;
        if (ai == null || !ai.BeginBehaviourTick()) return;

        // Preserve gravity and knockback, but never patrol or approach the player.
        ai.StopMovement();
        IsInAttackRange = CanAttackPlayer();
        if (IsInAttackRange) ai.FacePlayer();
    }

    // Recheck at launch as well, so a stale FixedUpdate result cannot fire through cover.
    public bool CanAttackPlayer()
    {
        if (!isActiveAndEnabled || ai == null || !ai.isActiveAndEnabled ||
            ai.Player == null || !ai.Player.gameObject.activeInHierarchy || !ai.CanDetectPlayer())
            return false;

        Vector2 origin = transform.position;
        Vector2 target = ai.Player.position;
        if ((target - origin).sqrMagnitude > attackRange * attackRange) return false;

        return sightObstacleLayer.value == 0 ||
            Physics2D.Linecast(origin, target, sightObstacleLayer).collider == null;
    }
}
