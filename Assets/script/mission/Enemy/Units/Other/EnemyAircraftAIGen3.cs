using System.Collections.Generic;
using UnityEngine;

public class EnemyAircraftAIGen3 : AircraftController
{
    public enum CombatState
    {
        Pursuit,
        LeadPursuit,
        Offset,
        Extend,
        EvadeMissile,
        BarrelRoll,
        RecoverAltitude
    }

    [System.Serializable]
    public struct StateTuning
    {
        public CombatState state;
        public float enterDelay;
        public float minimumDuration;
    }

    [Header("Target")]
    public Transform target;
    public float detectRange = 6000f;
    public float lockAssistRange = 2500f;
    public float retargetInterval = 0.35f;
    public bool preferFcsTarget = true;

    [Header("Combat Ranges")]
    public float leadPursuitRange = 2200f;
    public float offsetRange = 900f;
    public float extendRange = 550f;
    public float safeExtendRange = 1400f;

    [Header("Altitude")]
    public float minAltitude = 900f;
    public float maxAltitude = 8500f;
    public float desiredAltitude = 2500f;

    [Header("Missile Evasion")]
    public float missileDetectRange = 1800f;
    public float missileCriticalTime = 3.5f;
    public float missileThreatDot = 0.45f;
    public float evadeForwardWeight = 0.25f;
    public float evadeTargetWeight = 0.2f;
    public float missileApproachAngle = 45f;
    public int barrelRollThreatCount = 1;
    public float barrelRollSeconds = 1.2f;
    public float barrelRollInput = 1f;
    public float attackApproachAngle = 45f;
    public float lagPursuitSeconds = 1.2f;

    [Header("Steering")]
    public float offsetRadius = 450f;
    public float offsetRefreshInterval = 1.2f;
    public float targetDirectionRefreshInterval = 0.12f;
    public float downwardPitchLimit = 0.3f;
    public float yawAssist = 1f;

    [Header("Throttle")]
    public float cruiseThrottle = 1f;
    public float attackThrottle = 3.2f;
    public float fullThrottle = 5f;
    public float brakeThrottle = 0.05f;

    [Header("State Tuning")]
    [SerializeField]
    StateTuning[] stateTunings =
    {
        new StateTuning { state = CombatState.Pursuit, enterDelay = 0.35f, minimumDuration = 0.6f },
        new StateTuning { state = CombatState.LeadPursuit, enterDelay = 0.2f, minimumDuration = 0.5f },
        new StateTuning { state = CombatState.Offset, enterDelay = 0.15f, minimumDuration = 0.7f },
        new StateTuning { state = CombatState.Extend, enterDelay = 0.25f, minimumDuration = 1.0f },
        new StateTuning { state = CombatState.EvadeMissile, enterDelay = 0.05f, minimumDuration = 0.8f },
        new StateTuning { state = CombatState.BarrelRoll, enterDelay = 0.02f, minimumDuration = 0.8f },
        new StateTuning { state = CombatState.RecoverAltitude, enterDelay = 0.1f, minimumDuration = 0.7f },
    };

    [Header("Debug")]
    [SerializeField] CombatState currentState = CombatState.Pursuit;
    [SerializeField] CombatState bookedState = CombatState.Pursuit;
    [SerializeField] float pursuitScore;
    [SerializeField] float leadPursuitScore;
    [SerializeField] float offsetScore;
    [SerializeField] float extendScore;
    [SerializeField] float evadeMissileScore;
    [SerializeField] float recoverAltitudeScore;
    [SerializeField] float missileThreat;
    [SerializeField] float targetDistance;
    [SerializeField] Vector3 targetDirection = Vector3.forward;
    [SerializeField] Vector3 missileEvadeDirection;

    FCS_e fcs;
    Rigidbody targetRb;
    float nextRetargetTime;
    float nextDirectionRefreshTime;
    float nextOffsetRefreshTime;
    float bookedStateTimer;
    float stateTimer;
    float interceptTimeCache;
    Vector3 offsetVector;
    readonly List<EnemyMissileThreatSensor.ThreatInfo> missileThreats = new();
    Vector3[] sensedMissileDirections = new Vector3[4];
    float barrelRollTimer;
    float barrelRollSign = 1f;

    protected override void Awake()
    {
        base.Awake();
        fcs = GetComponent<FCS_e>();
        targetDirection = transform.forward;
        offsetVector = transform.right * offsetRadius;
    }

    void Update()
    {
        RefreshTarget();

        if (target == null)
        {
            ResetDecision();
            targetDirection = transform.forward;
            return;
        }

        targetDistance = Vector3.Distance(transform.position, target.position);
        missileThreat = EvaluateMissileThreat(out missileEvadeDirection);

        CombatState nextState = ChooseState();
        ApplyStateTransition(nextState);

        if (Time.time >= nextDirectionRefreshTime)
        {
            nextDirectionRefreshTime = Time.time + targetDirectionRefreshInterval;
            targetDirection = BuildTargetDirection();
        }
    }

    protected override Vector3 GetControlInput()
    {
        return SteerToward(targetDirection);
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return cruiseThrottle;

        switch (currentState)
        {
            case CombatState.Pursuit:
                return fullThrottle;
            case CombatState.LeadPursuit:
                return attackThrottle;
            case CombatState.Offset:
                return targetDistance < offsetRange ? brakeThrottle : attackThrottle;
            case CombatState.Extend:
                return fullThrottle;
            case CombatState.EvadeMissile:
                return fullThrottle;
            case CombatState.BarrelRoll:
                return fullThrottle;
            case CombatState.RecoverAltitude:
                return fullThrottle;
            default:
                return cruiseThrottle;
        }
    }

    void RefreshTarget()
    {
        if (Time.time < nextRetargetTime && target != null) return;
        nextRetargetTime = Time.time + retargetInterval;

        if (preferFcsTarget)
        {
            if (fcs == null) fcs = GetComponent<FCS_e>();
            if (fcs != null)
            {
                if (fcs.target != null)
                {
                    target = fcs.target.transform;
                    return;
                }

                if (fcs.waytarget != null)
                {
                    target = fcs.waytarget.transform;
                    return;
                }
            }
        }

        GameObject best = null;
        float bestScore = float.MinValue;
        if (ObjectManager.Instance == null || ObjectManager.Instance.allies == null) return;

        foreach (GameObject candidate in ObjectManager.Instance.allies)
        {
            if (candidate == null || candidate == gameObject) continue;

            Vector3 toCandidate = candidate.transform.position - transform.position;
            float distance = toCandidate.magnitude;
            if (distance > detectRange) continue;

            float forwardDot = Vector3.Dot(GetForwardReference(), toCandidate.normalized);
            float score = (detectRange - distance) + Mathf.Max(0f, forwardDot) * lockAssistRange;
            if (score <= bestScore) continue;

            bestScore = score;
            best = candidate;
        }

        target = best != null ? best.transform : null;
    }

    CombatState ChooseState()
    {
        ResetDecision();

        Vector3 toTarget = target.position - transform.position;
        Vector3 toTargetDir = SafeNormalize(toTarget, transform.forward);
        float altitude = transform.position.y;
        float noseAngle = Vector3.Angle(GetForwardReference(), toTargetDir);
        float closureRate = GetClosureRate(toTargetDir);

        pursuitScore = Mathf.Clamp(targetDistance - leadPursuitRange, 0f, 2500f);
        leadPursuitScore = Mathf.Clamp(leadPursuitRange - targetDistance, 0f, leadPursuitRange)
            + Mathf.Clamp(70f - noseAngle, 0f, 70f) * 8f;
        offsetScore = Mathf.Clamp(offsetRange - targetDistance, 0f, offsetRange) * 1.4f
            + Mathf.Clamp(35f - noseAngle, 0f, 35f) * 6f;
        extendScore = Mathf.Clamp(extendRange - targetDistance, 0f, extendRange) * 2.2f
            + Mathf.Clamp(closureRate, 0f, 250f) * 3f;
        evadeMissileScore = missileThreat;
        float barrelRollScore = missileThreats.Count >= barrelRollThreatCount
            ? missileThreat + 900f
            : 0f;
        recoverAltitudeScore = 0f;

        if (altitude < minAltitude)
            recoverAltitudeScore = (minAltitude - altitude) * 2f;
        else if (altitude > maxAltitude)
            recoverAltitudeScore = (altitude - maxAltitude) * 2f;

        CombatState bestState = CombatState.Pursuit;
        float bestScore = pursuitScore;
        PickBetter(CombatState.LeadPursuit, leadPursuitScore, ref bestState, ref bestScore);
        PickBetter(CombatState.Offset, offsetScore, ref bestState, ref bestScore);
        PickBetter(CombatState.Extend, extendScore, ref bestState, ref bestScore);
        PickBetter(CombatState.EvadeMissile, evadeMissileScore, ref bestState, ref bestScore);
        PickBetter(CombatState.BarrelRoll, barrelRollScore, ref bestState, ref bestScore);
        PickBetter(CombatState.RecoverAltitude, recoverAltitudeScore, ref bestState, ref bestScore);

        return bestState;
    }

    void PickBetter(CombatState state, float score, ref CombatState bestState, ref float bestScore)
    {
        if (score <= bestScore) return;
        bestState = state;
        bestScore = score;
    }

    void ApplyStateTransition(CombatState nextState)
    {
        stateTimer += Time.deltaTime;
        if (nextState == currentState)
        {
            bookedState = currentState;
            bookedStateTimer = 0f;
            return;
        }

        if (stateTimer < GetTuning(currentState).minimumDuration) return;

        if (bookedState != nextState)
        {
            bookedState = nextState;
            bookedStateTimer = 0f;
        }

        bookedStateTimer += Time.deltaTime;
        if (bookedStateTimer < GetTuning(bookedState).enterDelay) return;

        currentState = bookedState;
        if (currentState == CombatState.BarrelRoll)
        {
            barrelRollTimer = barrelRollSeconds;
            barrelRollSign = Random.value < 0.5f ? -1f : 1f;
        }
        bookedStateTimer = 0f;
        stateTimer = 0f;
        nextDirectionRefreshTime = 0f;
    }

    Vector3 BuildTargetDirection()
    {
        if (target == null) return transform.forward;

        Vector3 direct = SafeNormalize(target.position - transform.position, transform.forward);
        Vector3 lead = CalculateLeadDirection(out Vector3 leadVector);

        switch (currentState)
        {
            case CombatState.Pursuit:
                return direct;
            case CombatState.LeadPursuit:
                return targetDistance < offsetRange ? GetLagPursuitDirection() : BuildAttackApproachDirection(lead, leadVector);
            case CombatState.Offset:
                return SafeNormalize(BuildAttackApproachDirection(lead, leadVector) + GetOffsetVector() * 0.001f, direct);
            case CombatState.Extend:
                return SafeNormalize(transform.position - target.position + Vector3.up * 120f, -direct);
            case CombatState.EvadeMissile:
                return SafeNormalize(
                    missileEvadeDirection.normalized
                    + GetForwardReference() * evadeForwardWeight
                    + lead * evadeTargetWeight,
                    transform.right);
            case CombatState.BarrelRoll:
                barrelRollTimer -= Time.deltaTime;
                if (barrelRollTimer <= 0f)
                    currentState = CombatState.EvadeMissile;
                return SafeNormalize(
                    GetForwardReference() * 0.65f
                    + missileEvadeDirection.normalized * 0.35f,
                    transform.forward);
            case CombatState.RecoverAltitude:
                return GetAltitudeRecoveryDirection();
            default:
                return direct;
        }
    }

    Vector3 GetOffsetVector()
    {
        if (Time.time < nextOffsetRefreshTime) return offsetVector;

        nextOffsetRefreshTime = Time.time + offsetRefreshInterval;
        Vector3 side = Vector3.Cross(Vector3.up, SafeNormalize(target.position - transform.position, transform.forward));
        if (side.sqrMagnitude < 0.001f) side = transform.right;

        side.Normalize();
        if (Random.value < 0.5f) side = -side;

        float vertical = Random.Range(-0.35f, 0.5f);
        offsetVector = (side + Vector3.up * vertical).normalized * offsetRadius;
        return offsetVector;
    }

    Vector3 BuildAttackApproachDirection(Vector3 leadDirection, Vector3 leadVector)
    {
        if (target == null) return leadDirection;

        Vector3 toTarget = SafeNormalize(target.position - transform.position, transform.forward);
        Vector3 side = Vector3.Cross(Vector3.up, toTarget);
        if (side.sqrMagnitude < 0.001f) side = transform.right;
        side.Normalize();
        if (Vector3.Dot(side, transform.right) < 0f) side = -side;

        Vector3 approach = Quaternion.AngleAxis(attackApproachAngle, Vector3.up) * toTarget;
        if (Vector3.Dot(approach, side) < 0f)
            approach = Quaternion.AngleAxis(-attackApproachAngle, Vector3.up) * toTarget;

        return SafeNormalize(leadDirection + leadVector.normalized * 0.15f + approach * 0.35f, leadDirection);
    }

    Vector3 GetLagPursuitDirection()
    {
        if (target == null) return transform.forward;

        Rigidbody otherRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = otherRb != null ? otherRb.linearVelocity : target.forward * 120f;
        Vector3 lagPoint = target.position - targetVelocity * lagPursuitSeconds;
        return SafeNormalize(lagPoint - transform.position, transform.forward);
    }

    Vector3 GetAltitudeRecoveryDirection()
    {
        if (transform.position.y < minAltitude)
            return SafeNormalize(Vector3.up + transform.forward * 0.3f, Vector3.up);

        if (transform.position.y > maxAltitude)
            return SafeNormalize(Vector3.down + transform.forward * 0.35f, Vector3.down);

        Vector3 desiredPoint = new Vector3(transform.position.x, desiredAltitude, transform.position.z) + transform.forward * 500f;
        return SafeNormalize(desiredPoint - transform.position, transform.forward);
    }

    float EvaluateMissileThreat(out Vector3 evadeDirection)
    {
        evadeDirection = Vector3.zero;
        Sencing(out _, 4);
        if (missileThreats.Count == 0) return 0f;

        EnemyMissileThreatSensor.ThreatInfo highest = missileThreats[0];
        for (int i = 1; i < missileThreats.Count; i++)
        {
            if (missileThreats[i].score > highest.score)
                highest = missileThreats[i];
        }

        evadeDirection = highest.evadeDirection;
        return highest.score;
    }

    Vector3 CalculateLeadDirection(out Vector3 leadVector)
    {
        leadVector = Vector3.zero;
        if (target == null || rb == null) return transform.forward;

        if (targetRb == null || targetRb.transform != target)
            targetRb = target.GetComponent<Rigidbody>();

        if (targetRb == null)
            return SafeNormalize(target.position - transform.position, transform.forward);

        float bulletSpeed = 200f;
        if (fcs == null) fcs = GetComponent<FCS_e>();
        if (fcs != null)
            bulletSpeed = Mathf.Max(1f, fcs.bulletSpeed);

        Vector3 muzzlePos = transform.position;
        Vector3 bulletVel0 = rb.linearVelocity + transform.forward * bulletSpeed;
        float t = PredictInterceptTime(muzzlePos, bulletVel0, targetRb.position, targetRb.linearVelocity, bulletSpeed);
        Vector3 aimPoint = targetRb.position + targetRb.linearVelocity * t;
        leadVector = aimPoint - transform.position;
        return SafeNormalize(leadVector, target.position - transform.position);
    }

    float PredictInterceptTime(Vector3 muzzlePos, Vector3 bulletVel0, Vector3 targetPos, Vector3 targetVel, float bulletSpeed)
    {
        if (bulletSpeed <= 0.01f) return 0f;

        float t = interceptTimeCache > 0f
            ? interceptTimeCache
            : Vector3.Distance(muzzlePos, targetPos) / (bulletSpeed + Mathf.Max(rb.linearVelocity.magnitude, 1f));

        for (int i = 0; i < 5; i++)
        {
            Vector3 futureTarget = targetPos + targetVel * t;
            Vector3 bulletFuture = muzzlePos + bulletVel0 * t + 0.5f * Physics.gravity * t * t;
            if (Vector3.Distance(bulletFuture, futureTarget) < 0.5f) break;

            float distance = Vector3.Distance(muzzlePos, futureTarget);
            t = distance / (bulletSpeed + Mathf.Max(rb.linearVelocity.magnitude, 1f));
        }

        if (float.IsNaN(t) || float.IsInfinity(t) || t < 0f)
            t = 0f;

        interceptTimeCache = Mathf.Clamp(t, 0f, 30f);
        return interceptTimeCache;
    }

    Vector3 SteerToward(Vector3 worldDirection)
    {
        Vector3 localDir = transform.InverseTransformDirection(SafeNormalize(worldDirection, transform.forward));
        float downFactor = Mathf.Clamp01(-localDir.y);

        float roll = Mathf.Clamp(localDir.x, -1f, 1f);
        float pitchScale = Mathf.Lerp(downwardPitchLimit, 1f, 1f - downFactor);
        float pitch = Mathf.Clamp(localDir.y, -1f, 1f) * pitchScale;
        float yaw = Mathf.Clamp(localDir.x, -1f, 1f) * downFactor * Mathf.Abs(roll) * yawAssist;
        if (currentState == CombatState.BarrelRoll)
        {
            roll = Mathf.Clamp(barrelRollInput * barrelRollSign, -1f, 1f);
            pitch = Mathf.Clamp(pitch + 0.25f, -1f, 1f);
        }

        return new Vector3(pitch, roll, yaw);
    }

    float GetClosureRate(Vector3 toTargetDir)
    {
        if (target == null || rb == null) return 0f;
        Rigidbody otherRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = otherRb != null ? otherRb.linearVelocity : Vector3.zero;
        Vector3 relativeVelocity = rb.linearVelocity - targetVelocity;
        return Vector3.Dot(relativeVelocity, toTargetDir);
    }

    Vector3 GetForwardReference()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 25f)
            return rb.linearVelocity.normalized;

        return transform.forward;
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

    StateTuning GetTuning(CombatState state)
    {
        for (int i = 0; i < stateTunings.Length; i++)
        {
            if (stateTunings[i].state == state)
                return stateTunings[i];
        }

        return new StateTuning
        {
            state = state,
            enterDelay = 0.2f,
            minimumDuration = 0.5f
        };
    }

    public int sencing(out Vector3[] approachDirections, int maxCount)
    {
        return Sencing(out approachDirections, maxCount);
    }

    public int Sencing(out Vector3[] approachDirections, int maxCount)
    {
        int count = EnemyMissileThreatSensor.SenseIncomingMissiles(
            transform,
            rb,
            missileThreats,
            missileDetectRange,
            missileApproachAngle,
            missileCriticalTime);

        int outputCount = Mathf.Clamp(maxCount, 0, count);
        if (sensedMissileDirections.Length != outputCount)
            sensedMissileDirections = new Vector3[outputCount];

        for (int i = 0; i < outputCount; i++)
            sensedMissileDirections[i] = missileThreats[i].approachDirection;

        approachDirections = sensedMissileDirections;
        return count;
    }

    void ResetDecision()
    {
        pursuitScore = 0f;
        leadPursuitScore = 0f;
        offsetScore = 0f;
        extendScore = 0f;
        evadeMissileScore = 0f;
        recoverAltitudeScore = 0f;
        missileThreat = 0f;
        targetDistance = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + targetDirection.normalized * 500f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + missileEvadeDirection.normalized * 400f);

        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
