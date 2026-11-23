using UnityEngine;

// Moves and aims the player through its Rigidbody2D so walls stop it.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private PlayerHealth health;
    private Vector2 movement;
    private float aimAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null) health.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - rb.position;
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
