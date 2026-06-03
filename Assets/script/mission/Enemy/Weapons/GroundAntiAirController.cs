using UnityEngine;

[RequireComponent(typeof(EnemyTargetSelector))]
public class GroundAntiAirController : MonoBehaviour
{
    public bool useGun = true;
    public bool useMissile;
    public float gunRange = 900f;
    public float missileRange = 800f;

    public EnemyTargetSelector targetSelector;
    public EnemyGunShooter gunShooter;
    public EnemyMissileShooter missileShooter;

    AugumentStatus status;
    Rigidbody rb;

    void Awake()
    {
        targetSelector ??= GetComponent<EnemyTargetSelector>();
        gunShooter ??= GetComponent<EnemyGunShooter>();
        missileShooter ??= GetComponent<EnemyMissileShooter>();
        status = GetComponent<AugumentStatus>();
        rb = GetComponent<Rigidbody>();
        SyncTargetSelectorRange();
    }

    void Update()
    {
        if (targetSelector == null || !targetSelector.HasLockedTarget) return;

        Vector3 platformVelocity = GetPlatformVelocity();
        Vector3 direction = targetSelector.lastDirToTarget;
        float targetDistance = Vector3.Distance(transform.position, targetSelector.target.transform.position);

        if (useGun && gunShooter != null && targetDistance <= gunRange)
            gunShooter.TryFire(
                direction,
                platformVelocity,
                targetSelector.target.transform,
                targetSelector.targetVelocity
            );

        if (useMissile && missileShooter != null && targetDistance <= missileRange)
            missileShooter.TryFire(
                direction,
                platformVelocity,
                targetSelector.target.transform,
                targetSelector.IsAirsuppression
            );
    }

    Vector3 GetPlatformVelocity()
    {
        if (rb != null)
            return rb.linearVelocity;

        if (status != null)
            return status.Velocity;

        return Vector3.zero;
    }

    public void SyncTargetSelectorRange()
    {
        if (targetSelector == null && !TryGetComponent(out targetSelector)) return;

        float range = 0f;
        if (useGun)
            range = Mathf.Max(range, gunRange);
        if (useMissile)
            range = Mathf.Max(range, missileRange);
        if (range <= 0f) return;

        targetSelector.detectRange = Mathf.Max(targetSelector.detectRange, range);
        targetSelector.lockRange = Mathf.Max(targetSelector.lockRange, range);
    }
}
