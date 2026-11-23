public class Pistol : Weapon
{
    protected override void PerformShot()
    {
        SpawnProjectile(firePoint.rotation);
    }
}
