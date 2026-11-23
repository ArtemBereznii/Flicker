using UnityEngine;

// Explodes once: on hitting a zombie or wall, or when its lifetime runs out.
// All damage comes from the explosion, with falloff from the center.
public class RocketProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask zombieLayer; // Set to "Zombies" layer
    [SerializeField] private LayerMask wallLayer;   // Set to "Walls" layer

    private int damage;
    private float explosionRadius;
    private GameObject explosionEffect;
    private LayerMask damageLayers;
    private bool exploded;

    public void Initialize(int dmg, float radius, GameObject effect, LayerMask layers, float lifetime)
    {
        damage = dmg;
        explosionRadius = radius;
        explosionEffect = effect;
        damageLayers = layers;
        Invoke(nameof(Explode), lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = 1 << other.gameObject.layer;
        if ((zombieLayer.value & otherLayer) != 0 || (wallLayer.value & otherLayer) != 0)
            Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector2 center = transform.position;

        if (explosionEffect != null)
            Instantiate(explosionEffect, center, Quaternion.identity);

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, explosionRadius, damageLayers))
        {
            if (!hit.TryGetComponent(out ZombieHealth zombie))
                continue;

            // Distance to the collider's edge, so a direct hit deals full damage.
            float distance = Vector2.Distance(center, hit.ClosestPoint(center));
            float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);
            zombie.TakeDamage(Mathf.RoundToInt(damage * falloff));
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
