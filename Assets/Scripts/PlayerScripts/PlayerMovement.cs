using UnityEngine;

// Moves and aims the player through its Rigidbody2D so walls stop it.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    [Tooltip("Cursor closer than this to the player doesn't change the aim (prevents jitter).")]
    [SerializeField] private float minAimDistance = 0.5f;

    private Rigidbody2D rb;
    private PlayerHealth health;
    private PlayerInputReader input;
    private Vector2 movement;
    private float aimAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInputReader>();
        aimAngle = rb.rotation;
        // Subscribed for the whole lifetime: death disables this component, revival must still reach it.
        if (health != null)
        {
            health.OnDied += HandleDeath;
            health.OnRevived += HandleRevived;
        }
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.OnDied -= HandleDeath;
        health.OnRevived -= HandleRevived;
    }

    private void HandleRevived() => enabled = true;

    void Update()
    {
        movement = input.Move;

        // Aim from the rendered (interpolated) position, the same one the camera follows.
        // rb.position only changes on physics steps, which makes a short aim vector jitter.
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(input.AimScreenPosition);
        Vector2 direction = mousePos - (Vector2)transform.position;

        // A cursor on top of the player gives no usable direction: keep the last aim.
        if (direction.sqrMagnitude >= minAimDistance * minAimDistance)
            aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
        rb.MoveRotation(aimAngle);
    }

    private void HandleDeath()
    {
        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }
}
