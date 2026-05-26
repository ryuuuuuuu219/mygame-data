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
    public bool useLeadBias = false;
    public bool showDebugAimLine = false;
    public float debugAimLineLength = 3000f;

    [SerializeField] Gun_e_pool bulletpool;
    [SerializeField] EnemyGunLeadBiasAddon leadBiasAddon;

    float nextFireTime;
    LineRenderer debugLeadLine;

    public void TryFire(Vector3 direction, Vector3 platformVelocity)
    {
        TryFire(direction, platformVelocity, null, Vector3.zero);
    }

    public void TryFire(Vector3 direction, Vector3 platformVelocity, Transform target, Vector3 targetVelocity)
    {
        if (direction.sqrMagnitude <= 0.001f && target == null) return;

        Vector3 fireDirection = target != null
            ? CalculateLeadDirection(target.position, platformVelocity, targetVelocity)
            : direction.normalized;

        UpdateDebugLeadLine(fireDirection);
        if (useLeadBias)
            GetLeadBiasAddon(true);

        UpdateDebugAimLines(fireDirection);
        bool fireDirectionHasBias = leadBiasAddon != null && Mathf.Max(1, barrelCount) <= 1;
        if (fireDirectionHasBias)
            fireDirection = ApplyBiasAngle(fireDirection, leadBiasAddon.GetBiasAngle(0));

        if (Time.time < nextFireTime) return;

        Fire(fireDirection, platformVelocity, fireDirectionHasBias);
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

    void UpdateDebugLeadLine(Vector3 direction)
    {
        if (!showDebugAimLine) return;

        if (gunMuzzle == null) gunMuzzle = transform;
        EnsureDebugLeadLine();

        Vector3 start = gunMuzzle.position;
        debugLeadLine.SetPosition(0, start);
        debugLeadLine.SetPosition(1, start + direction.normalized * debugAimLineLength);
    }

    void UpdateDebugAimLines(Vector3 direction)
    {
        int count = Mathf.Max(1, barrelCount);
        leadBiasAddon?.EnsureBarrelCount(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 barrelDirection = GetBarrelDirection(direction, i, count);
            if (leadBiasAddon != null)
            {
                Vector2 biasAngle = leadBiasAddon.UpdateBiasAngle(i);
                Vector3 biasDirection = ApplyBiasAngle(barrelDirection, biasAngle);
                Vector3 targetDirection = ApplyBiasAngle(barrelDirection, leadBiasAddon.GetBiasTargetAngle(i));
                leadBiasAddon.UpdateDebugLines(gunMuzzle, debugAimLineLength, i, showDebugAimLine, biasDirection, targetDirection);
            }
        }
    }

    void EnsureDebugLeadLine()
    {
        if (debugLeadLine != null) return;

        debugLeadLine = CreateDebugLine("DebugLeadLine", Color.white);
    }

    LineRenderer CreateDebugLine(string lineName, Color color)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 5f;
        line.endWidth = 5f;
        line.startColor = color;
        line.endColor = color;
        line.material = new Material(Shader.Find("Sprites/Default"));
        return line;
    }

    EnemyGunLeadBiasAddon GetLeadBiasAddon(bool createIfMissing)
    {
        if (leadBiasAddon != null) return leadBiasAddon;

        leadBiasAddon = GetComponent<EnemyGunLeadBiasAddon>();
        if (leadBiasAddon == null && createIfMissing)
        {
            leadBiasAddon = gameObject.AddComponent<EnemyGunLeadBiasAddon>();
        }

        return leadBiasAddon;
    }

    void Fire(Vector3 direction, Vector3 platformVelocity, bool directionHasBias)
    {
        if (gunMuzzle == null) gunMuzzle = transform;
        if (bulletpool == null)
            bulletpool = FindFirstObjectByType<Gun_e_pool>();
        if (bulletpool == null) return;

        int count = Mathf.Max(1, barrelCount);
        if (useLeadBias)
            GetLeadBiasAddon(true);
        leadBiasAddon?.EnsureBarrelCount(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 barrelDirection = GetBarrelDirection(direction, i, count);
            if (leadBiasAddon != null && !directionHasBias)
                barrelDirection = ApplyBiasAngle(barrelDirection, leadBiasAddon.GetBiasAngle(i));

            Vector3 shootDirection = ApplySpread(barrelDirection);
            Vector3 velocity = platformVelocity + shootDirection * bulletSpeed;
            bulletpool.bulletpull(bulletSize, gunMuzzle.position, velocity, bulletLifetime);
        }
        GeneratedAudioManager.Play(GeneratedAudioCue.EnemyGunFire, gunMuzzle.position, 0.45f);
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

    Vector3 ApplyBiasAngle(Vector3 direction, Vector2 biasAngle)
    {
        if (biasAngle.sqrMagnitude <= 0.001f)
            return direction.normalized;

        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.Cross(Vector3.forward, forward);

        right.Normalize();
        Vector3 up = Vector3.Cross(forward, right).normalized;

        Quaternion yaw = Quaternion.AngleAxis(biasAngle.x, up);
        Quaternion pitch = Quaternion.AngleAxis(biasAngle.y, right);
        return (yaw * pitch * forward).normalized;
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
