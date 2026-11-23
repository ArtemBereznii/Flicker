using UnityEngine;

public class Shotgun : Weapon
{
    [Header("Shotgun Settings")]
    public int pellets = 8;
    public float spreadAngle = 30f;

    protected override void PerformShot()
    {
        for (int i = 0; i < pellets; i++)
        {
            float angle = Random.Range(-spreadAngle, spreadAngle);
            SpawnProjectile(Quaternion.Euler(0, 0, angle) * firePoint.rotation);
        }
    }
}
