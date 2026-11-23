using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private PlayerHealth playerHealth;

    void Start()
    {
        slider.maxValue = playerHealth.MaxHealth;
        slider.value = playerHealth.CurrentHealth;

        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (slider == null) slider = GetComponent<Slider>();
    }

    void Update()
    {
        UpdateHealthBar();
    }

    public void UpdateHealthBar()
    {
        slider.value = playerHealth.CurrentHealth;
    }
}