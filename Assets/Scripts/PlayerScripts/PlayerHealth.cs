using System;
using System.Collections;
using UnityEngine;

// Owns the player's health and announces changes and death.
public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private float flashDuration = 0.1f;

    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private bool isInvincible;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    // Ignores all damage while set (the level-transition cutscene sets it).
    public bool Invulnerable { get; set; }

    public event Action OnHealthChanged;
    public event Action OnDied;
    public event Action OnRevived;

    void Awake()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || isInvincible || Invulnerable) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        OnHealthChanged?.Invoke();

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);

        StartCoroutine(DamageFlash());

        if (currentHealth == 0)
            Die();
        else
            StartCoroutine(InvincibilityFrame());
    }

    private IEnumerator DamageFlash()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = original;
    }

    private IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke();
    }

    // Back to full health and control (used when a new block starts).
    public void Revive()
    {
        bool wasDead = IsDead;
        IsDead = false;
        isInvincible = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
        if (wasDead) OnRevived?.Invoke();
    }
}
