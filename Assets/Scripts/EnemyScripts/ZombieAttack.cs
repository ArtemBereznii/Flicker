using UnityEngine;

public class ZombieAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 1.5f;

    private float nextAttackTime;
    private Transform player;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (Time.time >= nextAttackTime &&
            Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            Attack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void Attack()
    {
        if (player.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(damage);
        }
    }

    // Visualize attack range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}