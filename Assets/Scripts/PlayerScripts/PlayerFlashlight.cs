using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    public float rotationOffset = 90f; // Offset to align the light with the mouse

    void Update()
    {
        // Rotate the flashlight toward the mouse
        RotateTowardMouse();
    }

    void RotateTowardMouse()
    {
        // Get the mouse position in world coordinates
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Ensure the z-coordinate is 0 (2D space)

        // Calculate the direction from the flashlight to the mouse
        Vector2 direction = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y);

        // Calculate the angle in degrees
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply the rotation with an offset
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }
}