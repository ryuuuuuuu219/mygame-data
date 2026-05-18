using UnityEngine;

public class EnemyMissileShooter : MonoBehaviour
{
    public Transform missileHardpoint;
    public int salvoCount = 1;
    public float salvoSpreadAngle = 0f;
    public float salvoVerticalSpreadAngle = 0f;
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

        int count = Mathf.Max(1, salvoCount);
        for (int i = 0; i < count; i++)
        {
            Vector3 launchDirection = ClampLaunchDirection(GetSalvoDirection(direction, i, count));
            Vector3 velocity = platformVelocity + launchDirection * missileSpeed;
            GameObject missileObject = bulletpool.missilepull(missileHardpoint.position, velocity, missileLifeTime);
            Missile missile = missileObject.GetComponent<Missile>();

            if (missile == null) continue;

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
    }

    Vector3 GetSalvoDirection(Vector3 baseDirection, int index, int count)
    {
        if (count <= 1 || (Mathf.Abs(salvoSpreadAngle) <= 0.001f && Mathf.Abs(salvoVerticalSpreadAngle) <= 0.001f))
            return baseDirection;

        float t = count == 1 ? 0.5f : index / (float)(count - 1);
        float yaw = Mathf.Lerp(-salvoSpreadAngle * 0.5f, salvoSpreadAngle * 0.5f, t);
        float pitch = Mathf.Lerp(-salvoVerticalSpreadAngle * 0.5f, salvoVerticalSpreadAngle * 0.5f, Mathf.PingPong(index, 2f) * 0.5f);

        Vector3 right = Vector3.Cross(Vector3.up, baseDirection);
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.Cross(Vector3.forward, baseDirection);

        right.Normalize();
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, right);
        return (yawRotation * pitchRotation * baseDirection).normalized;
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
