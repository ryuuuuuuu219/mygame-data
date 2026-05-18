using UnityEngine;

public class EnemyGunShooter : MonoBehaviour
{
    public Transform gunMuzzle;
    public int barrelCount = 1;
    public float barrelArcAngle = 0f;
    public float bulletSpeed = 200f;
    public float fireRate = 0.1f;
    public float spreadAngle = 2f;
    public float bulletSize = 1f;
    public float bulletLifetime = 3f;

    [SerializeField] Gun_e_pool bulletpool;

    float nextFireTime;

    public void TryFire(Vector3 direction, Vector3 platformVelocity)
    {
        TryFire(direction, platformVelocity, null, Vector3.zero);
    }

    public void TryFire(Vector3 direction, Vector3 platformVelocity, Transform target, Vector3 targetVelocity)
    {
        if (Time.time < nextFireTime) return;
        if (direction.sqrMagnitude <= 0.001f && target == null) return;

        Vector3 fireDirection = target != null
            ? CalculateLeadDirection(target.position, platformVelocity, targetVelocity)
            : direction.normalized;

        Fire(fireDirection, platformVelocity);
        nextFireTime = Time.time + fireRate;
    }

    Vector3 CalculateLeadDirection(Vector3 targetPosition, Vector3 platformVelocity, Vector3 targetVelocity)
    {
        if (gunMuzzle == null) gunMuzzle = transform;

        Vector3 toTarget = targetPosition - gunMuzzle.position;
        Vector3 relativeVelocity = targetVelocity - platformVelocity;

        float a = Vector3.Dot(relativeVelocity, relativeVelocity) - bulletSpeed * bulletSpeed;
        float b = 2f * Vector3.Dot(toTarget, relativeVelocity);
        float c = Vector3.Dot(toTarget, toTarget);
        float t = 0f;

        if (Mathf.Abs(a) < 0.001f)
        {
            if (Mathf.Abs(b) > 0.001f)
                t = -c / b;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant >= 0f)
            {
                float sqrt = Mathf.Sqrt(discriminant);
                float t1 = (-b - sqrt) / (2f * a);
                float t2 = (-b + sqrt) / (2f * a);
                t = Mathf.Min(t1 > 0f ? t1 : Mathf.Infinity, t2 > 0f ? t2 : Mathf.Infinity);
            }
        }

        if (!float.IsFinite(t) || t <= 0f)
            t = toTarget.magnitude / Mathf.Max(bulletSpeed, 1f);

        Vector3 aimPoint = targetPosition + relativeVelocity * t;
        return (aimPoint - gunMuzzle.position).normalized;
    }

    void Fire(Vector3 direction, Vector3 platformVelocity)
    {
        if (gunMuzzle == null) gunMuzzle = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (bulletpool == null) return;

        int count = Mathf.Max(1, barrelCount);
        for (int i = 0; i < count; i++)
        {
            Vector3 barrelDirection = GetBarrelDirection(direction, i, count);
            Vector3 shootDirection = ApplySpread(barrelDirection);
            Vector3 velocity = platformVelocity + shootDirection * bulletSpeed;
            bulletpool.bulletpull(bulletSize, gunMuzzle.position, velocity, bulletLifetime);
        }
    }

    Vector3 GetBarrelDirection(Vector3 baseDirection, int index, int count)
    {
        if (count <= 1 || Mathf.Abs(barrelArcAngle) <= 0.001f)
            return baseDirection;

        float step = barrelArcAngle >= 359.9f
            ? barrelArcAngle / count
            : barrelArcAngle / Mathf.Max(1, count - 1);
        float offset = barrelArcAngle >= 359.9f
            ? step * index
            : -barrelArcAngle * 0.5f + step * index;

        return (Quaternion.AngleAxis(offset, Vector3.up) * baseDirection).normalized;
    }

    Vector3 ApplySpread(Vector3 direction)
    {
        if (spreadAngle <= 0f) return direction;

        Vector2 randomCircle = Random.insideUnitCircle * spreadAngle;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.Cross(Vector3.forward, direction);

        right.Normalize();
        Vector3 up = Vector3.Cross(direction, right).normalized;

        Quaternion yaw = Quaternion.AngleAxis(randomCircle.x, up);
        Quaternion pitch = Quaternion.AngleAxis(randomCircle.y, right);

        return (yaw * pitch * direction).normalized;
    }
}
