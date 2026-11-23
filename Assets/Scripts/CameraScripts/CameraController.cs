using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float followStrength = 20f; // How quickly the camera follows the player
    public float mouseInfluence = 1f; // How much the mouse affects the camera position
    public float maxMouseDistance = 2f; // Maximum distance the mouse can pull the camera

    private Vector3 targetPosition;

    void Update()
    {
        // Get the mouse position in world coordinates
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Ensure the z-coordinate is 0 (2D space)

        // Calculate the direction from the player to the mouse
        Vector3 mouseDirection = (mousePos - player.position).normalized;

        // Limit the mouse influence to a maximum distance
        float mouseDistance = Vector3.Distance(player.position, mousePos);
        mouseDistance = Mathf.Min(mouseDistance, maxMouseDistance);

        // Calculate the target position for the camera
        targetPosition = player.position + mouseDirection * (mouseDistance / maxMouseDistance) * mouseInfluence;
    }

    void LateUpdate()
    {
        // Move the camera toward the target position with a fixed speed
        transform.position = Vector3.Lerp(transform.position, targetPosition, followStrength * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, transform.position.y, -10); // Ensure the camera stays at the correct z-coordinate
    }
}