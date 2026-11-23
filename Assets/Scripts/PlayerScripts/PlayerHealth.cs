using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private AudioClip hurtSound;

    [Header("References")]
    private AudioSource audioSource;
    private bool isInvincible;
    private int currentHealth;

    // Properties and event for the HealthBar
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public event System.Action OnHealthChanged;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke();
        audioSource.PlayOneShot(hurtSound);

        // Visual feedback
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrame());
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        Color original = sprite.color;
        sprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sprite.color = original;
    }

    private System.Collections.IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        // Add death animation/restart logic here
    }
}