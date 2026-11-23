using UnityEngine;
using UnityEngine.AI;

// The NavMeshAgent only plans the path; the Rigidbody2D does the moving, so walls,
// the player and other zombies collide through physics.
[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody2D), typeof(ZombieAttack))]
public class ZombieAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;
    [Tooltip("Within this distance and in line of sight, the zombie turns to face the player instead of its path.")]
    [SerializeField] private float faceTargetRange = 3f;
    [SerializeField] private float repathInterval = 0.2f;
    [Tooltip("Stop this fraction of the attack range away from the player.")]
    [SerializeField, Range(0.1f, 1f)] private float stopAtAttackRangeFraction = 0.8f;

    private NavMeshAgent agent;
    private Rigidbody2D rb;
    private ZombieAttack attack;
    private float nextRepathTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        attack = GetComponent<ZombieAttack>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;
        agent.stoppingDistance = attack.AttackRange * stopAtAttackRangeFraction;
    }

    void Update()
    {
        if (!attack.TargetAlive)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (Time.time >= nextRepathTime && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(attack.Target.position);
            nextRepathTime = Time.time + repathInterval;
        }
    }

    void FixedUpdate()
    {
        // Stop once the player is in reach, instead of shoving into them.
        bool inReach = attack.TargetAlive && attack.IsTargetInReach() && attack.CanSeePlayer();
        Vector2 velocity = attack.TargetAlive && !inReach && agent.isOnNavMesh ? (Vector2)agent.desiredVelocity : Vector2.zero;
        rb.linearVelocity = velocity;

        // Keep the agent's simulated position on the body, which physics may have pushed.
        agent.nextPosition = new Vector3(rb.position.x, rb.position.y, transform.position.z);

        Vector2 lookDirection = velocity;
        if (attack.TargetAlive)
        {
            Vector2 toTarget = (Vector2)attack.Target.position - rb.position;
            if (toTarget.magnitude <= faceTargetRange && attack.CanSeePlayer())
                lookDirection = toTarget;
        }

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
