using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    public float rotationOffset = 90f; // Offset to align the light with the mouse

    private PlayerHealth health;

    void Awake()
    {
        health = GetComponentInParent<PlayerHealth>();
        if (health != null) health.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        RotateTowardMouse();
    }

    void RotateTowardMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = new Vector2(mousePos.x - transform.position.x, mousePos.y - transform.position.y);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    private void HandleDeath()
    {
        enabled = false;
    }
}
