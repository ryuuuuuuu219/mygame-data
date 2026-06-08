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
    public float downwardPitchLimit = 0.35f;

    [Header("Debug")]
    public bool showTargetDebugLines = true;
    public Vector3 targetPosition;
    public float debugLineWidth = 4f;

    Vector3 targetDirection;
    LineRenderer centerToTargetLine;
    LineRenderer selfToTargetLine;

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

    void OnDestroy()
    {
        if (centerToTargetLine != null)
            Destroy(centerToTargetLine.gameObject);
        if (selfToTargetLine != null)
            Destroy(selfToTargetLine.gameObject);
    }

    void Update()
    {
        Vector3 toCenter = center - transform.position;
        toCenter.y = 0f;

        float distance = toCenter.magnitude;
        Vector3 horizontalForward = transform.forward;
        horizontalForward.y = 0f;
        horizontalForward = SafeNormalize(horizontalForward, transform.forward);

        if (distance < 1f)
        {
            float side = Mathf.Sign(Mathf.Approximately(orbitDirection, 0f) ? 1f : orbitDirection);
            targetPosition = center + transform.right * side * Mathf.Max(orbitRadius, 1f);
        }
        else
        {
            float targetAngle = Mathf.Atan2(toCenter.z, toCenter.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            bool isLeft = Mathf.DeltaAngle(targetAngle, currentAngle) < 0f;
            float side = Mathf.Approximately(orbitDirection, 0f)
                ? (isLeft ? -1f : 1f)
                : Mathf.Sign(orbitDirection);

            if (distance < orbitRadius)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized) * side;
                targetPosition = center + tangent * orbitRadius;
            }
            else
            {
                float theta = Mathf.Acos(Mathf.Clamp(orbitRadius / distance, -1f, 1f)) * Mathf.Rad2Deg;
                float orbitAngle = targetAngle + theta * side;
                Vector3 tangentPoint = center + new Vector3(
                    Mathf.Cos(orbitAngle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Sin(orbitAngle * Mathf.Deg2Rad)) * orbitRadius;
                targetPosition = tangentPoint;
            }
        }

        Vector3 vertical = Vector3.zero;
        if (transform.position.y < minAltitude)
            vertical = Vector3.up * altitudeCorrection;
        else if (transform.position.y > maxAltitude)
            vertical = Vector3.down * altitudeCorrection;

        Vector3 targetVector = targetPosition - transform.position;
        targetVector.y = 0f;
        targetDirection = SafeNormalize(targetVector + vertical, transform.forward);
        UpdateDebugLines();
    }

    protected override Vector3 GetControlInput()
    {
        Vector3 localDir = transform.InverseTransformDirection(SafeNormalize(targetDirection, transform.forward));
        float downFactor = Mathf.Clamp01(-localDir.y);

        float roll = Mathf.Clamp(localDir.x, -1f, 1f);
        float pitchScale = Mathf.Lerp(downwardPitchLimit, 1f, 1f - downFactor);
        float pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchScale;
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f) * Mathf.Lerp(0.25f, 1f, downFactor) * yawOnlyStrength;

        return new Vector3(pitch, roll, yaw);
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

    void UpdateDebugLines()
    {
        if (!showTargetDebugLines)
        {
            SetLineEnabled(centerToTargetLine, false);
            SetLineEnabled(selfToTargetLine, false);
            return;
        }

        EnsureDebugLines();

        SetLine(centerToTargetLine, center, targetPosition, Color.blue);
        SetLine(selfToTargetLine, transform.position, targetPosition, Color.red);
    }

    void EnsureDebugLines()
    {
        if (centerToTargetLine == null)
            centerToTargetLine = CreateDebugLine("OrbitCenterToTargetLine", Color.blue);
        if (selfToTargetLine == null)
            selfToTargetLine = CreateDebugLine("OrbitSelfToTargetLine", Color.red);
    }

    LineRenderer CreateDebugLine(string objectName, Color color)
    {
        GameObject obj = new(objectName);
        obj.transform.SetParent(null, true);

        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = debugLineWidth;
        line.endWidth = debugLineWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    void SetLine(LineRenderer line, Vector3 start, Vector3 end, Color color)
    {
        if (line == null) return;

        line.enabled = true;
        line.startWidth = debugLineWidth;
        line.endWidth = debugLineWidth;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    void SetLineEnabled(LineRenderer line, bool enabled)
    {
        if (line != null)
            line.enabled = enabled;
    }
}
