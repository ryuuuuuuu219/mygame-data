using UnityEngine;

public class Orbitcruise : AircraftController
{
    [Header("Orbit")]
    public Vector3 center;
    public bool useStartDistanceAsRadius = true;
    public float orbitRadius = 600f;
    public float orbitDirection = 1f;

    [Header("Altitude")]
    public float minAltitude = 900f;
    public float maxAltitude = 3500f;
    public float altitudeCorrection = 0.35f;

    [Header("Control")]
    public bool disableGravity = true;
    public float cruiseThrottle = 1.1f;
    public float lowSpeedThrottle = 2.5f;
    public float yawAssist = 0.8f;
    public float downwardPitchLimit = 0.35f;

    public Vector3 targetPosition;

    Vector3 targetDirection;

    protected override void Start()
    {
        base.Start();

        if (disableGravity && rb != null)
            rb.useGravity = false;

        if (useStartDistanceAsRadius)
        {
            Vector3 offset = transform.position - center;
            offset.y = 0f;
            if (offset.magnitude > 1f)
                orbitRadius = offset.magnitude;
        }

        targetDirection = transform.forward;
    }

    protected override void FixedUpdate()
    {
        if (disableGravity && rb != null)
            rb.useGravity = false;

        base.FixedUpdate();
    }

    void Update()
    {
        Vector3 toCenter = center - transform.position;
        toCenter.y = 0f;

        float distance = toCenter.magnitude;
        Vector3 horizontalForward = transform.forward;
        horizontalForward.y = 0f;
        horizontalForward = SafeNormalize(horizontalForward, transform.forward);

        if (distance < orbitRadius)
        {
            targetPosition = CalculateForwardCircleIntersection(horizontalForward);
        }
        else
        {
            float targetAngle = Mathf.Atan2(toCenter.z, toCenter.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            bool isLeft = Mathf.DeltaAngle(targetAngle, currentAngle) < 0f;
            float side = Mathf.Approximately(orbitDirection, 0f)
                ? (isLeft ? -1f : 1f)
                : Mathf.Sign(orbitDirection);

            float theta = Mathf.Acos(Mathf.Clamp(orbitRadius / Mathf.Max(distance, 1f), -1f, 1f)) * Mathf.Rad2Deg;
            float orbitAngle = targetAngle + theta * side;
            targetPosition = center + new Vector3(
                Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
                0f,
                Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius);
            targetPosition.y = transform.position.y;
        }

        Vector3 vertical = Vector3.zero;
        if (transform.position.y < minAltitude)
            vertical = Vector3.up * altitudeCorrection;
        else if (transform.position.y > maxAltitude)
            vertical = Vector3.down * altitudeCorrection;

        Vector3 targetVector = targetPosition - transform.position;
        targetVector.y = 0f;
        targetDirection = SafeNormalize(targetVector + vertical, transform.forward);
    }

    protected override Vector3 GetControlInput()
    {
        Vector3 localDir = transform.InverseTransformDirection(SafeNormalize(targetDirection, transform.forward));
        float downFactor = Mathf.Clamp01(-localDir.y);
        float pitchScale = Mathf.Lerp(downwardPitchLimit, 1f, 1f - downFactor);

        float pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchScale;
        float roll = Mathf.Clamp(localDir.x, -1f, 1f);
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f) * yawAssist;

        return new Vector3(pitch, roll, yaw);
    }

    protected override float GetThrottleInput()
    {
        if (rb != null && rb.linearVelocity.magnitude < stallSpeed)
            return lowSpeedThrottle;

        return cruiseThrottle;
    }

    Vector3 CalculateForwardCircleIntersection(Vector3 fallbackDirection)
    {
        Vector3 origin = transform.position;
        Vector3 direction = rb != null ? rb.linearVelocity : Velocity;
        direction.y = 0f;

        if (direction.magnitude < Mathf.Max(1f, stallSpeed * 0.5f))
            direction = fallbackDirection;

        direction = SafeNormalize(direction, fallbackDirection);

        Vector2 origin2 = new(origin.x, origin.z);
        Vector2 center2 = new(center.x, center.z);
        Vector2 direction2 = new(direction.x, direction.z);
        if (direction2.sqrMagnitude < 0.0001f)
            direction2 = new Vector2(fallbackDirection.x, fallbackDirection.z).normalized;
        else
            direction2.Normalize();

        Vector2 fromCenter = origin2 - center2;
        float radius = Mathf.Max(1f, orbitRadius);
        float b = Vector2.Dot(fromCenter, direction2);
        float c = Vector2.Dot(fromCenter, fromCenter) - radius * radius;
        float discriminant = b * b - c;

        if (discriminant < 0f)
            return origin + new Vector3(direction2.x, 0f, direction2.y) * radius;

        float t = -b + Mathf.Sqrt(discriminant);
        if (t < 0f)
            t = -b - Mathf.Sqrt(discriminant);
        t = Mathf.Max(0f, t);

        Vector2 hit = origin2 + direction2 * t;
        return new Vector3(hit.x, origin.y, hit.y);
    }

    Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        if (value.sqrMagnitude > 0.0001f && IsFinite(value))
            return value.normalized;

        if (fallback.sqrMagnitude > 0.0001f && IsFinite(fallback))
            return fallback.normalized;

        return Vector3.forward;
    }

    bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsNaN(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x)
            && !float.IsInfinity(value.y)
            && !float.IsInfinity(value.z);
    }
}
