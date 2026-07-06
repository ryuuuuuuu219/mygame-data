using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyAircraftAIGen4 : AircraftController
{
    public enum CombatState
    {
        LeadPursuit,
        Offset,
        Brake,
        Extend,
        EvadeMissile,
        AoALimitRelease,
        RecoverAltitude
    }

    [System.Serializable]
    public struct StateTuning
    {
        public CombatState state;
        public float enterDelay;
        public float minimumDuration;
        public float needToChoiceTime;
        public float maxTime;
    }

    [System.Serializable]
    public struct StateRuntime
    {
        public CombatState state;
        public bool conditionMet;
        public float remainTime;
        public float weight;
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

    [Header("Altitude")]
    public float minAltitude = 900f;
    public float maxAltitude = 8500f;
    public float desiredAltitude = 2500f;

    [Header("Missile Evasion")]
    public float missileDetectRange = 1800f;
    public float missileCriticalTime = 3.5f;
    public float evadeForwardWeight = 0.25f;
    public float evadeTargetWeight = 0.2f;
    public float missileApproachAngle = 45f;
    public float missileBarrelRollChance = 0.3f;
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
        new StateTuning { state = CombatState.LeadPursuit, enterDelay = 0.2f, minimumDuration = 0.5f },
        new StateTuning { state = CombatState.Offset, enterDelay = 0.15f, minimumDuration = 0.7f },
        new StateTuning { state = CombatState.Brake, enterDelay = 0.08f, minimumDuration = 0.6f },
        new StateTuning { state = CombatState.Extend, enterDelay = 0.25f, minimumDuration = 1.0f },
        new StateTuning { state = CombatState.EvadeMissile, enterDelay = 0.05f, minimumDuration = 0.8f },
        new StateTuning { state = CombatState.AoALimitRelease, enterDelay = 0.08f, minimumDuration = 0.6f },
        new StateTuning { state = CombatState.RecoverAltitude, enterDelay = 0.1f, minimumDuration = 0.7f },
    };
    [SerializeField] StateRuntime[] stateRuntime;
    [SerializeField] float enterDelayRandomAddMax = 0.15f;
    [SerializeField] float minimumDurationRandomAddMax = 0.4f;

    [Header("Decision")]
    [SerializeField] float threatenedAngle = 70f;
    [SerializeField] float brakeDistance = 250f;
    [SerializeField] float missileThreatNearDistance = 100f;
    [SerializeField] float missileThreatFarDistance = 2000f;
    [SerializeField] float randomManeuverCheckInterval = 0.5f;
    [SerializeField] float randomManeuverChance = 0.04f;
    [SerializeField] float randomManeuverCooldown = 12f;
    [SerializeField] float tacticSwitchCheckInterval = 1f;
    [SerializeField] float tacticSwitchChance = 0.05f;
    [SerializeField] float tacticSwitchCooldown = 30f;
    [SerializeField] float stateRemainRecoverSeconds = 10f;

    [Header("Debug")]
    [SerializeField] CombatState currentState = CombatState.LeadPursuit;
    [SerializeField] CombatState bookedState = CombatState.LeadPursuit;
    [SerializeField] bool evadeMissileUseBarrelRoll;
    [SerializeField] float missileThreat;
    [SerializeField] float targetDistance;
    [FormerlySerializedAs("targetDirection")]
    [SerializeField] Vector3 commandedFlightDirection = Vector3.forward;
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
    float barrelRollSign = 1f;
    float nextRandomManeuverCheckTime;
    float nextRandomManeuverTime;
    float nextTacticSwitchCheckTime;
    float nextTacticSwitchTime;
    bool invertOffensiveTactic;

    protected override void Awake()
    {
        base.Awake();
        TuningOverwrite();
        InitializeStateRuntime();
        fcs = GetComponent<FCS_e>();
        commandedFlightDirection = transform.forward;
        offsetVector = transform.right * offsetRadius;
    }

    void TuningOverwrite()
    {
        StateTuning[] overwrittenTunings = new StateTuning[stateTunings.Length];
        for (int i = 0; i < stateTunings.Length; i++)
        {
            StateTuning tuning = stateTunings[i];
            tuning.enterDelay += Random.Range(0f, enterDelayRandomAddMax);
            tuning.minimumDuration += Random.Range(0f, minimumDurationRandomAddMax);
            tuning.needToChoiceTime = 6f;
            tuning.maxTime = 20f;
            overwrittenTunings[i] = tuning;
        }

        stateTunings = overwrittenTunings;
    }

    void InitializeStateRuntime()
    {
        StateRuntime[] initializedRuntime = new StateRuntime[stateTunings.Length];
        for (int i = 0; i < stateTunings.Length; i++)
        {
            int existingIndex = FindRuntimeIndex(stateTunings[i].state);
            StateRuntime runtime = existingIndex >= 0
                ? stateRuntime[existingIndex]
                : new StateRuntime();

            runtime.state = stateTunings[i].state;
            runtime.conditionMet = false;
            runtime.remainTime = 0f;
            runtime.weight = 0f;
            initializedRuntime[i] = runtime;
        }

        stateRuntime = initializedRuntime;
    }

    void Update()
    {
        RefreshTarget();

        if (target == null)
        {
            ResetDecision();
            missileThreat = 0f;
            targetDistance = 0f;
            commandedFlightDirection = transform.forward;
            return;
        }

        targetDistance = Vector3.Distance(transform.position, target.position);
        missileThreat = EvaluateMissileThreat(out Vector3 evaluatedMissileEvadeDirection);
        missileEvadeDirection = evaluatedMissileEvadeDirection;
        UpdateStateChoiceTimes();

        CombatState nextState = ChooseState();
        ApplyStateTransition(nextState);

        if (Time.time >= nextDirectionRefreshTime)
        {
            nextDirectionRefreshTime = Time.time + targetDirectionRefreshInterval;
            commandedFlightDirection = BuildTargetDirection();
        }
    }

    protected override Vector3 GetControlInput()
    {
        return SteerToward(commandedFlightDirection);
    }

    protected override float GetThrottleInput()
    {
        if (target == null) return cruiseThrottle;

        switch (currentState)
        {
            case CombatState.LeadPursuit:
                return attackThrottle;
            case CombatState.Offset:
                return targetDistance < offsetRange ? brakeThrottle : attackThrottle;
            case CombatState.Brake:
                return brakeThrottle;
            case CombatState.Extend:
                return fullThrottle;
            case CombatState.EvadeMissile:
                return fullThrottle;
            case CombatState.AoALimitRelease:
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
        Vector3 targetToMeDir = SafeNormalize(transform.position - target.position, -target.forward);
        float altitude = transform.position.y;
        float toTargetAngle = Vector3.Angle(GetForwardReference(), toTargetDir);
        float targetNoseToMeAngle = Vector3.Angle(target.forward, targetToMeDir);
        bool baseOffensive = targetNoseToMeAngle > toTargetAngle;
        UpdateOffensiveTactic(baseOffensive);
        bool offensive = invertOffensiveTactic ? !baseOffensive : baseOffensive;
        bool threatened = targetNoseToMeAngle < threatenedAngle;
        bool targetBehindMe = toTargetAngle > 90f;
        bool leadPursuitReady = offensive && toTargetAngle < 30f && targetDistance < leadPursuitRange;
        bool brakeReady = targetBehindMe && threatened && targetDistance < brakeDistance * 0.6f;
        bool extendReady = targetBehindMe && !threatened && targetDistance > extendRange;
        float altitudeDangerScore = GetAltitudeDangerScore(altitude);
        UpdateStateConditions(offensive, threatened, targetBehindMe, leadPursuitReady, brakeReady, extendReady, altitudeDangerScore);

        if (TryPickRandomManeuver(offensive, threatened, targetBehindMe, brakeReady, extendReady, toTargetAngle))
            return currentState;

        if (TryGetStateCandidate(out CombatState candidateState))
            return candidateState;

        return currentState;
    }

    bool TryPickRandomManeuver(bool offensive, bool threatened, bool targetBehindMe, bool brakeReady, bool extendReady, float toTargetAngle)
    {
        if (Time.time < nextRandomManeuverTime || Time.time < nextRandomManeuverCheckTime)
            return false;

        nextRandomManeuverCheckTime = Time.time + randomManeuverCheckInterval;
        if (Random.value >= randomManeuverChance)
            return false;

        nextRandomManeuverTime = Time.time + randomManeuverCooldown;

        if (missileThreat > 0f)
        {
            ForceState(CombatState.EvadeMissile);
            return true;
        }

        if (offensive)
        {
            ForceState(toTargetAngle > 12f ? CombatState.AoALimitRelease : CombatState.LeadPursuit);
            return true;
        }

        if (threatened || targetBehindMe)
        {
            float roll = Random.value;
            if (roll < 0.4f && brakeReady)
                ForceState(CombatState.Brake);
            else if (extendReady)
                ForceState(CombatState.Extend);
            else
                ForceState(CombatState.Offset);

            return true;
        }

        return false;
    }

    void UpdateOffensiveTactic(bool baseOffensive)
    {
        if (!baseOffensive) return;
        if (Time.time < nextTacticSwitchTime || Time.time < nextTacticSwitchCheckTime) return;

        nextTacticSwitchCheckTime = Time.time + tacticSwitchCheckInterval;
        if (Random.value >= tacticSwitchChance) return;

        invertOffensiveTactic = !invertOffensiveTactic;
        nextTacticSwitchTime = Time.time + tacticSwitchCooldown;
    }

    void ForceState(CombatState state)
    {
        currentState = state;
        bookedState = state;
        bookedStateTimer = 0f;
        stateTimer = 0f;
        nextDirectionRefreshTime = 0f;
        ResetStateRemainTime(state);
        PickEvadeMissileManeuver(state);
    }

    void PickEvadeMissileManeuver(CombatState state)
    {
        if (state != CombatState.EvadeMissile)
        {
            evadeMissileUseBarrelRoll = false;
            return;
        }

        evadeMissileUseBarrelRoll = Random.value < missileBarrelRollChance;
        barrelRollSign = Random.value < 0.5f ? -1f : 1f;
    }

    void UpdateStateChoiceTimes()
    {
        for (int i = 0; i < stateTunings.Length; i++)
        {
            StateTuning tuning = stateTunings[i];
            int runtimeIndex = FindRuntimeIndex(tuning.state);
            if (runtimeIndex < 0) continue;

            StateRuntime runtime = stateRuntime[runtimeIndex];
            float needToChoiceTime = Mathf.Max(0.01f, tuning.needToChoiceTime);
            float maxTime = Mathf.Max(needToChoiceTime, tuning.maxTime);
            float recoverSpeed = tuning.state == currentState
                ? 1f
                : needToChoiceTime / Mathf.Max(0.01f, stateRemainRecoverSeconds);

            runtime.remainTime = Mathf.Clamp(runtime.remainTime + Time.deltaTime * recoverSpeed, 0f, maxTime);
            runtime.weight = GetStateWeight(tuning, runtime);
            stateRuntime[runtimeIndex] = runtime;
        }
    }

    float GetStateWeight(StateTuning tuning, StateRuntime runtime)
    {
        float needToChoiceTime = Mathf.Max(0.01f, tuning.needToChoiceTime);
        if (!runtime.conditionMet || runtime.remainTime <= needToChoiceTime)
            return 0f;

        return runtime.remainTime / needToChoiceTime;
    }

    void ResetStateRemainTime(CombatState state)
    {
        int runtimeIndex = FindRuntimeIndex(state);
        if (runtimeIndex < 0) return;

        StateRuntime runtime = stateRuntime[runtimeIndex];
        runtime.remainTime = 0f;
        runtime.weight = 0f;
        stateRuntime[runtimeIndex] = runtime;
    }

    void UpdateStateConditions(bool offensive, bool threatened, bool targetBehindMe, bool leadPursuitReady, bool brakeReady, bool extendReady, float altitudeDangerScore)
    {
        SetStateCondition(CombatState.LeadPursuit, offensive && leadPursuitReady);
        SetStateCondition(CombatState.Offset, !offensive && threatened);
        SetStateCondition(CombatState.Brake, brakeReady);
        SetStateCondition(CombatState.Extend, extendReady);
        SetStateCondition(CombatState.EvadeMissile, missileThreat > 0f);
        SetStateCondition(CombatState.AoALimitRelease, offensive && !leadPursuitReady);
        SetStateCondition(CombatState.RecoverAltitude, altitudeDangerScore > 0f);
    }

    void SetStateCondition(CombatState state, bool conditionMet)
    {
        int tuningIndex = FindTuningIndex(state);
        int runtimeIndex = FindRuntimeIndex(state);
        if (tuningIndex < 0 || runtimeIndex < 0) return;

        StateRuntime runtime = stateRuntime[runtimeIndex];
        runtime.conditionMet = conditionMet;
        runtime.weight = GetStateWeight(stateTunings[tuningIndex], runtime);
        stateRuntime[runtimeIndex] = runtime;
    }

    bool TryGetStateCandidate(out CombatState state)
    {
        float totalWeight = 0f;
        for (int i = 0; i < stateRuntime.Length; i++)
            totalWeight += stateRuntime[i].weight;

        if (totalWeight <= 0f)
        {
            state = currentState;
            return false;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < stateRuntime.Length; i++)
        {
            roll -= stateRuntime[i].weight;
            if (roll > 0f) continue;

            state = stateRuntime[i].state;
            return true;
        }

        state = currentState;
        return false;
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

        if (currentState == CombatState.AoALimitRelease && IsTargetInFront(12f))
        {
            currentState = nextState;
            bookedState = nextState;
            bookedStateTimer = 0f;
            stateTimer = 0f;
            nextDirectionRefreshTime = 0f;
            ResetStateRemainTime(currentState);
            PickEvadeMissileManeuver(currentState);
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
        ResetStateRemainTime(currentState);
        PickEvadeMissileManeuver(currentState);
        bookedStateTimer = 0f;
        stateTimer = 0f;
        nextDirectionRefreshTime = 0f;
    }

    Vector3 BuildTargetDirection()
    {
        if (target == null) return transform.forward;

        Vector3 directTargetDirection = SafeNormalize(target.position - transform.position, transform.forward);
        Vector3 interceptDirection = CalculateLeadDirection(out Vector3 vectorToInterceptPoint);

        switch (currentState)
        {
            case CombatState.LeadPursuit:
                return targetDistance < offsetRange ? GetLagPursuitDirection() : BuildAttackApproachDirection(interceptDirection, vectorToInterceptPoint);
            case CombatState.Offset:
                return SafeNormalize(BuildAttackApproachDirection(interceptDirection, vectorToInterceptPoint) + GetOffsetVector() * 0.001f, directTargetDirection);
            case CombatState.Brake:
                return SafeNormalize(GetForwardReference() * 0.7f + GetOffsetVector() * 0.0015f, transform.forward);
            case CombatState.Extend:
                return SafeNormalize(transform.position - target.position + Vector3.up * 120f, -directTargetDirection);
            case CombatState.EvadeMissile:
                if (evadeMissileUseBarrelRoll)
                {
                    return SafeNormalize(
                        GetForwardReference() * 0.65f
                        + missileEvadeDirection.normalized * 0.35f,
                        transform.forward);
                }

                return SafeNormalize(
                    missileEvadeDirection.normalized
                    + GetForwardReference() * evadeForwardWeight
                    + interceptDirection * evadeTargetWeight,
                    transform.right);
            case CombatState.AoALimitRelease:
                return directTargetDirection;
            case CombatState.RecoverAltitude:
                return GetAltitudeRecoveryDirection();
            default:
                return directTargetDirection;
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

    Vector3 BuildAttackApproachDirection(Vector3 interceptDirection, Vector3 vectorToInterceptPoint)
    {
        if (target == null) return interceptDirection;

        Vector3 directTargetDirection = SafeNormalize(target.position - transform.position, transform.forward);
        Vector3 lateralDirection = Vector3.Cross(Vector3.up, directTargetDirection);
        if (lateralDirection.sqrMagnitude < 0.001f) lateralDirection = transform.right;
        lateralDirection.Normalize();
        if (Vector3.Dot(lateralDirection, transform.right) < 0f) lateralDirection = -lateralDirection;

        Vector3 approachDirection = Quaternion.AngleAxis(attackApproachAngle, Vector3.up) * directTargetDirection;
        if (Vector3.Dot(approachDirection, lateralDirection) < 0f)
            approachDirection = Quaternion.AngleAxis(-attackApproachAngle, Vector3.up) * directTargetDirection;

        return SafeNormalize(interceptDirection + vectorToInterceptPoint.normalized * 0.15f + approachDirection * 0.35f, interceptDirection);
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
        SenseIncomingMissiles(out _, 4);
        if (missileThreats.Count == 0) return 0f;

        EnemyMissileThreatSensor.ThreatInfo highest = missileThreats[0];
        for (int i = 1; i < missileThreats.Count; i++)
        {
            if (GetMissileDistanceThreat(missileThreats[i]) > GetMissileDistanceThreat(highest))
                highest = missileThreats[i];
        }

        evadeDirection = highest.evadeDirection;
        return GetMissileDistanceThreat(highest);
    }

    float GetMissileDistanceThreat(EnemyMissileThreatSensor.ThreatInfo threat)
    {
        float range = Mathf.Max(1f, missileThreatFarDistance - missileThreatNearDistance);
        return Mathf.Clamp01((missileThreatFarDistance - threat.dist) / range) * 1000f;
    }

    float GetAltitudeDangerScore(float altitude)
    {
        float score = 0f;
        if (altitude < minAltitude)
            score = (minAltitude - altitude) * 2f;
        else if (altitude > maxAltitude)
            score = (altitude - maxAltitude) * 2f;

        if (rb != null)
        {
            float predictedY = altitude + rb.linearVelocity.y * 2f;
            if (rb.linearVelocity.y < -10f && predictedY < minAltitude)
                score = Mathf.Max(score, (minAltitude - predictedY) * 2f);
        }

        if (transform.forward.y < -0.2f && altitude < minAltitude + 400f)
            score = Mathf.Max(score, 1200f);

        return score;
    }

    Vector3 CalculateLeadDirection(out Vector3 vectorToInterceptPoint)
    {
        vectorToInterceptPoint = Vector3.zero;
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
        vectorToInterceptPoint = aimPoint - transform.position;
        return SafeNormalize(vectorToInterceptPoint, target.position - transform.position);
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
        if (currentState == CombatState.AoALimitRelease)
        {
            float pitchAxisError = Mathf.Abs(localDir.x);
            roll = Mathf.Clamp(localDir.x * 2.2f, -1f, 1f);
            pitch *= Mathf.Clamp01(1f - pitchAxisError * 1.6f - downFactor > 0 ? 1f : 0f);
            yaw *= 0.35f;
        }

        if (currentState == CombatState.EvadeMissile && evadeMissileUseBarrelRoll)
        {
            roll = Mathf.Clamp(barrelRollInput * barrelRollSign, -1f, 1f);
            pitch = Mathf.Clamp(pitch + 0.25f, -1f, 1f);
        }

        return new Vector3(pitch, roll, yaw);
    }

    bool IsTargetInFront(float angle)
    {
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude <= 0.001f) return true;

        return Vector3.Angle(GetForwardReference(), toTarget.normalized) <= angle;
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
        int index = FindTuningIndex(state);
        if (index >= 0)
            return stateTunings[index];

        return new StateTuning
        {
            state = state,
            enterDelay = 0.2f,
            minimumDuration = 0.5f,
            needToChoiceTime = 6f,
            maxTime = 20f
        };
    }

    int FindTuningIndex(CombatState state)
    {
        for (int i = 0; i < stateTunings.Length; i++)
        {
            if (stateTunings[i].state == state)
                return i;
        }

        return -1;
    }

    int FindRuntimeIndex(CombatState state)
    {
        if (stateRuntime == null) return -1;

        for (int i = 0; i < stateRuntime.Length; i++)
        {
            if (stateRuntime[i].state == state)
                return i;
        }

        return -1;
    }

    protected override bool GetLimiter()
    {
        return currentState != CombatState.AoALimitRelease;
    }

    public int SenseIncomingMissiles(out Vector3[] approachDirections, int maxCount)
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
        evadeMissileUseBarrelRoll = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + commandedFlightDirection.normalized * 500f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + missileEvadeDirection.normalized * 400f);

        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
