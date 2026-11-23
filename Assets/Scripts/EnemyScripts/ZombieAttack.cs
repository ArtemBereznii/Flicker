using UnityEngine;

// Damages the player when in range and not blocked by a wall.
public class ZombieAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [Tooltip("Layers that block attacks (defaults to the Wall layer).")]
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("Gap between colliders that still counts as touching.")]
    [SerializeField] private float contactTolerance = 0.05f;

    private float nextAttackTime;
    private Transform player;
    private PlayerHealth playerHealth;
    private Collider2D ownCollider;
    private Collider2D playerCollider;

    public float AttackRange => attackRange;
    public Transform Target => player;
    public bool TargetAlive => playerHealth != null && !playerHealth.IsDead;

    void Awake()
    {
        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask("Wall");
        ownCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<PlayerHealth>();
            playerCollider = playerObject.GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (!TargetAlive || Time.time < nextAttackTime)
            return;

        if (IsTargetInReach() && CanSeePlayer())
        {
            playerHealth.TakeDamage(damage);
            nextAttackTime = Time.time + attackRate;
        }
    }

    // In reach when centers are within attackRange, or when the bodies are touching
    // (colliders keep them apart, so a short range could otherwise never be reached).
    public bool IsTargetInReach()
    {
        if (player == null) return false;
        if (Vector2.Distance(transform.position, player.position) <= attackRange) return true;
        return ownCollider != null && playerCollider != null
            && Physics2D.Distance(ownCollider, playerCollider).distance <= contactTolerance;
    }

    public bool CanSeePlayer()
    {
        if (player == null) return false;
        return !Physics2D.Linecast(transform.position, player.position, wallLayer);
    }

    // Visualize attack range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
