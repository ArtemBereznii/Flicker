using UnityEngine;

public class RPG : Weapon
{
    [Header("RPG Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private LayerMask damageLayers;

    protected override void PerformShot()
    {
        GameObject rocket = SpawnProjectile(firePoint.rotation);

        if (rocket.TryGetComponent(out RocketProjectile rocketProjectile))
            rocketProjectile.Initialize(damagePerShot, explosionRadius, explosionEffect, damageLayers);
    }
}
