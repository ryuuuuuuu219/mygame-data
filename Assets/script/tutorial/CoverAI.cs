using UnityEngine;

public class CoverAI : AircraftController
{
    [Header("Cover Point")]
    public Transform coverPointObject;
    public Vector3 coverPoint;
    public bool useVectorWhenObjectIsNull = true;

    [Header("Behavior")]
    public float followBreakDistance = 900f;
    public float coverRadius = 220f;
    public float orbitSide = 1f;
    public float cruiseThrottle = 1.2f;
    public float returnThrottle = 3.5f;
    public float closeThrottle = 0.8f;
    public float minAltitude = 700f;
    public float maxAltitude = 5000f;

    [Header("Steering")]
    public float yawAssist = 0.6f;
    public float downwardPitchLimit = 0.35f;

    Vector3 targetDirection;

    protected override void Start()
    {
        base.Start();
        targetDirection = transform.forward;
    }

    void Update()
    {
        Vector3 point = GetCoverPoint();
        Vector3 toPoint = point - transform.position;
        float distance = toPoint.magnitude;

        if (distance > followBreakDistance)
        {
            targetDirection = SafeNormalize(toPoint, transform.forward);
            return;
        }

        Vector3 horizontalToPoint = toPoint;
        horizontalToPoint.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, SafeNormalize(horizontalToPoint, transform.forward)) *
                          Mathf.Sign(Mathf.Approximately(orbitSide, 0f) ? 1f : orbitSide);

        Vector3 radialCorrection = Vector3.zero;
        if (horizontalToPoint.magnitude > coverRadius)
            radialCorrection = SafeNormalize(horizontalToPoint, Vector3.zero) * 0.45f;
        else if (horizontalToPoint.magnitude < coverRadius * 0.65f)
            radialCorrection = -SafeNormalize(horizontalToPoint, Vector3.zero) * 0.35f;

        Vector3 altitudeCorrection = Vector3.zero;
        if (transform.position.y < minAltitude)
            altitudeCorrection = Vector3.up * 0.8f;
        else if (transform.position.y > maxAltitude)
            altitudeCorrection = Vector3.down * 0.6f;

        targetDirection = SafeNormalize(tangent + radialCorrection + altitudeCorrection, transform.forward);
    }

    protected override Vector3 GetControlInput()
    {
        return SteerToward(targetDirection);
    }

    protected override float GetThrottleInput()
    {
        float distance = Vector3.Distance(transform.position, GetCoverPoint());
        if (distance > followBreakDistance) return returnThrottle;
        if (distance < coverRadius * 0.7f) return closeThrottle;
        return cruiseThrottle;
    }

    Vector3 GetCoverPoint()
    {
        if (coverPointObject != null)
            return coverPointObject.position;

        if (useVectorWhenObjectIsNull)
            return coverPoint;

        return transform.position + transform.forward * coverRadius;
    }

    Vector3 SteerToward(Vector3 worldDirection)
    {
        Vector3 localDir = transform.InverseTransformDirection(SafeNormalize(worldDirection, transform.forward));
        float downFactor = Mathf.Clamp01(-localDir.y);

        float roll = Mathf.Clamp(localDir.x, -1f, 1f);
        float pitchScale = Mathf.Lerp(downwardPitchLimit, 1f, 1f - downFactor);
        float pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchScale;
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f) * downFactor * Mathf.Abs(roll) * yawAssist;

        return new Vector3(pitch, roll, yaw);
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
