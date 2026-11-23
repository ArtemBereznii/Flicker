using UnityEngine;
using static WeaponManager;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of the player

    private Vector2 movement;

    void Update()
    {
        // Input handling (WASD or Arrow Keys)
        movement.x = Input.GetAxisRaw("Horizontal"); // Left/Right movement
        movement.y = Input.GetAxisRaw("Vertical"); // Up/Down movement

        // Normalize the movement vector to prevent faster diagonal movement
        movement = movement.normalized;

        // Rotate player toward mouse
        RotateTowardMouse();
    }

    void FixedUpdate()
    {
        // Movement logic (using transform)
        transform.Translate(movement * moveSpeed * Time.fixedDeltaTime, Space.World);
    }

    void RotateTowardMouse()
    {
        // Get the mouse position in world coordinates
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Ensure the z-coordinate is 0 (2D space)

        // Calculate the direction from the player to the mouse
        Vector2 direction = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y);

        // Rotate the player to face the mouse
        transform.right = direction; // Point the player's "right" direction toward the mouse
    }
}