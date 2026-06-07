using UnityEngine;

public class Orbitcruise : AircraftController
{
    [Header("Orbit")]
    public Vector3 center;
    public bool useStartDistanceAsRadius = true;
    public float orbitRadius = 600f;
    public float radiusCorrection = 0.45f;
    public float orbitDirection = 1f;

    [Header("Altitude")]
    public float minAltitude = 900f;
    public float maxAltitude = 3500f;
    public float altitudeCorrection = 0.35f;

    [Header("Motion")]
    public float cruiseThrottle = 1.1f;
    public float lowSpeedThrottle = 2.5f;
    public float yawOnlyStrength = 0.8f;

    Vector3 targetDirection;

    protected override void Start()
    {
        base.Start();

        if (useStartDistanceAsRadius)
        {
            Vector3 offset = transform.position - center;
            offset.y = 0f;
            if (offset.magnitude > 1f)
                orbitRadius = offset.magnitude;
        }

        targetDirection = transform.forward;
    }

    void Update()
    {
        Vector3 offset = transform.position - center;
        offset.y = 0f;
        Vector3 radial = SafeNormalize(offset, transform.right);

        Vector3 tangent = Vector3.Cross(Vector3.up, radial) *
                          Mathf.Sign(Mathf.Approximately(orbitDirection, 0f) ? 1f : orbitDirection);

        float radiusError = offset.magnitude - orbitRadius;
        Vector3 correction = -radial * Mathf.Clamp(radiusError / Mathf.Max(1f, orbitRadius), -1f, 1f) * radiusCorrection;

        Vector3 vertical = Vector3.zero;
        if (transform.position.y < minAltitude)
            vertical = Vector3.up * altitudeCorrection;
        else if (transform.position.y > maxAltitude)
            vertical = Vector3.down * altitudeCorrection;

        targetDirection = SafeNormalize(tangent + correction + vertical, transform.forward);
    }

    protected override Vector3 GetControlInput()
    {
        Vector3 localDir = transform.InverseTransformDirection(SafeNormalize(targetDirection, transform.forward));
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f) * yawOnlyStrength;

        return new Vector3(0f, 0f, yaw);
    }

    protected override float GetThrottleInput()
    {
        if (rb != null && rb.linearVelocity.magnitude < stallSpeed * 1.25f)
            return lowSpeedThrottle;

        return cruiseThrottle;
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
