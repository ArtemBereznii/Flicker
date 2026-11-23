using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public int damage; // Set by the firing Weapon (damagePerShot)
    [SerializeField] private LayerMask zombieLayer; // Set to "Zombies" layer
    [SerializeField] private LayerMask wallLayer;   // Set to "Walls" layer
    [SerializeField] private GameObject impactEffect;

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = 1 << other.gameObject.layer;

        // Check if collided with zombie or wall
        if ((zombieLayer.value & otherLayer) != 0) // Zombie hit
        {
            if (other.TryGetComponent(out ZombieHealth zombie))
            {
                zombie.TakeDamage(damage);
                SpawnImpactEffect(other.ClosestPoint(transform.position));
            }
            PooledObject.Despawn(gameObject);
        }
        else if ((wallLayer.value & otherLayer) != 0) // Wall hit
        {
            SpawnImpactEffect(other.ClosestPoint(transform.position));
            PooledObject.Despawn(gameObject);
        }
        // Ignore all other collisions
    }

    private void SpawnImpactEffect(Vector2 position)
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, position, Quaternion.identity);
        }
    }
}