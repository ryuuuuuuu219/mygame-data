using UnityEngine;

public class EnemyMissileShooter : MonoBehaviour
{
    public Transform missileHardpoint;
    public float missileSpeed = 100f;
    public float missileCooldown = 3f;
    public float missileLifeTime = 10f;

    public float missilePower = 50f;
    public float missileAcceleration = 20f;
    public float missileMaxSpeed = 150f;
    public float missileTurnRate = 8f;
    public float missileBreakAngle = 90f;
    public float missileProportionalConstant = 3f;
    public bool requireLineOfSight = true;
    public LayerMask lineOfSightMask = ~0;
    public float lineOfSightOriginOffset = 2f;
    public float minimumLaunchUpDot = 0.15f;

    [SerializeField] Gun_e_pool bulletpool;

    float nextMissileTime;

    public void TryFire(Vector3 direction, Vector3 platformVelocity, Transform target)
    {
        if (Time.time < nextMissileTime) return;
        if (direction.sqrMagnitude <= 0.001f) return;
        if (requireLineOfSight && !HasLineOfSight(target)) return;

        Fire(direction.normalized, platformVelocity, target);
        nextMissileTime = Time.time + missileCooldown;
    }

    void Fire(Vector3 direction, Vector3 platformVelocity, Transform target)
    {
        if (missileHardpoint == null) missileHardpoint = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (bulletpool == null) return;

        direction = ClampLaunchDirection(direction);
        Vector3 velocity = platformVelocity + direction * missileSpeed;
        GameObject missileObject = bulletpool.missilepull(missileHardpoint.position, velocity, missileLifeTime);
        Missile missile = missileObject.GetComponent<Missile>();

        if (missile == null) return;

        missile.missileInit(missileHardpoint.position, velocity, missileLifeTime);
        missile.StatusSetting(
            missilePower,
            missileAcceleration,
            missileMaxSpeed,
            missileTurnRate,
            missileBreakAngle,
            missileProportionalConstant
        );
        missile.isheatseeker = false;
        missile.target = target;
    }

    Vector3 ClampLaunchDirection(Vector3 direction)
    {
        if (direction.y >= minimumLaunchUpDot) return direction;

        direction.y = minimumLaunchUpDot;
        return direction.normalized;
    }

    bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;
        if (missileHardpoint == null) missileHardpoint = transform;

        Vector3 origin = missileHardpoint.position + Vector3.up * lineOfSightOriginOffset;
        Vector3 toTarget = target.position - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            toTarget / distance,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.transform == target || hit.transform.IsChildOf(target))
                return true;

            return false;
        }

        return true;
    }
}
