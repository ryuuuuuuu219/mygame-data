using UnityEngine;

public class GroundAntiAirController : MonoBehaviour
{
    public bool useGun = true;
    public bool useMissile;

    public EnemyTargetSelector targetSelector;
    public EnemyGunShooter gunShooter;
    public EnemyMissileShooter missileShooter;
    public EnemyMissileShooter_longrange longrangeMissileShooter;

    AugumentStatus status;
    Rigidbody rb;

    void Awake()
    {
        targetSelector ??= GetComponent<EnemyTargetSelector>();
        gunShooter ??= GetComponent<EnemyGunShooter>();
        missileShooter ??= GetComponent<EnemyMissileShooter>();
        longrangeMissileShooter ??= GetComponent<EnemyMissileShooter_longrange>();
        status = GetComponent<AugumentStatus>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (targetSelector == null || !targetSelector.HasLockedTarget) return;

        Vector3 platformVelocity = GetPlatformVelocity();
        Vector3 direction = targetSelector.lastDirToTarget;

        if (useGun && gunShooter != null)
            gunShooter.TryFire(
                direction,
                platformVelocity,
                targetSelector.target.transform,
                targetSelector.targetVelocity
            );

        if (useMissile && missileShooter != null)
            missileShooter.TryFire(direction, platformVelocity, targetSelector.target.transform);

        if (useMissile && longrangeMissileShooter != null)
            longrangeMissileShooter.TryFire(direction, platformVelocity, targetSelector.target.transform);
    }

    Vector3 GetPlatformVelocity()
    {
        if (rb != null)
            return rb.linearVelocity;

        if (status != null)
            return status.Velocity;

        return Vector3.zero;
    }
}
