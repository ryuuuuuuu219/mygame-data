using System.Collections.Generic;
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
    public List<Vector3> biasDirections = new();
    public List<Vector3> biasMoveDirections = new();
    public float biasDegreeSpeed = 30f;
    public float biasDegree = 5f;
    public bool showDebugAimLine = false;
    public float debugAimLineLength = 3000f;

    [SerializeField] Gun_e_pool bulletpool;

    float nextFireTime;
    readonly List<LineRenderer> debugAimLines = new();
    LineRenderer debugLeadLine;
    readonly List<Vector3> biasOffsets = new();
    readonly List<Vector3> biasMoveOffsets = new();
    readonly List<float> biasAngles = new();
    readonly List<Vector3> previousLeadDirections = new();

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
        UpdateDebugAimLines(fireDirection);

        if (Time.time < nextFireTime) return;

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

    Vector3 ApplyLeadBias(Vector3 leadDirection, int barrelIndex)
    {
        if (biasDegree <= 0f) return leadDirection;

        Vector3 center = leadDirection.normalized;
        EnsureLeadBias(center, barrelIndex);
        ApplyLeadDirectionDelta(center, barrelIndex);

        float step = biasDegreeSpeed * Time.deltaTime;
        if (step <= 0f)
            return biasDirections[barrelIndex].normalized;

        biasDirections[barrelIndex] = Vector3.RotateTowards(
            biasDirections[barrelIndex].normalized,
            biasMoveDirections[barrelIndex].normalized,
            step * Mathf.Deg2Rad,
            0f
        ).normalized;

        if (Vector3.Angle(biasDirections[barrelIndex], biasMoveDirections[barrelIndex]) <= 0.1f)
            RerollLeadBiasMoveDirection(center, barrelIndex);

        return biasDirections[barrelIndex].normalized;
    }

    void EnsureLeadBias(Vector3 center, int barrelIndex)
    {
        EnsureBiasListMinSize(barrelIndex + 1);

        if (biasDirections[barrelIndex].sqrMagnitude <= 0.001f ||
            biasMoveDirections[barrelIndex].sqrMagnitude <= 0.001f)
        {
            RerollLeadBias(center, barrelIndex, biasDirections[barrelIndex]);
        }
    }

    void EnsureBiasListSize(int count)
    {
        while (biasDirections.Count < count)
            biasDirections.Add(Vector3.zero);
        while (biasMoveDirections.Count < count)
            biasMoveDirections.Add(Vector3.zero);
        while (biasOffsets.Count < count)
            biasOffsets.Add(Vector3.zero);
        while (biasMoveOffsets.Count < count)
            biasMoveOffsets.Add(Vector3.zero);
        while (biasAngles.Count < count)
            biasAngles.Add(0f);
        while (previousLeadDirections.Count < count)
            previousLeadDirections.Add(Vector3.zero);

        if (biasDirections.Count > count)
            biasDirections.RemoveRange(count, biasDirections.Count - count);
        if (biasMoveDirections.Count > count)
            biasMoveDirections.RemoveRange(count, biasMoveDirections.Count - count);
        if (biasOffsets.Count > count)
            biasOffsets.RemoveRange(count, biasOffsets.Count - count);
        if (biasMoveOffsets.Count > count)
            biasMoveOffsets.RemoveRange(count, biasMoveOffsets.Count - count);
        if (biasAngles.Count > count)
            biasAngles.RemoveRange(count, biasAngles.Count - count);
        if (previousLeadDirections.Count > count)
            previousLeadDirections.RemoveRange(count, previousLeadDirections.Count - count);
    }

    void EnsureBiasListMinSize(int count)
    {
        while (biasDirections.Count < count)
            biasDirections.Add(Vector3.zero);
        while (biasMoveDirections.Count < count)
            biasMoveDirections.Add(Vector3.zero);
        while (biasOffsets.Count < count)
            biasOffsets.Add(Vector3.zero);
        while (biasMoveOffsets.Count < count)
            biasMoveOffsets.Add(Vector3.zero);
        while (biasAngles.Count < count)
            biasAngles.Add(0f);
        while (previousLeadDirections.Count < count)
            previousLeadDirections.Add(Vector3.zero);
    }

    void ApplyLeadDirectionDelta(Vector3 center, int barrelIndex)
    {
        Vector3 previousCenter = previousLeadDirections[barrelIndex];
        previousLeadDirections[barrelIndex] = center;

        if (previousCenter.sqrMagnitude <= 0.001f)
            return;

        Quaternion leadDelta = Quaternion.FromToRotation(previousCenter.normalized, center);
        biasDirections[barrelIndex] = (leadDelta * biasDirections[barrelIndex]).normalized;
        biasMoveDirections[barrelIndex] = (leadDelta * biasMoveDirections[barrelIndex]).normalized;
    }

    void RerollLeadBias(Vector3 center, int barrelIndex, Vector3 previousBiasDirection)
    {
        Vector3 previousOffset = GetBiasOffset(center, previousBiasDirection);
        Vector3 nextOffset = Random.onUnitSphere;

        if (previousOffset.sqrMagnitude > 0.001f)
            nextOffset = PickRelativeBiasOffset(previousOffset.normalized);

        biasOffsets[barrelIndex] = GetStableOffset(center, nextOffset);
        biasAngles[barrelIndex] = biasDegree;
        biasDirections[barrelIndex] = RotateFromCenter(center, biasOffsets[barrelIndex], biasDegree);
        biasMoveOffsets[barrelIndex] = Vector3.Angle(previousBiasDirection, biasDirections[barrelIndex]) >= 160f
            ? Vector3.zero
            : GetStableOffset(center, Random.onUnitSphere);
        biasMoveDirections[barrelIndex] = biasMoveOffsets[barrelIndex].sqrMagnitude <= 0.001f
            ? center
            : RotateFromCenter(center, biasMoveOffsets[barrelIndex], biasDegree);
        previousLeadDirections[barrelIndex] = center;
    }

    void RerollLeadBiasMoveDirection(Vector3 center, int barrelIndex)
    {
        Vector3 currentOffset = GetBiasOffset(center, biasDirections[barrelIndex]);
        Vector3 nextOffset = currentOffset.sqrMagnitude > 0.001f
            ? PickRelativeBiasOffset(currentOffset.normalized)
            : Random.onUnitSphere;

        nextOffset = GetStableOffset(center, nextOffset);
        Vector3 nextDirection = RotateFromCenter(center, nextOffset, biasDegree);
        biasMoveOffsets[barrelIndex] = Vector3.Angle(biasDirections[barrelIndex], nextDirection) >= 160f
            ? Vector3.zero
            : nextOffset;
        biasMoveDirections[barrelIndex] = biasMoveOffsets[barrelIndex].sqrMagnitude <= 0.001f
            ? center
            : RotateFromCenter(center, biasMoveOffsets[barrelIndex], biasDegree);
        biasOffsets[barrelIndex] = currentOffset;
        biasAngles[barrelIndex] = Vector3.Angle(center, biasDirections[barrelIndex]);
    }

    Vector3 PickRelativeBiasOffset(Vector3 previousOffset)
    {
        Vector3 nextOffset = Random.onUnitSphere;
        for (int i = 0; i < 12 && Vector3.Angle(previousOffset, nextOffset) < 45f; i++)
            nextOffset = Random.onUnitSphere;

        return Vector3.Angle(previousOffset, nextOffset) >= 160f
            ? -previousOffset
            : nextOffset.normalized;
    }

    Vector3 GetBiasOffset(Vector3 center, Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        Vector3 offset = Vector3.ProjectOnPlane(direction.normalized, center);
        if (offset.sqrMagnitude <= 0.001f)
            offset = Vector3.Cross(center, Vector3.up);
        if (offset.sqrMagnitude <= 0.001f)
            offset = Vector3.Cross(center, Vector3.forward);

        return offset.normalized;
    }

    Vector3 GetStableOffset(Vector3 center, Vector3 offset)
    {
        Vector3 stableOffset = Vector3.ProjectOnPlane(offset, center);
        if (stableOffset.sqrMagnitude <= 0.001f)
            stableOffset = Vector3.Cross(center, Vector3.up);
        if (stableOffset.sqrMagnitude <= 0.001f)
            stableOffset = Vector3.Cross(center, Vector3.forward);

        return stableOffset.normalized;
    }

    Vector3 RotateFromCenter(Vector3 center, Vector3 offset, float degrees)
    {
        Vector3 axis = Vector3.ProjectOnPlane(offset, center);
        if (axis.sqrMagnitude <= 0.001f)
            axis = Vector3.Cross(center, Vector3.up);
        if (axis.sqrMagnitude <= 0.001f)
            axis = Vector3.Cross(center, Vector3.forward);

        return (Quaternion.AngleAxis(degrees, axis.normalized) * center).normalized;
    }

    void UpdateDebugAimLine(Vector3 direction, int barrelIndex)
    {
        if (!showDebugAimLine) return;

        if (gunMuzzle == null) gunMuzzle = transform;
        EnsureDebugAimLine(barrelIndex);

        Vector3 start = gunMuzzle.position;
        LineRenderer line = debugAimLines[barrelIndex];
        line.SetPosition(0, start);
        line.SetPosition(1, start + direction.normalized * debugAimLineLength);
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
        EnsureBiasListSize(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 barrelDirection = GetBarrelDirection(direction, i, count);
            if (useLeadBias)
            {
                ApplyLeadBias(barrelDirection, i);
                barrelDirection = biasDirections[i].normalized;
            }

            UpdateDebugAimLine(barrelDirection, i);
        }
    }

    void EnsureDebugLeadLine()
    {
        if (debugLeadLine != null) return;

        debugLeadLine = CreateDebugLine("DebugLeadLine", Color.white);
    }

    void EnsureDebugAimLine(int barrelIndex)
    {
        while (debugAimLines.Count <= barrelIndex)
            debugAimLines.Add(null);

        if (debugAimLines[barrelIndex] != null) return;

        debugAimLines[barrelIndex] = CreateDebugLine($"DebugAimLine_{barrelIndex + 1:00}", Color.red);
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
            if (useLeadBias)
                barrelDirection = biasDirections[i].sqrMagnitude > 0.001f
                    ? biasDirections[i].normalized
                    : ApplyLeadBias(barrelDirection, i);

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
