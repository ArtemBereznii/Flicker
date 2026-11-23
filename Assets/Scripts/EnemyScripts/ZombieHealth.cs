using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Effects")]
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private AudioClip[] hurtSounds; // Multiple variants

    private int currentHealth;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Visual/Audio feedback
        StartCoroutine(DamageFlash());
        PlayRandomHurtSound();
        SpawnBloodEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void PlayRandomHurtSound()
    {
        if (hurtSounds.Length > 0)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffect != null)
        {
            Instantiate(bloodEffect, transform.position, Quaternion.identity);
        }
    }

    private void Die()
    {
        // Notify spawner if needed
        //if (ZombieSpawner.Instance != null)
        //    ZombieSpawner.Instance.ZombieDied();

        Destroy(gameObject);
    }
}