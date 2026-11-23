using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f; // How quickly zombie rotates toward player

    private Transform player;
    private NavMeshAgent agent;
    private Rigidbody2D rb;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();

        // Configure NavMesh for 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;
    }

    void Update()
    {
        if (player == null) return;

        // Pathfinding (handled by NavMeshAgent)
        agent.SetDestination(player.position);

        // Manual rotation toward player
        Vector2 direction = (player.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Smooth rotation
        Quaternion rotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        //if (collision.gameObject.CompareTag("Player"))
        //{
        //    collision.gameObject.GetComponent<PlayerHealth>().TakeDamage(10);
        //}
    }
}