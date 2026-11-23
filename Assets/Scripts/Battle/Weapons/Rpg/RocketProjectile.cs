using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask zombieLayer; // Set to "Zombies" layer
    [SerializeField] private LayerMask wallLayer;   // Set to "Walls" layer

    private int damage;
    private float explosionRadius;
    private GameObject explosionEffect;
    private LayerMask damageLayers;

    void Start()
    {
        // Automatically destroy projectile after 2 seconds if it doesn't hit anything
        Destroy(gameObject, 2f);
    }

    public void Initialize(int dmg, float radius, GameObject effect, LayerMask layers)
    {
        damage = dmg;
        explosionRadius = radius;
        explosionEffect = effect;
        damageLayers = layers;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = 1 << other.gameObject.layer;

        // Check if collided with zombie or wall
        if ((zombieLayer.value & otherLayer) != 0) // Zombie hit
        {
            if (other.TryGetComponent(out ZombieHealth zombie))
            {
                zombie.TakeDamage(damage);
                Explode();
            }
            Destroy(gameObject);
        }
        else if ((wallLayer.value & otherLayer) != 0) // Wall hit
        {
            Explode();
            Destroy(gameObject);
        }
    }

    void Explode()
    {
        // Visual effect
        Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Damage calculation
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out ZombieHealth zombie))
            {
                // Reduced damage by distance
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                float damageFalloff = 1 - Mathf.Clamp01(distance / explosionRadius);
                zombie.TakeDamage(Mathf.RoundToInt(damage * damageFalloff));
            }
        }

        // Camera shake (optional)
        // CameraShake.Instance.Shake(0.3f, 0.7f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}