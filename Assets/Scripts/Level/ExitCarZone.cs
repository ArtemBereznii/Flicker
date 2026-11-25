using UnityEngine;

// The exit car's reach area: a trigger child of the car that tells it when the player steps in or out.
// It lives on the Default layer, never Wall: ZombieVisibility's wall linecasts would hit the trigger.
[RequireComponent(typeof(Collider2D))]
public class ExitCarZone : MonoBehaviour
{
    private ExitCar car;

    void Awake() => car = GetComponentInParent<ExitCar>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameTags.Player)) car.SetPlayerInRange(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(GameTags.Player)) car.SetPlayerInRange(false);
    }
}
