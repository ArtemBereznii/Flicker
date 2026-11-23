using System.Collections;
using UnityEngine;

// Owns the zombie's health and its hit tint. Never touches alpha: that belongs to ZombieVisibility.
public class ZombieHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = Color.red;

    [Header("Effects")]
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private AudioClip[] hurtSounds; // Multiple variants

    private int currentHealth;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Color baseTint;
    private Coroutine flashRoutine;

    public bool IsDead { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseTint = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DamageFlash());
        PlayRandomHurtSound();
        SpawnBloodEffect();

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator DamageFlash()
    {
        SetTint(flashColor);
        yield return new WaitForSeconds(flashDuration);
        SetTint(baseTint);
        flashRoutine = null;
    }

    // Changes RGB only, keeping whatever alpha ZombieVisibility has set.
    private void SetTint(Color tint)
    {
        tint.a = spriteRenderer.color.a;
        spriteRenderer.color = tint;
    }

    private void PlayRandomHurtSound()
    {
        if (audioSource != null && hurtSounds != null && hurtSounds.Length > 0)
            audioSource.PlayOneShot(hurtSounds[Random.Range(0, hurtSounds.Length)]);
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffect != null)
            Instantiate(bloodEffect, transform.position, Quaternion.identity);
    }

    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
}
