using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(0)]
[RequireComponent(typeof(AirCombatRandomRepositioner))]
public class AirCombatBehaviorAnalyzer : MonoBehaviour
{
    [Header("Targets")]
    public GameObject playerObject;
    public GameObject enemyObject;
    public InputManager inputManager;

    [Header("Analysis Settings")]
    [SerializeField] float leadPredictionTime = 1.2f;
    [SerializeField] float inViewAngle = 65f;
    [SerializeField] float lowPitchInputThreshold = 0.3f;
    [SerializeField] float pitchAxisAlignedAngle = 10f;
    [SerializeField] float closeRange = 1200f;
    [SerializeField] float axisOffsetRollThreshold = 0.55f;
    [SerializeField] float missileDetectRange = 1800f;
    [SerializeField] float missileApproachAngle = 45f;
    [SerializeField] float missileCriticalTime = 3.5f;
    [SerializeField] float trackingSecondsSearchRange = 8f;
    [SerializeField] float trackingSecondsSearchStep = 0.1f;

    [Header("Input Snapshot")]
    [SerializeField] float pitchInput;
    [SerializeField] float rollInput;
    [SerializeField] float yawInput;
    [SerializeField] float throttleInput;
    [SerializeField] float rollPitchInputRatio;

    [Header("Relative Geometry")]
    [SerializeField] float distance;
    [SerializeField] float playerSpeed;
    [SerializeField] float enemySpeed;
    [SerializeField] float relativeSpeed;
    [SerializeField] float closureRate;
    [SerializeField] float playerThrottle;
    [SerializeField] float enemyThrottle;
    [SerializeField] float playerNoseToEnemyAngle;
    [SerializeField] float enemyNoseToPlayerAngle;
    [SerializeField] float signedAzimuthError;
    [SerializeField] float signedElevationError;
    [SerializeField] float bankAngle;
    [SerializeField] Vector3 playerLocalAngularVelocity;
    [SerializeField] float playerVelocityToEnemyAngle;
    [SerializeField] float enemyVelocityToPlayerAngle;
    [SerializeField] Vector3 playerVelocity;
    [SerializeField] Vector3 enemyVelocity;
    [SerializeField] Vector3 relativeVelocity;
    [SerializeField] float velocityDirectionAngle;
    [SerializeField] float velocityLineClosestDistance;
    [SerializeField] float normalizedVelocityLineSeparation;
    [SerializeField] float normalizedVelocityTripleProduct;
    [SerializeField] float timeToClosestApproach;
    [SerializeField] float predictedClosestApproachDistance;
    [SerializeField] VelocityLineRelation velocityLineRelation;
    [SerializeField] VelocityEncounterType velocityEncounterType;

    [Header("Pursuit Analysis")]
    [SerializeField] bool closing;
    [SerializeField] bool leadPredictionPreferred;
    [SerializeField] float purePursuitAngle;
    [SerializeField] float leadPursuitAngle;
    [SerializeField] float lastLeadPreferredDistance;
    [SerializeField] Vector3 purePursuitDirection;
    [SerializeField] Vector3 leadPursuitDirection;
    [SerializeField] Vector3 predictedEnemyPosition;

    [Header("Missile Evasion")]
    [SerializeField] int enemyIncomingMissileCount;
    [SerializeField] float enemyMissileDistance;
    [SerializeField] float enemyMissileTimeToImpact;
    [SerializeField] Vector3 enemyMissileApproachDirection;
    [SerializeField] Vector3 enemyMissileEvadeDirection;

    [Header("Pitch Axis Analysis")]
    [SerializeField] bool enemyInView;
    [SerializeField] float pitchAxisToEnemyAngle;
    [SerializeField] float pitchAxisToEnemyVelocityAngle;
    [SerializeField] float pitchAxisErrorChangePerSecond;
    [SerializeField] float estimatedPitchAxisAlignTime;
    [SerializeField] float inViewVelocityAxisAlignFrequency;
    [SerializeField] int inViewVelocityAxisAlignedSamples;
    [SerializeField] int inViewVelocityAxisTotalSamples;
    [SerializeField] float outOfViewTrackingSeconds;
    [SerializeField] float outOfViewTrackingErrorAngle;

    [Header("Low Pitch Input Snapshot")]
    [SerializeField] bool lowPitchInput;
    [SerializeField] float lowPitchPitchAxisToEnemyAngle;
    [SerializeField] float lowPitchRollPitchRatio;
    [SerializeField] float lowPitchAxisPrecision;

    [Header("Scissors / Rolling Scissors")]
    [SerializeField] bool scissorsAxisOffsetCandidate;
    [SerializeField] int axisOffsetRollEvents;
    [SerializeField] float axisOffsetRollFrequency;
    [SerializeField] float lastAxisOffsetAngle;
    [SerializeField] float averageAxisOffsetAngle;
    [SerializeField] float rollingScissorsCandidateTime;

    Transform playerTransform;
    Transform enemyTransform;
    Rigidbody playerRb;
    Rigidbody enemyRb;
    AircraftController playerAircraft;
    AircraftController enemyAircraft;
    readonly List<EnemyMissileThreatSensor.ThreatInfo> missileThreats = new();

    float previousPitchAxisError;
    float previousRollAbs;
    float axisOffsetAngleSum;
    float analysisStartTime;

    public bool HasValidTargets => playerTransform != null && enemyTransform != null;
    public float PitchInput => pitchInput;
    public float RollInput => rollInput;
    public float YawInput => yawInput;
    public float ThrottleInput => throttleInput;
    public float RollPitchInputRatio => rollPitchInputRatio;
    public float Distance => distance;
    public float PlayerSpeed => playerSpeed;
    public float EnemySpeed => enemySpeed;
    public float RelativeSpeed => relativeSpeed;
    public float ClosureRate => closureRate;
    public float PlayerThrottle => playerThrottle;
    public float EnemyThrottle => enemyThrottle;
    public float PlayerNoseToEnemyAngle => playerNoseToEnemyAngle;
    public float EnemyNoseToPlayerAngle => enemyNoseToPlayerAngle;
    public float SignedAzimuthError => signedAzimuthError;
    public float SignedElevationError => signedElevationError;
    public float BankAngle => bankAngle;
    public Vector3 PlayerLocalAngularVelocity => playerLocalAngularVelocity;
    public float PlayerVelocityToEnemyAngle => playerVelocityToEnemyAngle;
    public float EnemyVelocityToPlayerAngle => enemyVelocityToPlayerAngle;
    public Vector3 PlayerVelocity => playerVelocity;
    public Vector3 EnemyVelocity => enemyVelocity;
    public Vector3 RelativeVelocity => relativeVelocity;
    public float VelocityDirectionAngle => velocityDirectionAngle;
    public float VelocityLineClosestDistance => velocityLineClosestDistance;
    public float NormalizedVelocityLineSeparation => normalizedVelocityLineSeparation;
    public float NormalizedVelocityTripleProduct => normalizedVelocityTripleProduct;
    public float TimeToClosestApproach => timeToClosestApproach;
    public float PredictedClosestApproachDistance => predictedClosestApproachDistance;
    public VelocityLineRelation VelocityLineRelation => velocityLineRelation;
    public VelocityEncounterType VelocityEncounterType => velocityEncounterType;
    public bool EnemyInView => enemyInView;
    public float PitchAxisToEnemyAngle => pitchAxisToEnemyAngle;
    public float PitchAxisToEnemyVelocityAngle => pitchAxisToEnemyVelocityAngle;
    public float OutOfViewTrackingSeconds => outOfViewTrackingSeconds;
    public float OutOfViewTrackingErrorAngle => outOfViewTrackingErrorAngle;
    public float LowPitchInputThreshold => lowPitchInputThreshold;
    public bool LowPitchInput => lowPitchInput;
    public float LowPitchPitchAxisToEnemyAngle => lowPitchPitchAxisToEnemyAngle;
    public float LowPitchRollPitchRatio => lowPitchRollPitchRatio;
    public float LowPitchAxisPrecision => lowPitchAxisPrecision;

    void Awake()
    {
        analysisStartTime = Time.time;
    }

    void LateUpdate()
    {
        CacheReferences();
        if (playerTransform == null || enemyTransform == null)
            return;

        UpdateInputSnapshot();
        UpdateRelativeGeometry();
        UpdatePursuitAnalysis();
        UpdateMissileAnalysis();
        UpdatePitchAxisAnalysis();
        UpdateLowPitchInputSnapshot();
        UpdateScissorsAnalysis();
    }

    void CacheReferences()
    {
        if (inputManager == null)
            inputManager = InputManager.Instance;

        if (playerObject != null && playerObject.transform != playerTransform)
        {
            playerTransform = playerObject.transform;
            playerRb = playerObject.GetComponent<Rigidbody>();
            playerAircraft = playerObject.GetComponent<AircraftController>();
        }

        if (enemyObject != null && enemyObject.transform != enemyTransform)
        {
            enemyTransform = enemyObject.transform;
            enemyRb = enemyObject.GetComponent<Rigidbody>();
            enemyAircraft = enemyObject.GetComponent<AircraftController>();
        }
    }

    void UpdateInputSnapshot()
    {
        if (inputManager == null)
        {
            pitchInput = 0f;
            rollInput = 0f;
            yawInput = 0f;
            throttleInput = 0f;
            rollPitchInputRatio = 0f;
            return;
        }

        pitchInput = inputManager.verticalL;
        rollInput = inputManager.horizontalL;

        float rawYaw = inputManager.r2 - inputManager.l2;
        if (Mathf.Abs(rawYaw) <= 0.001f)
            rawYaw = (inputManager.altr2 ? 1f : 0f) + (inputManager.altl2 ? -1f : 0f);

        yawInput = rawYaw * 0.2f;
        throttleInput = inputManager.accel;
        rollPitchInputRatio = Mathf.Abs(rollInput) / Mathf.Max(0.001f, Mathf.Abs(pitchInput));
    }

    void UpdateRelativeGeometry()
    {
        playerVelocity = GetVelocity(playerRb);
        enemyVelocity = GetVelocity(enemyRb);
        Vector3 toEnemy = enemyTransform.position - playerTransform.position;
        Vector3 toPlayer = -toEnemy;
        Vector3 toEnemyDirection = SafeNormalize(toEnemy, playerTransform.forward);
        Vector3 toPlayerDirection = SafeNormalize(toPlayer, enemyTransform.forward);

        distance = toEnemy.magnitude;
        playerSpeed = playerVelocity.magnitude;
        enemySpeed = enemyVelocity.magnitude;
        relativeSpeed = (playerVelocity - enemyVelocity).magnitude;
        closureRate = Vector3.Dot(playerVelocity - enemyVelocity, toEnemyDirection);
        closing = closureRate > 0f;
        playerThrottle = playerAircraft != null ? playerAircraft.throttle : 0f;
        enemyThrottle = enemyAircraft != null ? enemyAircraft.throttle : 0f;
        playerNoseToEnemyAngle = Vector3.Angle(playerTransform.forward, toEnemyDirection);
        enemyNoseToPlayerAngle = Vector3.Angle(enemyTransform.forward, toPlayerDirection);
        Vector3 localToEnemy = playerTransform.InverseTransformDirection(toEnemy);
        signedAzimuthError = Mathf.Atan2(localToEnemy.x, localToEnemy.z) * Mathf.Rad2Deg;
        signedElevationError = Mathf.Atan2(localToEnemy.y, new Vector2(localToEnemy.x, localToEnemy.z).magnitude) * Mathf.Rad2Deg;
        bankAngle = Vector3.SignedAngle(Vector3.ProjectOnPlane(playerTransform.up, Vector3.up), Vector3.ProjectOnPlane(Vector3.up, playerTransform.forward), playerTransform.forward);
        playerLocalAngularVelocity = playerTransform.InverseTransformDirection(playerRb != null ? playerRb.angularVelocity : Vector3.zero);
        playerVelocityToEnemyAngle = Vector3.Angle(SafeNormalize(playerVelocity, playerTransform.forward), toEnemyDirection);
        enemyVelocityToPlayerAngle = Vector3.Angle(SafeNormalize(enemyVelocity, enemyTransform.forward), toPlayerDirection);
        UpdateVelocityGeometry(toEnemy);
    }

    void UpdateVelocityGeometry(Vector3 relativePosition)
    {
        relativeVelocity = enemyVelocity - playerVelocity;
        float playerMagnitude = playerVelocity.magnitude;
        float enemyMagnitude = enemyVelocity.magnitude;
        bool degenerate = playerMagnitude <= 0.001f || enemyMagnitude <= 0.001f;
        Vector3 playerDirection = degenerate ? Vector3.zero : playerVelocity / playerMagnitude;
        Vector3 enemyDirection = degenerate ? Vector3.zero : enemyVelocity / enemyMagnitude;
        Vector3 cross = Vector3.Cross(playerDirection, enemyDirection);

        velocityDirectionAngle = degenerate ? 0f : Vector3.Angle(playerDirection, enemyDirection);
        if (degenerate)
        {
            velocityLineClosestDistance = relativePosition.magnitude;
            normalizedVelocityTripleProduct = 0f;
            velocityLineRelation = VelocityLineRelation.Degenerate;
        }
        else if (cross.sqrMagnitude <= 0.0001f)
        {
            velocityLineClosestDistance = Vector3.Cross(relativePosition, playerDirection).magnitude;
            normalizedVelocityTripleProduct = 0f;
            velocityLineRelation = velocityLineClosestDistance <= Mathf.Max(1f, distance * 0.01f)
                ? VelocityLineRelation.NearCollinear : VelocityLineRelation.NearParallel;
        }
        else
        {
            velocityLineClosestDistance = Mathf.Abs(Vector3.Dot(relativePosition, cross)) / cross.magnitude;
            normalizedVelocityTripleProduct = Vector3.Dot(relativePosition, cross)
                / Mathf.Max(relativePosition.magnitude, 0.001f);
            velocityLineRelation = velocityLineClosestDistance <= Mathf.Max(1f, distance * 0.01f)
                ? VelocityLineRelation.NearIntersecting : VelocityLineRelation.Skew;
        }

        normalizedVelocityLineSeparation = velocityLineClosestDistance / Mathf.Max(distance, 0.001f);
        if (relativeVelocity.sqrMagnitude <= 0.0001f)
        {
            timeToClosestApproach = 0f;
            predictedClosestApproachDistance = relativePosition.magnitude;
        }
        else
        {
            timeToClosestApproach = -Vector3.Dot(relativePosition, relativeVelocity) / relativeVelocity.sqrMagnitude;
            predictedClosestApproachDistance = (relativePosition + relativeVelocity * timeToClosestApproach).magnitude;
        }

        if (degenerate)
            velocityEncounterType = VelocityEncounterType.Unknown;
        else if (timeToClosestApproach < 0f)
            velocityEncounterType = VelocityEncounterType.PassedClosestApproach;
        else if (closureRate < -0.01f)
            velocityEncounterType = VelocityEncounterType.Diverging;
        else if (velocityDirectionAngle < 30f)
            velocityEncounterType = VelocityEncounterType.SameDirection;
        else if (velocityDirectionAngle > 150f)
            velocityEncounterType = VelocityEncounterType.HeadOn;
        else
            velocityEncounterType = VelocityEncounterType.Crossing;
    }

    void UpdatePursuitAnalysis()
    {
        Vector3 toEnemy = enemyTransform.position - playerTransform.position;
        purePursuitDirection = SafeNormalize(toEnemy, playerTransform.forward);

        predictedEnemyPosition = enemyTransform.position + GetVelocity(enemyRb) * Mathf.Max(0f, leadPredictionTime);
        leadPursuitDirection = SafeNormalize(predictedEnemyPosition - playerTransform.position, purePursuitDirection);

        purePursuitAngle = Vector3.Angle(playerTransform.forward, purePursuitDirection);
        leadPursuitAngle = Vector3.Angle(playerTransform.forward, leadPursuitDirection);
        leadPredictionPreferred = closing && leadPursuitAngle < purePursuitAngle;
        if (leadPredictionPreferred)
            lastLeadPreferredDistance = distance;
    }

    void UpdateMissileAnalysis()
    {
        enemyIncomingMissileCount = EnemyMissileThreatSensor.SenseIncomingMissiles(
            enemyTransform,
            enemyRb,
            missileThreats,
            missileDetectRange,
            missileApproachAngle,
            missileCriticalTime);

        if (enemyIncomingMissileCount <= 0)
        {
            enemyMissileDistance = 0f;
            enemyMissileTimeToImpact = 0f;
            enemyMissileApproachDirection = Vector3.zero;
            enemyMissileEvadeDirection = Vector3.zero;
            return;
        }

        EnemyMissileThreatSensor.ThreatInfo highestThreat = missileThreats[0];
        for (int i = 1; i < missileThreats.Count; i++)
        {
            if (missileThreats[i].score > highestThreat.score)
                highestThreat = missileThreats[i];
        }

        enemyMissileDistance = highestThreat.dist;
        enemyMissileTimeToImpact = highestThreat.timeToImpact;
        enemyMissileApproachDirection = highestThreat.approachDirection;
        enemyMissileEvadeDirection = highestThreat.evadeDirection;
    }

    void UpdatePitchAxisAnalysis()
    {
        Vector3 toEnemyDirection = SafeNormalize(enemyTransform.position - playerTransform.position, playerTransform.forward);
        Vector3 enemyVelocityDirection = SafeNormalize(GetVelocity(enemyRb), enemyTransform.forward);

        enemyInView = playerNoseToEnemyAngle <= inViewAngle;
        pitchAxisToEnemyAngle = GetPitchAxisErrorAngle(playerTransform, toEnemyDirection);
        pitchAxisToEnemyVelocityAngle = GetPitchAxisErrorAngle(playerTransform, enemyVelocityDirection);

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        pitchAxisErrorChangePerSecond = (previousPitchAxisError - Mathf.Abs(pitchAxisToEnemyAngle)) / deltaTime;
        estimatedPitchAxisAlignTime = pitchAxisErrorChangePerSecond > 0.001f
            ? Mathf.Abs(pitchAxisToEnemyAngle) / pitchAxisErrorChangePerSecond
            : -1f;
        previousPitchAxisError = Mathf.Abs(pitchAxisToEnemyAngle);

        if (enemyInView)
        {
            inViewVelocityAxisTotalSamples++;
            if (Mathf.Abs(pitchAxisToEnemyVelocityAngle) <= pitchAxisAlignedAngle)
                inViewVelocityAxisAlignedSamples++;

            inViewVelocityAxisAlignFrequency =
                (float)inViewVelocityAxisAlignedSamples / Mathf.Max(1, inViewVelocityAxisTotalSamples);
        }

        UpdateOutOfViewTrackingMethod();
    }

    void UpdateOutOfViewTrackingMethod()
    {
        if (enemyInView)
        {
            outOfViewTrackingSeconds = 0f;
            outOfViewTrackingErrorAngle = 0f;
            return;
        }

        Vector3 enemyVelocity = GetVelocity(enemyRb);
        float searchRange = Mathf.Max(0f, trackingSecondsSearchRange);
        float searchStep = Mathf.Max(0.01f, trackingSecondsSearchStep);
        float bestSeconds = 0f;
        float bestError = float.MaxValue;

        for (float seconds = -searchRange; seconds <= searchRange + 0.0001f; seconds += searchStep)
        {
            Vector3 trackedPosition = enemyTransform.position + enemyVelocity * seconds;
            Vector3 direction = SafeNormalize(trackedPosition - playerTransform.position, playerTransform.forward);
            float error = Mathf.Abs(GetPitchAxisErrorAngle(playerTransform, direction));
            if (error < bestError)
            {
                bestError = error;
                bestSeconds = seconds;
            }
        }

        outOfViewTrackingSeconds = bestSeconds;
        outOfViewTrackingErrorAngle = bestError;
    }

    void UpdateLowPitchInputSnapshot()
    {
        lowPitchInput = Mathf.Abs(pitchInput) <= lowPitchInputThreshold;
        if (!lowPitchInput)
            return;

        lowPitchPitchAxisToEnemyAngle = pitchAxisToEnemyAngle;
        lowPitchRollPitchRatio = rollPitchInputRatio;
        lowPitchAxisPrecision = 1f - Mathf.Clamp01(Mathf.Abs(pitchAxisToEnemyAngle) / 90f);
    }

    void UpdateScissorsAnalysis()
    {
        float rollAbs = Mathf.Abs(rollInput);
        bool closeEnough = distance <= closeRange;
        bool strongRollStarted = previousRollAbs < axisOffsetRollThreshold && rollAbs >= axisOffsetRollThreshold;

        scissorsAxisOffsetCandidate = closeEnough
            && rollAbs >= axisOffsetRollThreshold
            && Mathf.Abs(pitchAxisToEnemyAngle) > pitchAxisAlignedAngle;

        if (scissorsAxisOffsetCandidate)
            rollingScissorsCandidateTime += Time.deltaTime;

        if (closeEnough && strongRollStarted)
        {
            axisOffsetRollEvents++;
            lastAxisOffsetAngle = pitchAxisToEnemyAngle;
            axisOffsetAngleSum += Mathf.Abs(lastAxisOffsetAngle);
            averageAxisOffsetAngle = axisOffsetAngleSum / Mathf.Max(1, axisOffsetRollEvents);
        }

        float elapsed = Mathf.Max(0.001f, Time.time - analysisStartTime);
        axisOffsetRollFrequency = axisOffsetRollEvents / elapsed;
        previousRollAbs = rollAbs;
    }

    static float GetPitchAxisErrorAngle(Transform reference, Vector3 worldDirection)
    {
        Vector3 localDirection = reference.InverseTransformDirection(SafeNormalize(worldDirection, reference.forward));
        float signedPlaneOffset = Mathf.Clamp(localDirection.x, -1f, 1f);
        return Mathf.Asin(signedPlaneOffset) * Mathf.Rad2Deg;
    }

    static Vector3 GetVelocity(Rigidbody body)
    {
        return body != null ? body.linearVelocity : Vector3.zero;
    }

    static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        if (value.sqrMagnitude > 0.0001f && IsFinite(value))
            return value.normalized;

        if (fallback.sqrMagnitude > 0.0001f && IsFinite(fallback))
            return fallback.normalized;

        return Vector3.forward;
    }

    static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)
            && !float.IsNaN(value.y)
            && !float.IsNaN(value.z)
            && !float.IsInfinity(value.x)
            && !float.IsInfinity(value.y)
            && !float.IsInfinity(value.z);
    }
}
