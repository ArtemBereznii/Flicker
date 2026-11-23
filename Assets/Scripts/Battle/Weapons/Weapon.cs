using UnityEngine;
using System.Collections;

// Base class for every weapon. A weapon prefab carries everything the game needs
// to know about it (stats, menu icon, player sprite, ammo bar), so dropping a new
// prefab into Assets/Resources/Weapons is all it takes to add a weapon.
public abstract class Weapon : MonoBehaviour
{
    [Header("Identity")]
    public string weaponName;
    [Tooltip("Order in the selection menu and default loadout (lower comes first).")]
    public int sortOrder;
    public Sprite menuIcon;
    [Tooltip("Player sprite shown while this weapon is equipped.")]
    public Sprite playerSprite;
    [Tooltip("HUD ammo bar spawned for this weapon.")]
    public GameObject ammoBarPrefab;

    [Header("Base Settings")]
    public int damagePerShot = 10;
    public float fireRate = 0.5f;
    public float reloadTime = 1.5f;
    public int maxAmmo = 12;

    [Header("Projectile")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;
    public float projectileLifetime = 2f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    protected AudioSource audioSource;

    public int CurrentAmmo { get; protected set; }
    public int MaxAmmo => maxAmmo;
    public bool IsReloading { get; protected set; }

    protected float nextFireTime;
    private float reloadStartTime;
    private Coroutine activeReloadCoroutine;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        CurrentAmmo = maxAmmo;
    }

    protected virtual void OnEnable()
    {
        nextFireTime = Time.time;
    }

    protected virtual void OnDisable()
    {
        // Coroutines stop when the object is disabled, so an interrupted reload must be reset.
        if (IsReloading)
        {
            StopAllCoroutines();
            activeReloadCoroutine = null;
            IsReloading = false;
        }
    }

    public void ResetFireDelay()
    {
        nextFireTime = Time.time;
    }

    public void Shoot()
    {
        if (IsReloading || Time.time < nextFireTime || CurrentAmmo <= 0)
            return;

        if (firePoint == null || projectilePrefab == null)
        {
            Debug.LogWarning($"{weaponName}: Missing firePoint or projectilePrefab!", this);
            return;
        }

        PlayShootSound();
        PerformShot();
        CurrentAmmo--;
        nextFireTime = Time.time + fireRate;

        if (CurrentAmmo <= 0)
            StartReload();
    }

    // Fire this weapon's projectiles. Use SpawnProjectile for each one.
    protected abstract void PerformShot();

    // Projectiles come from ObjectPools. autoDestroy = false leaves the lifetime to the projectile itself (e.g. rockets explode on expiry).
    protected GameObject SpawnProjectile(Quaternion rotation, bool autoDestroy = true)
    {
        GameObject projectile = ObjectPools.Get(projectilePrefab, firePoint.position, rotation);

        if (projectile.TryGetComponent(out Rigidbody2D rb))
            rb.linearVelocity = rotation * Vector3.right * projectileSpeed;

        if (projectile.TryGetComponent(out Projectile bullet))
            bullet.damage = damagePerShot;

        if (autoDestroy)
            projectile.GetComponent<PooledObject>().ReleaseAfter(projectileLifetime);
        return projectile;
    }

    protected void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    public void StartReload()
    {
        if (IsReloading || CurrentAmmo >= maxAmmo)
            return;

        activeReloadCoroutine = StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        reloadStartTime = Time.time;
        IsReloading = true;

        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);

        yield return new WaitForSeconds(reloadTime);

        CurrentAmmo = maxAmmo;
        IsReloading = false;
        activeReloadCoroutine = null;
    }

    public float CurrentReloadProgress()
    {
        if (!IsReloading) return 0f;
        return (Time.time - reloadStartTime) / reloadTime;
    }
}
