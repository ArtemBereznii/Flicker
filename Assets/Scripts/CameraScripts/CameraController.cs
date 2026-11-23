using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float followStrength = 20f; // How quickly the camera follows the player
    public float mouseInfluence = 1f; // How much the mouse affects the camera position
    public float maxMouseDistance = 2f; // Maximum distance the mouse can pull the camera

    private Camera cam;
    private PlayerInputReader input;
    private Vector3 targetPosition;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (player != null) input = player.GetComponent<PlayerInputReader>();
    }

    void Update()
    {
        // Cursor offset from the screen center, in world units. Measured relative to the camera
        // itself, so moving the camera doesn't move the offset (no feedback loop).
        Vector2 cursorScreen = input != null ? input.AimScreenPosition : (Vector2)cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        Vector3 cursorOffset = cam.ScreenToWorldPoint(cursorScreen) - transform.position;
        cursorOffset.z = 0f;

        float distance = Mathf.Min(cursorOffset.magnitude, maxMouseDistance);
        Vector3 lead = cursorOffset.normalized * (distance / maxMouseDistance) * mouseInfluence;

        targetPosition = player.position + lead;
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, followStrength * Time.deltaTime);
        transform.position = new Vector3(transform.position.x, transform.position.y, -10f); // Keep the camera at its 2D depth
    }
}
