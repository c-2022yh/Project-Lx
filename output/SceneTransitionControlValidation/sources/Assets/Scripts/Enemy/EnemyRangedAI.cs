using UnityEngine;
using UnityEngine.Serialization;

// Stationary ranged enemy: detect, face the player, and let EnemyRangedAttack fire.
public class EnemyRangedAI : MonoBehaviour
{
    [Header("Rectangular Detection")]
    [Tooltip("Horizontal reach on each side of the detection center.")]
    [FormerlySerializedAs("attackRange")]
    [SerializeField, Min(0f)] private float detectionRangeX = 6f;
    [Tooltip("Vertical reach above and below the detection center.")]
    [SerializeField, Min(0f)] private float detectionRangeY = 0.8f;
    [SerializeField] private Vector2 detectionOffset;

    [Header("Line Of Sight")]
    [Tooltip("Terrain layers that block sight. Nothing uses distance detection only.")]
    [SerializeField] private LayerMask sightObstacleLayer;

    private EnemyAI ai;
    private EnemyRangedAttack attack;

    public bool IsInAttackRange { get; private set; }

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        attack = GetComponent<EnemyRangedAttack>();
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
        if (IsInAttackRange && (attack == null || !attack.IsAttacking)) ai.FacePlayer();
    }

    // Recheck at launch as well, so a stale FixedUpdate result cannot fire through cover.
    public bool CanAttackPlayer()
    {
        if (!isActiveAndEnabled || ai == null || !ai.isActiveAndEnabled ||
            ai.Player == null || !ai.Player.gameObject.activeInHierarchy)
            return false;

        Vector2 origin = (Vector2)transform.position + detectionOffset;
        Vector2 target = ai.Player.position;
        Vector2 difference = target - origin;
        if (Mathf.Abs(difference.x) > detectionRangeX ||
            Mathf.Abs(difference.y) > detectionRangeY) return false;

        return sightObstacleLayer.value == 0 ||
            Physics2D.Linecast(origin, target, sightObstacleLayer).collider == null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 center = (Vector2)transform.position + detectionOffset;
        Gizmos.DrawWireCube(center, new Vector3(detectionRangeX * 2f, detectionRangeY * 2f, 0f));
    }
}
