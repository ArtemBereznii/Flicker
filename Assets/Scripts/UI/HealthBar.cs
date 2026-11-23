using UnityEngine;
using UnityEngine.UI;

// Shows the player's health; updates only when PlayerHealth reports a change.
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private PlayerHealth playerHealth;

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    void Start()
    {
        if (playerHealth == null || slider == null)
        {
            Debug.LogWarning("HealthBar is missing its Slider or PlayerHealth.", this);
            enabled = false;
            return;
        }

        slider.maxValue = playerHealth.MaxHealth;
        UpdateHealthBar();
    }

    public void UpdateHealthBar()
    {
        slider.value = playerHealth.CurrentHealth;
    }
}
