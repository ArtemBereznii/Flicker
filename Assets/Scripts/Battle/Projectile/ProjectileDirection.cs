using UnityEngine;

public class ProjectileDirection : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool faceMovementDirection = true;
    [SerializeField] private float rotationOffset = -90f; // Adjust for your sprite's orientation

    private Rigidbody2D rb;
    private Vector2 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Initial rotation based on starting velocity
        if (rb.linearVelocity != Vector2.zero)
        {
            UpdateRotation(rb.linearVelocity);
        }
    }

    void Update()
    {
        if (faceMovementDirection && rb.linearVelocity != Vector2.zero)
        {
            UpdateRotation(rb.linearVelocity);
        }
    }

    private void UpdateRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    // Optional: For physics-based projectiles
    void FixedUpdate()
    {
        if (faceMovementDirection && rb.linearVelocity != lastVelocity)
        {
            lastVelocity = rb.linearVelocity;
            UpdateRotation(rb.linearVelocity);
        }
    }
}