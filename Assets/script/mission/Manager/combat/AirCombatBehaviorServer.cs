using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class AirCombatBehaviorServer : MonoBehaviour
{
    [System.Serializable]
    public struct LowPitchAxisSample
    {
        public float pitchAxisToEnemyAngle;
        public float rollPitchInputRatio;
        public float axisPrecision;
    }

    [System.Serializable]
    public struct DisengageSample
    {
        public float confirmedTime;
        public float angleHoldTime;
        public float playerNoseToEnemyAngle;
        public float distance;
    }

    [System.Serializable]
    public struct GraphBin
    {
        public float minAngle;
        public float maxAngle;
        public float centerAngle;
        public int sampleCount;
        public int lowPitchSampleCount;
        public int disengageSampleCount;
        public float averageRollPitchInputRatio;
        public float averageAxisPrecision;
        public float averageDisengageDistance;
    }

    [System.Serializable]
    public struct TurnPlaneGainBin
    {
        public float minAzimuthAbs;
        public float maxAzimuthAbs;
        public int sampleCount;
        public float averageRollAbs;
        public float averageYawAbs;
        public float averageAzimuthReductionRate;
    }

    [System.Serializable]
    public struct TurnPlaneIqrPoint
    {
        public int startEpisodeIndex;
        public int endEpisodeIndex;
        public float minimumRelativeAngleMedian;
        public float minimumRelativeAngleQ1;
        public float minimumRelativeAngleQ3;
        public float minimumRelativeAngleIqr;
        public float finalAzimuthAbsMedian;
        public float finalAzimuthAbsQ1;
        public float finalAzimuthAbsQ3;
        public float finalAzimuthAbsIqr;
        public float correctionEfficiencyMedian;
        public float correctionEfficiencyQ1;
        public float correctionEfficiencyQ3;
        public float correctionEfficiencyIqr;
    }

    [System.Serializable]
    public struct TurnPlaneCorrectionPoint
    {
        public float timeFromStart;
        public float pitchInput;
        public float rollInput;
        public float yawInput;
        public float signedAzimuthError;
        public float signedElevationError;
        public float totalRelativeAngle;
        public float bankAngle;
        public Vector3 localAngularVelocity;
        public float distance;
        public float closureRate;
    }

    [System.Serializable]
    public struct RollControlEvent
    {
        public float timeFromEpisodeStart;
        public float signedAzimuthError;
        public float totalRelativeAngle;
        public float rollInputBefore;
        public float rollInputAfter;
        public float yawInputBefore;
        public float yawInputAfter;
        public bool isStop;
        public bool isReverse;
        public bool isYawReverse;
        public bool isReinput;
    }

    [System.Serializable]
    public struct TurnPlaneCorrectionEpisode
    {
        public float startTime;
        public float endTime;
        public float duration;
        public float initialAzimuth;
        public int initialAzimuthSign;
        public float minimumAzimuthAbs;
        public float finalAzimuth;
        public float minimumRelativeAngle;
        public float timeToMinimumRelativeAngle;
        public int rollStopCount;
        public int rollReverseCount;
        public int yawReverseCount;
        public int azimuthZeroCrossCount;
        public int overshootCount;
        public float rollMaxAbs;
        public float rollMeanAbs;
        public float rollRms;
        public float rollSignedIntegral;
        public float rollAbsoluteIntegral;
        public float rollQ1;
        public float rollMedian;
        public float rollQ3;
        public float rollIqr;
        public float yawMaxAbs;
        public float yawMeanAbs;
        public float yawRms;
        public float yawSignedIntegral;
        public float yawAbsoluteIntegral;
        public float yawQ1;
        public float yawMedian;
        public float yawQ3;
        public float yawIqr;
        public float yawRollEffortRatio;
        public float yawRollRmsRatio;
        public float averageReverseImpulseRatio;
        public float averageReversePeakRatio;
        public float averageReverseDurationRatio;
        public float reactionDelay;
        public float releaseDelay;
        public float postMinimumInputDuration;
        public float averageReversePeriod;
        public float minReversePeriod;
        public float averageHysteresisWidth;
        public float correctionEfficiency;
        public float rollBestLagSeconds;
        public float rollBestLagCorrelation;
        public float yawBestLagSeconds;
        public float yawBestLagCorrelation;
        public List<TurnPlaneCorrectionPoint> points;
        public List<RollControlEvent> rollEvents;
    }

    [System.Serializable]
    public struct NormalizedTurnPlanePoint
    {
        public float normalizedTime;
        public float pitchInput;
        public float rollInput;
        public float yawInput;
        public float signedAzimuthError;
        public float totalRelativeAngle;
        public float bankAngle;
    }

    [Header("Source")]
    public AirCombatBehaviorAnalyzer analyzer;

    [Header("Recording")]
    [SerializeField] bool record = true;
    [SerializeField] float minSampleInterval = 0.08f;
    [SerializeField] bool requireEnemyInView;

    [Header("Turn Plane Correction")]
    [SerializeField] float correctionStartAzimuthThreshold = 5f;
    [SerializeField] float correctionEndAzimuthThreshold = 2f;
    [SerializeField] float correctionInputThreshold = 0.15f;
    [SerializeField] float correctionPitchThreshold = 0.2f;
    [SerializeField] bool requirePitchDuringCorrection = true;
    [SerializeField] float correctionSampleInterval = 0.05f;
    [SerializeField] float correctionMaxDuration = 5f;
    [SerializeField] int maxCorrectionEpisodes = 128;
    [SerializeField] int maxCorrectionPointsPerEpisode = 512;
    [SerializeField] float rollControlStopThreshold = 0.1f;
    [SerializeField] float rollReverseThreshold = 0.2f;
    [SerializeField] float overshootInputHoldTime = 0.15f;
    [SerializeField] float delayedCorrelationMaxLag = 1f;
    [SerializeField] int turnPlaneGainBinCount = 9;
    [SerializeField] int iqrEpisodeWindowSize = 8;

    [Header("Disengage Recording")]
    [SerializeField] float disengageAngleThreshold = 90f;
    [SerializeField] float disengageResetAngle = 70f;
    [SerializeField] float disengageConfirmHoldTime = 0.2f;
    [SerializeField] float disengageMinDistance = 800f;

    [Header("Graph Bins")]
    [SerializeField] float minAngle = -90f;
    [SerializeField] float maxAngle = 90f;
    [SerializeField] int binCount = 18;

    [Header("Samples")]
    [SerializeField] List<TurnPlaneCorrectionEpisode> turnPlaneCorrectionEpisodes = new();
    [SerializeField] List<NormalizedTurnPlanePoint> normalizedTurnPlaneAverage = new();
    [SerializeField] List<TurnPlaneIqrPoint> turnPlaneIqrHistory = new();
    [SerializeField] GraphBin[] graphBins;
    [SerializeField] int lowPitchSampleCount;
    [SerializeField] int disengageSampleCount;
    [SerializeField] float disengageFrequencyPerMinute;
    [SerializeField] float averageDisengageDistance;
    [SerializeField] float averageDisengageAngle;
    [SerializeField] float[] lowPitchRatioSums;
    [SerializeField] float[] lowPitchPrecisionSums;
    [SerializeField] float[] disengageDistanceSums;
    [SerializeField] int[] noseAngleHistogramBins;
    [SerializeField] float[] relativeDirectionSectorSeconds;
    [SerializeField] float[] relativeDirectionSectorAngleSums;
    [SerializeField] int[] relativeDirectionSectorCounts;
    [SerializeField] int[] relativeDirectionSectorAngleBins;
    [SerializeField] int leftCorrectionEpisodeCount;
    [SerializeField] int rightCorrectionEpisodeCount;
    [SerializeField] float leftCorrectionEfficiencyAverage;
    [SerializeField] float rightCorrectionEfficiencyAverage;
    [SerializeField] float leftHysteresisWidthAverage;
    [SerializeField] float rightHysteresisWidthAverage;
    [SerializeField] float correctionEfficiencyAsymmetry;
    [SerializeField] float hysteresisWidthAsymmetry;
    [SerializeField] TurnPlaneGainBin[] turnPlaneGainBins;

    float nextSampleTime;
    bool disengagePending;
    bool disengageConfirmed;
    float disengageAngleTimer;
    float recordingStartTime;
    DisengageSample pendingDisengageSample;
    float disengageDistanceSum;
    float disengageAngleSum;
    bool turnPlaneCorrectionActive;
    TurnPlaneCorrectionEpisode currentTurnPlaneCorrection;
    float correctionNextSampleTime;
    float correctionLastSampleTime;
    float correctionPreviousRollInput;
    float correctionPreviousYawInput;
    float correctionPreviousAbsRollInput;
    float correctionThresholdCrossTime = -1f;
    bool correctionAwaitingRollReinput;

    public IReadOnlyList<TurnPlaneCorrectionEpisode> TurnPlaneCorrectionEpisodes => turnPlaneCorrectionEpisodes;
    public IReadOnlyList<NormalizedTurnPlanePoint> NormalizedTurnPlaneAverage => normalizedTurnPlaneAverage;
    public IReadOnlyList<TurnPlaneIqrPoint> TurnPlaneIqrHistory => turnPlaneIqrHistory;
    public IReadOnlyList<TurnPlaneGainBin> TurnPlaneGainBins => turnPlaneGainBins;
    public IReadOnlyList<GraphBin> GraphBins => graphBins;
    public IReadOnlyList<int> NoseAngleHistogramBins => noseAngleHistogramBins;
    public IReadOnlyList<float> RelativeDirectionSectorSeconds => relativeDirectionSectorSeconds;
    public IReadOnlyList<float> RelativeDirectionSectorAngleSums => relativeDirectionSectorAngleSums;
    public IReadOnlyList<int> RelativeDirectionSectorCounts => relativeDirectionSectorCounts;
    public IReadOnlyList<int> RelativeDirectionSectorAngleBins => relativeDirectionSectorAngleBins;
    public int DisengageSampleCount => disengageSampleCount;
    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public float DisengageFrequencyPerMinute => disengageFrequencyPerMinute;
    public float AverageDisengageDistance => averageDisengageDistance;
    public float AverageDisengageAngle => averageDisengageAngle;
    public int LeftCorrectionEpisodeCount => leftCorrectionEpisodeCount;
    public int RightCorrectionEpisodeCount => rightCorrectionEpisodeCount;
    public float LeftCorrectionEfficiencyAverage => leftCorrectionEfficiencyAverage;
    public float RightCorrectionEfficiencyAverage => rightCorrectionEfficiencyAverage;
    public float LeftHysteresisWidthAverage => leftHysteresisWidthAverage;
    public float RightHysteresisWidthAverage => rightHysteresisWidthAverage;
    public float CorrectionEfficiencyAsymmetry => correctionEfficiencyAsymmetry;
    public float HysteresisWidthAsymmetry => hysteresisWidthAsymmetry;

    void Reset()
    {
        analyzer = GetComponent<AirCombatBehaviorAnalyzer>();
        RebuildGraphBins();
    }

    void Awake()
    {
        if (analyzer == null)
            analyzer = GetComponent<AirCombatBehaviorAnalyzer>();

        recordingStartTime = Time.time;
        RebuildGraphBins();
    }

    void LateUpdate()
    {
        if (!record || analyzer == null || !analyzer.HasValidTargets)
            return;

        RecordLowPitchSample();
        RecordTurnPlaneCorrection();
        RecordDisengageSample();
        RecordNoseAngleSample();
    }

    public void ClearSamples()
    {
        turnPlaneCorrectionEpisodes.Clear();
        normalizedTurnPlaneAverage.Clear();
        turnPlaneIqrHistory.Clear();
        lowPitchSampleCount = 0;
        disengageSampleCount = 0;
        disengagePending = false;
        disengageConfirmed = false;
        disengageAngleTimer = 0f;
        turnPlaneCorrectionActive = false;
        correctionThresholdCrossTime = -1f;
        correctionPreviousRollInput = 0f;
        correctionPreviousYawInput = 0f;
        correctionPreviousAbsRollInput = 0f;
        correctionAwaitingRollReinput = false;
        leftCorrectionEpisodeCount = 0;
        rightCorrectionEpisodeCount = 0;
        leftCorrectionEfficiencyAverage = 0f;
        rightCorrectionEfficiencyAverage = 0f;
        leftHysteresisWidthAverage = 0f;
        rightHysteresisWidthAverage = 0f;
        correctionEfficiencyAsymmetry = 0f;
        hysteresisWidthAsymmetry = 0f;
        recordingStartTime = Time.time;
        RebuildGraphBins();
    }

    public void RebuildGraphBins()
    {
        int safeBinCount = Mathf.Max(1, binCount);
        graphBins = new GraphBin[safeBinCount];

        float range = Mathf.Max(0.001f, maxAngle - minAngle);
        float binWidth = range / safeBinCount;
        lowPitchRatioSums = new float[safeBinCount];
        lowPitchPrecisionSums = new float[safeBinCount];
        disengageDistanceSums = new float[safeBinCount];
        noseAngleHistogramBins = new int[18];
        relativeDirectionSectorSeconds = new float[8];
        relativeDirectionSectorAngleSums = new float[8];
        relativeDirectionSectorCounts = new int[8];
        relativeDirectionSectorAngleBins = new int[8 * 18];
        disengageDistanceSum = 0f;
        disengageAngleSum = 0f;

        for (int i = 0; i < safeBinCount; i++)
        {
            float binMin = minAngle + binWidth * i;
            float binMax = binMin + binWidth;
            graphBins[i] = new GraphBin
            {
                minAngle = binMin,
                maxAngle = binMax,
                centerAngle = (binMin + binMax) * 0.5f
            };
        }

        UpdateDisengageSummary();
    }

    void AddSample(LowPitchAxisSample sample)
    {
        EnsureGraphBins();
        lowPitchSampleCount++;
        AccumulateLowPitchSample(sample, 1);
    }

    void AddDisengageSample(DisengageSample sample)
    {
        EnsureGraphBins();
        disengageSampleCount++;
        AccumulateDisengageSample(sample, 1);
        UpdateDisengageSummary();
    }

    void RecordLowPitchSample()
    {
        if (Time.time < nextSampleTime)
            return;

        if (!analyzer.LowPitchInput)
            return;

        if (requireEnemyInView && !analyzer.EnemyInView)
            return;

        nextSampleTime = Time.time + minSampleInterval;
        AddSample(new LowPitchAxisSample
        {
            pitchAxisToEnemyAngle = analyzer.LowPitchPitchAxisToEnemyAngle,
            rollPitchInputRatio = analyzer.LowPitchRollPitchRatio,
            axisPrecision = analyzer.LowPitchAxisPrecision
        });
    }

    void RecordTurnPlaneCorrection()
    {
        float absAzimuth = Mathf.Abs(analyzer.SignedAzimuthError);
        bool pitchOk = !requirePitchDuringCorrection || Mathf.Abs(analyzer.PitchInput) >= correctionPitchThreshold;
        bool correctionInputActive = Mathf.Abs(analyzer.RollInput) >= correctionInputThreshold
            || Mathf.Abs(analyzer.YawInput) >= correctionInputThreshold;

        if (!turnPlaneCorrectionActive)
        {
            if (absAzimuth >= correctionStartAzimuthThreshold)
            {
                if (correctionThresholdCrossTime < 0f)
                    correctionThresholdCrossTime = Time.time;
            }
            else
            {
                correctionThresholdCrossTime = -1f;
            }

            if (absAzimuth >= correctionStartAzimuthThreshold && correctionInputActive && pitchOk)
                BeginTurnPlaneCorrection();

            correctionPreviousRollInput = analyzer.RollInput;
            correctionPreviousYawInput = analyzer.YawInput;
            correctionPreviousAbsRollInput = Mathf.Abs(analyzer.RollInput);
            return;
        }

        RecordTurnPlaneCorrectionPointIfNeeded();
        DetectTurnPlaneCorrectionEvents();
        UpdateTurnPlaneCorrectionMinimums();

        bool inputStopped = Mathf.Abs(analyzer.RollInput) < correctionInputThreshold
            && Mathf.Abs(analyzer.YawInput) < correctionInputThreshold;
        bool azimuthSettled = absAzimuth <= correctionEndAzimuthThreshold;
        bool timedOut = Time.time - currentTurnPlaneCorrection.startTime >= correctionMaxDuration;

        if (azimuthSettled || inputStopped || timedOut)
            EndTurnPlaneCorrection();

        correctionPreviousRollInput = analyzer.RollInput;
        correctionPreviousYawInput = analyzer.YawInput;
        correctionPreviousAbsRollInput = Mathf.Abs(analyzer.RollInput);
    }

    void BeginTurnPlaneCorrection()
    {
        turnPlaneCorrectionActive = true;
        correctionNextSampleTime = Time.time;
        correctionLastSampleTime = Time.time;
        currentTurnPlaneCorrection = new TurnPlaneCorrectionEpisode
        {
            startTime = Time.time,
            initialAzimuth = analyzer.SignedAzimuthError,
            initialAzimuthSign = GetInputSign(analyzer.SignedAzimuthError, 0.001f),
            minimumAzimuthAbs = Mathf.Abs(analyzer.SignedAzimuthError),
            minimumRelativeAngle = analyzer.PlayerNoseToEnemyAngle,
            timeToMinimumRelativeAngle = 0f,
            reactionDelay = correctionThresholdCrossTime >= 0f ? Time.time - correctionThresholdCrossTime : 0f,
            points = new List<TurnPlaneCorrectionPoint>(),
            rollEvents = new List<RollControlEvent>()
        };

        correctionAwaitingRollReinput = false;
        RecordTurnPlaneCorrectionPoint();
    }

    void RecordTurnPlaneCorrectionPointIfNeeded()
    {
        while (Time.time + 0.0001f >= correctionNextSampleTime)
        {
            RecordTurnPlaneCorrectionPoint();
            correctionNextSampleTime += Mathf.Max(0.001f, correctionSampleInterval);
            if (currentTurnPlaneCorrection.points.Count >= maxCorrectionPointsPerEpisode)
                break;
        }
    }

    void RecordTurnPlaneCorrectionPoint()
    {
        if (currentTurnPlaneCorrection.points == null)
            currentTurnPlaneCorrection.points = new List<TurnPlaneCorrectionPoint>();

        if (currentTurnPlaneCorrection.points.Count >= maxCorrectionPointsPerEpisode)
            return;

        currentTurnPlaneCorrection.points.Add(new TurnPlaneCorrectionPoint
        {
            timeFromStart = Time.time - currentTurnPlaneCorrection.startTime,
            pitchInput = analyzer.PitchInput,
            rollInput = analyzer.RollInput,
            yawInput = analyzer.YawInput,
            signedAzimuthError = analyzer.SignedAzimuthError,
            signedElevationError = analyzer.SignedElevationError,
            totalRelativeAngle = analyzer.PlayerNoseToEnemyAngle,
            bankAngle = analyzer.BankAngle,
            localAngularVelocity = analyzer.PlayerLocalAngularVelocity,
            distance = analyzer.Distance,
            closureRate = analyzer.ClosureRate
        });
    }

    void DetectTurnPlaneCorrectionEvents()
    {
        if (currentTurnPlaneCorrection.rollEvents == null)
            currentTurnPlaneCorrection.rollEvents = new List<RollControlEvent>();

        float absRoll = Mathf.Abs(analyzer.RollInput);
        if (correctionPreviousAbsRollInput > rollControlStopThreshold && absRoll <= rollControlStopThreshold)
        {
            currentTurnPlaneCorrection.rollStopCount++;
            correctionAwaitingRollReinput = true;
            AddRollControlEvent(true, false, false, false);
        }

        if (correctionAwaitingRollReinput
            && correctionPreviousAbsRollInput <= rollControlStopThreshold
            && absRoll >= correctionInputThreshold)
        {
            correctionAwaitingRollReinput = false;
            AddRollControlEvent(false, false, false, true);
        }

        int previousSign = GetInputSign(correctionPreviousRollInput, rollReverseThreshold);
        int currentSign = GetInputSign(analyzer.RollInput, rollReverseThreshold);
        if (previousSign != 0 && currentSign != 0 && previousSign != currentSign)
        {
            currentTurnPlaneCorrection.rollReverseCount++;
            AddRollControlEvent(false, true, false, false);
        }

        int previousYawSign = GetInputSign(correctionPreviousYawInput, rollReverseThreshold);
        int currentYawSign = GetInputSign(analyzer.YawInput, rollReverseThreshold);
        if (previousYawSign != 0 && currentYawSign != 0 && previousYawSign != currentYawSign)
        {
            currentTurnPlaneCorrection.yawReverseCount++;
            AddRollControlEvent(false, false, true, false);
        }
    }

    void AddRollControlEvent(bool isStop, bool isReverse, bool isYawReverse, bool isReinput)
    {
        currentTurnPlaneCorrection.rollEvents.Add(new RollControlEvent
        {
            timeFromEpisodeStart = Time.time - currentTurnPlaneCorrection.startTime,
            signedAzimuthError = analyzer.SignedAzimuthError,
            totalRelativeAngle = analyzer.PlayerNoseToEnemyAngle,
            rollInputBefore = correctionPreviousRollInput,
            rollInputAfter = analyzer.RollInput,
            yawInputBefore = correctionPreviousYawInput,
            yawInputAfter = analyzer.YawInput,
            isStop = isStop,
            isReverse = isReverse,
            isYawReverse = isYawReverse,
            isReinput = isReinput
        });
    }

    void UpdateTurnPlaneCorrectionMinimums()
    {
        float absAzimuth = Mathf.Abs(analyzer.SignedAzimuthError);
        if (absAzimuth < currentTurnPlaneCorrection.minimumAzimuthAbs)
            currentTurnPlaneCorrection.minimumAzimuthAbs = absAzimuth;

        if (analyzer.PlayerNoseToEnemyAngle < currentTurnPlaneCorrection.minimumRelativeAngle)
        {
            currentTurnPlaneCorrection.minimumRelativeAngle = analyzer.PlayerNoseToEnemyAngle;
            currentTurnPlaneCorrection.timeToMinimumRelativeAngle = Time.time - currentTurnPlaneCorrection.startTime;
        }
    }

    void EndTurnPlaneCorrection()
    {
        RecordTurnPlaneCorrectionPoint();
        currentTurnPlaneCorrection.endTime = Time.time;
        currentTurnPlaneCorrection.duration = Mathf.Max(0f, currentTurnPlaneCorrection.endTime - currentTurnPlaneCorrection.startTime);
        currentTurnPlaneCorrection.finalAzimuth = analyzer.SignedAzimuthError;
        currentTurnPlaneCorrection.releaseDelay = Mathf.Max(0f, currentTurnPlaneCorrection.duration - currentTurnPlaneCorrection.timeToMinimumRelativeAngle);
        CalculateTurnPlaneCorrectionStats(ref currentTurnPlaneCorrection);

        turnPlaneCorrectionEpisodes.Add(currentTurnPlaneCorrection);
        while (turnPlaneCorrectionEpisodes.Count > maxCorrectionEpisodes)
            turnPlaneCorrectionEpisodes.RemoveAt(0);

        turnPlaneCorrectionActive = false;
        correctionThresholdCrossTime = -1f;
        correctionAwaitingRollReinput = false;
    }

    void CalculateTurnPlaneCorrectionStats(ref TurnPlaneCorrectionEpisode episode)
    {
        if (episode.points == null || episode.points.Count == 0)
            return;

        float rollAbsSum = 0f;
        float yawAbsSum = 0f;
        float rollSquareSum = 0f;
        float yawSquareSum = 0f;
        float rollSignedIntegral = 0f;
        float yawSignedIntegral = 0f;
        float rollAbsIntegral = 0f;
        float yawAbsIntegral = 0f;
        float[] rollAbsValues = new float[episode.points.Count];
        float[] yawAbsValues = new float[episode.points.Count];

        for (int i = 0; i < episode.points.Count; i++)
        {
            TurnPlaneCorrectionPoint point = episode.points[i];
            float dt = i > 0
                ? Mathf.Max(0.001f, point.timeFromStart - episode.points[i - 1].timeFromStart)
                : Mathf.Max(0.001f, correctionSampleInterval);
            float rollAbs = Mathf.Abs(point.rollInput);
            float yawAbs = Mathf.Abs(point.yawInput);
            episode.rollMaxAbs = Mathf.Max(episode.rollMaxAbs, rollAbs);
            episode.yawMaxAbs = Mathf.Max(episode.yawMaxAbs, yawAbs);
            rollAbsValues[i] = rollAbs;
            yawAbsValues[i] = yawAbs;
            rollAbsSum += rollAbs;
            yawAbsSum += yawAbs;
            rollSquareSum += point.rollInput * point.rollInput;
            yawSquareSum += point.yawInput * point.yawInput;
            rollSignedIntegral += point.rollInput * dt;
            yawSignedIntegral += point.yawInput * dt;
            rollAbsIntegral += rollAbs * dt;
            yawAbsIntegral += yawAbs * dt;
        }

        int count = Mathf.Max(1, episode.points.Count);
        episode.rollMeanAbs = rollAbsSum / count;
        episode.yawMeanAbs = yawAbsSum / count;
        episode.rollRms = Mathf.Sqrt(rollSquareSum / count);
        episode.yawRms = Mathf.Sqrt(yawSquareSum / count);
        episode.rollSignedIntegral = rollSignedIntegral;
        episode.yawSignedIntegral = yawSignedIntegral;
        episode.rollAbsoluteIntegral = rollAbsIntegral;
        episode.yawAbsoluteIntegral = yawAbsIntegral;
        CalculateQuartiles(rollAbsValues, out episode.rollQ1, out episode.rollMedian, out episode.rollQ3, out episode.rollIqr);
        CalculateQuartiles(yawAbsValues, out episode.yawQ1, out episode.yawMedian, out episode.yawQ3, out episode.yawIqr);
        episode.yawRollEffortRatio = yawAbsIntegral / Mathf.Max(0.0001f, rollAbsIntegral);
        episode.yawRollRmsRatio = episode.yawRms / Mathf.Max(0.0001f, episode.rollRms);
        episode.correctionEfficiency = (Mathf.Abs(episode.initialAzimuth) - episode.minimumAzimuthAbs)
            / Mathf.Max(0.0001f, rollAbsIntegral + yawAbsIntegral);
        episode.postMinimumInputDuration = CalculatePostMinimumInputDuration(episode);
        CalculateAzimuthCrossingStats(ref episode);
        CalculateHysteresisStats(ref episode);
        CalculateDelayedInputCorrelations(ref episode);
        CalculateReverseRatios(ref episode);
    }

    void CalculateReverseRatios(ref TurnPlaneCorrectionEpisode episode)
    {
        if (episode.rollEvents == null || episode.points == null)
            return;

        float impulseRatioSum = 0f;
        float peakRatioSum = 0f;
        float durationRatioSum = 0f;
        int ratioCount = 0;

        for (int i = 0; i < episode.rollEvents.Count; i++)
        {
            if (!episode.rollEvents[i].isReverse)
                continue;

            float eventTime = episode.rollEvents[i].timeFromEpisodeStart;
            CalculateRollWindow(episode.points, eventTime, -1, out float beforeDuration, out float beforePeak, out float beforeImpulse);
            CalculateRollWindow(episode.points, eventTime, 1, out float afterDuration, out float afterPeak, out float afterImpulse);
            impulseRatioSum += afterImpulse / Mathf.Max(0.0001f, beforeImpulse);
            peakRatioSum += afterPeak / Mathf.Max(0.0001f, beforePeak);
            durationRatioSum += afterDuration / Mathf.Max(0.0001f, beforeDuration);
            ratioCount++;
        }

        if (ratioCount > 0)
        {
            episode.averageReverseImpulseRatio = impulseRatioSum / ratioCount;
            episode.averageReversePeakRatio = peakRatioSum / ratioCount;
            episode.averageReverseDurationRatio = durationRatioSum / ratioCount;
        }
    }

    float CalculatePostMinimumInputDuration(TurnPlaneCorrectionEpisode episode)
    {
        if (episode.points == null || episode.points.Count < 2)
            return 0f;

        float duration = 0f;
        for (int i = 1; i < episode.points.Count; i++)
        {
            TurnPlaneCorrectionPoint point = episode.points[i - 1];
            if (point.timeFromStart < episode.timeToMinimumRelativeAngle)
                continue;

            if (Mathf.Abs(point.rollInput) < correctionInputThreshold
                && Mathf.Abs(point.yawInput) < correctionInputThreshold)
                continue;

            duration += Mathf.Max(0f, episode.points[i].timeFromStart - point.timeFromStart);
        }

        return duration;
    }

    void CalculateAzimuthCrossingStats(ref TurnPlaneCorrectionEpisode episode)
    {
        if (episode.points == null || episode.points.Count < 2)
            return;

        int zeroCrossCount = 0;
        int overshootCount = 0;
        int previousSign = GetInputSign(episode.points[0].signedAzimuthError, 0.001f);

        for (int i = 1; i < episode.points.Count; i++)
        {
            int currentSign = GetInputSign(episode.points[i].signedAzimuthError, 0.001f);
            if (previousSign != 0 && currentSign != 0 && previousSign != currentSign)
            {
                zeroCrossCount++;
                if (HasActiveInputForDuration(episode.points, i, overshootInputHoldTime))
                    overshootCount++;
            }

            if (currentSign != 0)
                previousSign = currentSign;
        }

        episode.azimuthZeroCrossCount = zeroCrossCount;
        episode.overshootCount = overshootCount;
    }

    bool HasActiveInputForDuration(List<TurnPlaneCorrectionPoint> points, int startIndex, float requiredDuration)
    {
        float held = 0f;
        for (int i = startIndex; i < points.Count - 1; i++)
        {
            if (Mathf.Abs(points[i].rollInput) < correctionInputThreshold
                && Mathf.Abs(points[i].yawInput) < correctionInputThreshold)
                break;

            held += Mathf.Max(0f, points[i + 1].timeFromStart - points[i].timeFromStart);
            if (held >= requiredDuration)
                return true;
        }

        return false;
    }

    void CalculateHysteresisStats(ref TurnPlaneCorrectionEpisode episode)
    {
        if (episode.rollEvents == null)
            return;

        float lastStopAbsAzimuth = 0f;
        bool hasStop = false;
        float widthSum = 0f;
        int widthCount = 0;
        float lastReverseTime = -1f;
        float reversePeriodSum = 0f;
        float minReversePeriod = float.PositiveInfinity;
        int reversePeriodCount = 0;

        for (int i = 0; i < episode.rollEvents.Count; i++)
        {
            RollControlEvent controlEvent = episode.rollEvents[i];
            if (controlEvent.isStop)
            {
                lastStopAbsAzimuth = Mathf.Abs(controlEvent.signedAzimuthError);
                hasStop = true;
            }
            else if (controlEvent.isReinput && hasStop)
            {
                widthSum += Mathf.Abs(controlEvent.signedAzimuthError) - lastStopAbsAzimuth;
                widthCount++;
                hasStop = false;
            }

            if (!controlEvent.isReverse && !controlEvent.isYawReverse)
                continue;

            if (lastReverseTime >= 0f)
            {
                float period = Mathf.Max(0f, controlEvent.timeFromEpisodeStart - lastReverseTime);
                reversePeriodSum += period;
                minReversePeriod = Mathf.Min(minReversePeriod, period);
                reversePeriodCount++;
            }

            lastReverseTime = controlEvent.timeFromEpisodeStart;
        }

        episode.averageHysteresisWidth = widthCount > 0 ? widthSum / widthCount : 0f;
        episode.averageReversePeriod = reversePeriodCount > 0 ? reversePeriodSum / reversePeriodCount : 0f;
        episode.minReversePeriod = reversePeriodCount > 0 ? minReversePeriod : 0f;
    }

    void CalculateDelayedInputCorrelations(ref TurnPlaneCorrectionEpisode episode)
    {
        if (episode.points == null || episode.points.Count < 3)
            return;

        float maxLag = Mathf.Max(0f, delayedCorrelationMaxLag);
        int lagSteps = Mathf.Max(0, Mathf.RoundToInt(maxLag / Mathf.Max(0.001f, correctionSampleInterval)));
        FindBestDelayedCorrelation(episode.points, lagSteps, true, out episode.rollBestLagSeconds, out episode.rollBestLagCorrelation);
        FindBestDelayedCorrelation(episode.points, lagSteps, false, out episode.yawBestLagSeconds, out episode.yawBestLagCorrelation);
    }

    void FindBestDelayedCorrelation(
        List<TurnPlaneCorrectionPoint> points,
        int maxLagSteps,
        bool useRoll,
        out float bestLagSeconds,
        out float bestCorrelation)
    {
        bestLagSeconds = 0f;
        bestCorrelation = 0f;
        float bestAbsCorrelation = 0f;

        for (int lag = 0; lag <= maxLagSteps; lag++)
        {
            float correlation = CalculateInputToAzimuthRateCorrelation(points, lag, useRoll);
            float absCorrelation = Mathf.Abs(correlation);
            if (absCorrelation <= bestAbsCorrelation)
                continue;

            bestAbsCorrelation = absCorrelation;
            bestCorrelation = correlation;
            bestLagSeconds = lag * correctionSampleInterval;
        }
    }

    float CalculateInputToAzimuthRateCorrelation(List<TurnPlaneCorrectionPoint> points, int lagSteps, bool useRoll)
    {
        int count = points.Count - 1 - lagSteps;
        if (count < 2)
            return 0f;

        float inputSum = 0f;
        float rateSum = 0f;
        float inputSquareSum = 0f;
        float rateSquareSum = 0f;
        float crossSum = 0f;

        for (int i = 0; i < count; i++)
        {
            TurnPlaneCorrectionPoint inputPoint = points[i];
            TurnPlaneCorrectionPoint a = points[i + lagSteps];
            TurnPlaneCorrectionPoint b = points[i + lagSteps + 1];
            float dt = Mathf.Max(0.001f, b.timeFromStart - a.timeFromStart);
            float input = useRoll ? inputPoint.rollInput : inputPoint.yawInput;
            float azimuthReductionRate = (Mathf.Abs(a.signedAzimuthError) - Mathf.Abs(b.signedAzimuthError)) / dt;

            inputSum += input;
            rateSum += azimuthReductionRate;
            inputSquareSum += input * input;
            rateSquareSum += azimuthReductionRate * azimuthReductionRate;
            crossSum += input * azimuthReductionRate;
        }

        float covariance = crossSum - inputSum * rateSum / count;
        float inputVariance = inputSquareSum - inputSum * inputSum / count;
        float rateVariance = rateSquareSum - rateSum * rateSum / count;
        float denominator = Mathf.Sqrt(Mathf.Max(0f, inputVariance) * Mathf.Max(0f, rateVariance));
        return denominator > 0.0001f ? covariance / denominator : 0f;
    }

    void CalculateRollWindow(
        List<TurnPlaneCorrectionPoint> points,
        float eventTime,
        int direction,
        out float duration,
        out float peak,
        out float impulse)
    {
        duration = 0f;
        peak = 0f;
        impulse = 0f;
        if (points == null || points.Count < 2)
            return;

        int eventIndex = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].timeFromStart <= eventTime)
                eventIndex = i;
        }

        int start = direction < 0 ? eventIndex : Mathf.Min(points.Count - 1, eventIndex + 1);
        int end = direction < 0 ? 0 : points.Count - 1;
        int step = direction < 0 ? -1 : 1;
        int baseSign = GetInputSign(points[start].rollInput, rollReverseThreshold);

        for (int i = start; direction < 0 ? i > end : i < end; i += step)
        {
            int next = i + step;
            if (next < 0 || next >= points.Count)
                break;

            int sign = GetInputSign(points[i].rollInput, rollReverseThreshold);
            if (baseSign != 0 && sign != 0 && sign != baseSign)
                break;

            float dt = Mathf.Abs(points[next].timeFromStart - points[i].timeFromStart);
            float absRoll = Mathf.Abs(points[i].rollInput);
            duration += dt;
            peak = Mathf.Max(peak, absRoll);
            impulse += absRoll * dt;
        }
    }

    void RebuildNormalizedTurnPlaneAverage()
    {
        normalizedTurnPlaneAverage.Clear();
        const int count = 101;
        if (turnPlaneCorrectionEpisodes.Count == 0)
            return;

        float[] pitch = new float[count];
        float[] roll = new float[count];
        float[] yaw = new float[count];
        float[] azimuth = new float[count];
        float[] total = new float[count];
        float[] bank = new float[count];
        int usedEpisodes = 0;

        for (int e = 0; e < turnPlaneCorrectionEpisodes.Count; e++)
        {
            TurnPlaneCorrectionEpisode episode = turnPlaneCorrectionEpisodes[e];
            if (episode.points == null || episode.points.Count < 2 || episode.timeToMinimumRelativeAngle <= 0.001f)
                continue;

            usedEpisodes++;
            for (int i = 0; i < count; i++)
            {
                float normalized = i / 100f;
                TurnPlaneCorrectionPoint point = SampleTurnPlanePoint(episode.points, normalized * episode.timeToMinimumRelativeAngle);
                pitch[i] += point.pitchInput;
                roll[i] += point.rollInput;
                yaw[i] += point.yawInput;
                azimuth[i] += point.signedAzimuthError;
                total[i] += point.totalRelativeAngle;
                bank[i] += point.bankAngle;
            }
        }

        if (usedEpisodes == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            normalizedTurnPlaneAverage.Add(new NormalizedTurnPlanePoint
            {
                normalizedTime = i / 100f,
                pitchInput = pitch[i] / usedEpisodes,
                rollInput = roll[i] / usedEpisodes,
                yawInput = yaw[i] / usedEpisodes,
                signedAzimuthError = azimuth[i] / usedEpisodes,
                totalRelativeAngle = total[i] / usedEpisodes,
                bankAngle = bank[i] / usedEpisodes
            });
        }
    }

    static TurnPlaneCorrectionPoint SampleTurnPlanePoint(List<TurnPlaneCorrectionPoint> points, float time)
    {
        if (points == null || points.Count == 0)
            return default;

        if (time <= points[0].timeFromStart)
            return points[0];

        for (int i = 1; i < points.Count; i++)
        {
            if (time > points[i].timeFromStart)
                continue;

            TurnPlaneCorrectionPoint a = points[i - 1];
            TurnPlaneCorrectionPoint b = points[i];
            float t = Mathf.InverseLerp(a.timeFromStart, b.timeFromStart, time);
            return new TurnPlaneCorrectionPoint
            {
                timeFromStart = time,
                pitchInput = Mathf.Lerp(a.pitchInput, b.pitchInput, t),
                rollInput = Mathf.Lerp(a.rollInput, b.rollInput, t),
                yawInput = Mathf.Lerp(a.yawInput, b.yawInput, t),
                signedAzimuthError = Mathf.Lerp(a.signedAzimuthError, b.signedAzimuthError, t),
                signedElevationError = Mathf.Lerp(a.signedElevationError, b.signedElevationError, t),
                totalRelativeAngle = Mathf.Lerp(a.totalRelativeAngle, b.totalRelativeAngle, t),
                bankAngle = Mathf.Lerp(a.bankAngle, b.bankAngle, t),
                localAngularVelocity = Vector3.Lerp(a.localAngularVelocity, b.localAngularVelocity, t),
                distance = Mathf.Lerp(a.distance, b.distance, t),
                closureRate = Mathf.Lerp(a.closureRate, b.closureRate, t)
            };
        }

        return points[points.Count - 1];
    }

    void RebuildTurnPlaneCorrectionSummary()
    {
        leftCorrectionEpisodeCount = 0;
        rightCorrectionEpisodeCount = 0;
        float leftEfficiencySum = 0f;
        float rightEfficiencySum = 0f;
        float leftHysteresisSum = 0f;
        float rightHysteresisSum = 0f;

        for (int i = 0; i < turnPlaneCorrectionEpisodes.Count; i++)
        {
            TurnPlaneCorrectionEpisode episode = turnPlaneCorrectionEpisodes[i];
            if (episode.initialAzimuthSign < 0)
            {
                leftCorrectionEpisodeCount++;
                leftEfficiencySum += episode.correctionEfficiency;
                leftHysteresisSum += episode.averageHysteresisWidth;
            }
            else if (episode.initialAzimuthSign > 0)
            {
                rightCorrectionEpisodeCount++;
                rightEfficiencySum += episode.correctionEfficiency;
                rightHysteresisSum += episode.averageHysteresisWidth;
            }
        }

        leftCorrectionEfficiencyAverage = leftCorrectionEpisodeCount > 0 ? leftEfficiencySum / leftCorrectionEpisodeCount : 0f;
        rightCorrectionEfficiencyAverage = rightCorrectionEpisodeCount > 0 ? rightEfficiencySum / rightCorrectionEpisodeCount : 0f;
        leftHysteresisWidthAverage = leftCorrectionEpisodeCount > 0 ? leftHysteresisSum / leftCorrectionEpisodeCount : 0f;
        rightHysteresisWidthAverage = rightCorrectionEpisodeCount > 0 ? rightHysteresisSum / rightCorrectionEpisodeCount : 0f;
        correctionEfficiencyAsymmetry = rightCorrectionEfficiencyAverage - leftCorrectionEfficiencyAverage;
        hysteresisWidthAsymmetry = rightHysteresisWidthAverage - leftHysteresisWidthAverage;
    }

    void RebuildTurnPlaneGainBins()
    {
        int safeBinCount = Mathf.Max(1, turnPlaneGainBinCount);
        turnPlaneGainBins = new TurnPlaneGainBin[safeBinCount];
        float binWidth = 180f / safeBinCount;
        float[] rollSums = new float[safeBinCount];
        float[] yawSums = new float[safeBinCount];
        float[] reductionSums = new float[safeBinCount];

        for (int i = 0; i < safeBinCount; i++)
        {
            turnPlaneGainBins[i] = new TurnPlaneGainBin
            {
                minAzimuthAbs = i * binWidth,
                maxAzimuthAbs = (i + 1) * binWidth
            };
        }

        for (int e = 0; e < turnPlaneCorrectionEpisodes.Count; e++)
        {
            List<TurnPlaneCorrectionPoint> points = turnPlaneCorrectionEpisodes[e].points;
            if (points == null || points.Count < 2)
                continue;

            for (int i = 0; i < points.Count - 1; i++)
            {
                float azimuthAbs = Mathf.Abs(points[i].signedAzimuthError);
                int index = Mathf.Clamp(Mathf.FloorToInt(azimuthAbs / 180f * safeBinCount), 0, safeBinCount - 1);
                float dt = Mathf.Max(0.001f, points[i + 1].timeFromStart - points[i].timeFromStart);
                float reductionRate = (azimuthAbs - Mathf.Abs(points[i + 1].signedAzimuthError)) / dt;

                TurnPlaneGainBin bin = turnPlaneGainBins[index];
                bin.sampleCount++;
                turnPlaneGainBins[index] = bin;
                rollSums[index] += Mathf.Abs(points[i].rollInput);
                yawSums[index] += Mathf.Abs(points[i].yawInput);
                reductionSums[index] += reductionRate;
            }
        }

        for (int i = 0; i < safeBinCount; i++)
        {
            TurnPlaneGainBin bin = turnPlaneGainBins[i];
            if (bin.sampleCount > 0)
            {
                bin.averageRollAbs = rollSums[i] / bin.sampleCount;
                bin.averageYawAbs = yawSums[i] / bin.sampleCount;
                bin.averageAzimuthReductionRate = reductionSums[i] / bin.sampleCount;
            }

            turnPlaneGainBins[i] = bin;
        }
    }

    void RebuildTurnPlaneIqrHistory()
    {
        turnPlaneIqrHistory.Clear();
        int windowSize = Mathf.Max(1, iqrEpisodeWindowSize);
        if (turnPlaneCorrectionEpisodes.Count == 0)
            return;

        for (int start = 0; start < turnPlaneCorrectionEpisodes.Count; start += windowSize)
        {
            int end = Mathf.Min(turnPlaneCorrectionEpisodes.Count, start + windowSize);
            int count = end - start;
            float[] minRelativeAngles = new float[count];
            float[] finalAzimuthAbs = new float[count];
            float[] efficiencies = new float[count];

            for (int i = 0; i < count; i++)
            {
                TurnPlaneCorrectionEpisode episode = turnPlaneCorrectionEpisodes[start + i];
                minRelativeAngles[i] = episode.minimumRelativeAngle;
                finalAzimuthAbs[i] = Mathf.Abs(episode.finalAzimuth);
                efficiencies[i] = episode.correctionEfficiency;
            }

            CalculateQuartiles(minRelativeAngles, out float minQ1, out float minMedian, out float minQ3, out float minIqr);
            CalculateQuartiles(finalAzimuthAbs, out float finalQ1, out float finalMedian, out float finalQ3, out float finalIqr);
            CalculateQuartiles(efficiencies, out float effQ1, out float effMedian, out float effQ3, out float effIqr);

            turnPlaneIqrHistory.Add(new TurnPlaneIqrPoint
            {
                startEpisodeIndex = start,
                endEpisodeIndex = end - 1,
                minimumRelativeAngleMedian = minMedian,
                minimumRelativeAngleQ1 = minQ1,
                minimumRelativeAngleQ3 = minQ3,
                minimumRelativeAngleIqr = minIqr,
                finalAzimuthAbsMedian = finalMedian,
                finalAzimuthAbsQ1 = finalQ1,
                finalAzimuthAbsQ3 = finalQ3,
                finalAzimuthAbsIqr = finalIqr,
                correctionEfficiencyMedian = effMedian,
                correctionEfficiencyQ1 = effQ1,
                correctionEfficiencyQ3 = effQ3,
                correctionEfficiencyIqr = effIqr
            });
        }
    }

    static void CalculateQuartiles(float[] values, out float q1, out float median, out float q3, out float iqr)
    {
        q1 = 0f;
        median = 0f;
        q3 = 0f;
        iqr = 0f;
        if (values == null || values.Length == 0)
            return;

        System.Array.Sort(values);
        q1 = GetPercentile(values, 0.25f);
        median = GetPercentile(values, 0.5f);
        q3 = GetPercentile(values, 0.75f);
        iqr = q3 - q1;
    }

    static float GetPercentile(float[] sortedValues, float percentile)
    {
        if (sortedValues.Length == 1)
            return sortedValues[0];

        float scaled = Mathf.Clamp01(percentile) * (sortedValues.Length - 1);
        int low = Mathf.FloorToInt(scaled);
        int high = Mathf.Min(sortedValues.Length - 1, low + 1);
        return Mathf.Lerp(sortedValues[low], sortedValues[high], scaled - low);
    }

    static int GetInputSign(float value, float threshold)
    {
        if (value > threshold)
            return 1;
        if (value < -threshold)
            return -1;
        return 0;
    }

    void RecordDisengageSample()
    {
        float angle = analyzer.PlayerNoseToEnemyAngle;

        if (disengageConfirmed)
        {
            if (angle < disengageResetAngle)
            {
                disengageConfirmed = false;
                disengageAngleTimer = 0f;
            }

            return;
        }

        if (angle < disengageAngleThreshold)
        {
            disengagePending = false;
            disengageAngleTimer = 0f;
            return;
        }

        disengageAngleTimer += Time.deltaTime;
        if (!disengagePending)
        {
            disengagePending = true;
            pendingDisengageSample = CreateDisengageSample(Time.time, Time.time, 0f);
        }

        if (disengageAngleTimer >= disengageConfirmHoldTime
            && analyzer.Distance > disengageMinDistance)
        {
            pendingDisengageSample.confirmedTime = Time.time;
            pendingDisengageSample.angleHoldTime = disengageAngleTimer;
            AddDisengageSample(pendingDisengageSample);
            disengagePending = false;
            disengageConfirmed = true;
        }
    }

    DisengageSample CreateDisengageSample(float time, float confirmedTime, float angleHoldTime)
    {
        return new DisengageSample
        {
            confirmedTime = confirmedTime,
            angleHoldTime = angleHoldTime,
            playerNoseToEnemyAngle = analyzer.PlayerNoseToEnemyAngle,
            distance = analyzer.Distance
        };
    }

    void RecordNoseAngleSample()
    {
        EnsureAnalysisBins();

        float angleAbs = Mathf.Abs(analyzer.PlayerNoseToEnemyAngle);
        int noseBin = Mathf.Min(noseAngleHistogramBins.Length - 1, Mathf.FloorToInt(Mathf.Clamp(angleAbs, 0f, 180f) / 180f * noseAngleHistogramBins.Length));
        noseAngleHistogramBins[noseBin]++;

        Transform playerTransform = analyzer.playerObject != null ? analyzer.playerObject.transform : null;
        Transform enemyTransform = analyzer.enemyObject != null ? analyzer.enemyObject.transform : null;
        if (playerTransform == null || enemyTransform == null)
            return;

        Vector3 localToEnemy = playerTransform.InverseTransformDirection(enemyTransform.position - playerTransform.position);
        Vector2 forwardPlane = new Vector2(localToEnemy.x, localToEnemy.y);
        if (forwardPlane.sqrMagnitude < 0.0001f)
            return;

        float sectorAngle = Mathf.Atan2(forwardPlane.y, forwardPlane.x) * Mathf.Rad2Deg;
        if (sectorAngle < 0f)
            sectorAngle += 360f;

        int sector = Mathf.FloorToInt((sectorAngle + 22.5f) / 45f) % 8;
        float duration = Mathf.Max(0f, Time.deltaTime);
        relativeDirectionSectorSeconds[sector] += duration;
        relativeDirectionSectorAngleSums[sector] += angleAbs * duration;
        relativeDirectionSectorCounts[sector]++;

        int angleBin = Mathf.Min(17, Mathf.FloorToInt(Mathf.Clamp(angleAbs, 0f, 180f) / 10f));
        relativeDirectionSectorAngleBins[sector * 18 + angleBin]++;
    }

    void EnsureGraphBins()
    {
        int safeBinCount = Mathf.Max(1, binCount);
        if (graphBins == null
            || graphBins.Length != safeBinCount
            || lowPitchRatioSums == null
            || lowPitchRatioSums.Length != safeBinCount
            || lowPitchPrecisionSums == null
            || lowPitchPrecisionSums.Length != safeBinCount
            || disengageDistanceSums == null
            || disengageDistanceSums.Length != safeBinCount)
        {
            RebuildGraphBins();
        }
    }

    void EnsureAnalysisBins()
    {
        if (noseAngleHistogramBins == null || noseAngleHistogramBins.Length != 18)
            noseAngleHistogramBins = new int[18];

        if (relativeDirectionSectorSeconds == null || relativeDirectionSectorSeconds.Length != 8)
            relativeDirectionSectorSeconds = new float[8];

        if (relativeDirectionSectorAngleSums == null || relativeDirectionSectorAngleSums.Length != 8)
            relativeDirectionSectorAngleSums = new float[8];

        if (relativeDirectionSectorCounts == null || relativeDirectionSectorCounts.Length != 8)
            relativeDirectionSectorCounts = new int[8];

        if (relativeDirectionSectorAngleBins == null || relativeDirectionSectorAngleBins.Length != 8 * 18)
            relativeDirectionSectorAngleBins = new int[8 * 18];
    }

    int GetBinIndex(float angle)
    {
        EnsureGraphBins();

        float range = Mathf.Max(0.001f, maxAngle - minAngle);
        float binWidth = range / Mathf.Max(1, graphBins.Length);
        return Mathf.Clamp(Mathf.FloorToInt((angle - minAngle) / binWidth), 0, graphBins.Length - 1);
    }

    void AccumulateLowPitchSample(LowPitchAxisSample sample, int direction)
    {
        if (direction == 0)
            return;

        int index = GetBinIndex(sample.pitchAxisToEnemyAngle);
        GraphBin bin = graphBins[index];
        bin.sampleCount = Mathf.Max(0, bin.sampleCount + direction);
        bin.lowPitchSampleCount = Mathf.Max(0, bin.lowPitchSampleCount + direction);
        lowPitchRatioSums[index] = Mathf.Max(0f, lowPitchRatioSums[index] + sample.rollPitchInputRatio * direction);
        lowPitchPrecisionSums[index] = Mathf.Max(0f, lowPitchPrecisionSums[index] + sample.axisPrecision * direction);

        if (bin.lowPitchSampleCount > 0)
        {
            bin.averageRollPitchInputRatio = lowPitchRatioSums[index] / bin.lowPitchSampleCount;
            bin.averageAxisPrecision = lowPitchPrecisionSums[index] / bin.lowPitchSampleCount;
        }
        else
        {
            bin.averageRollPitchInputRatio = 0f;
            bin.averageAxisPrecision = 0f;
        }

        graphBins[index] = bin;
    }

    void AccumulateDisengageSample(DisengageSample sample, int direction)
    {
        if (direction == 0)
            return;

        int index = GetBinIndex(sample.playerNoseToEnemyAngle);
        GraphBin bin = graphBins[index];
        bin.sampleCount = Mathf.Max(0, bin.sampleCount + direction);
        bin.disengageSampleCount = Mathf.Max(0, bin.disengageSampleCount + direction);
        disengageDistanceSums[index] += sample.distance * direction;

        if (bin.disengageSampleCount > 0)
            bin.averageDisengageDistance = disengageDistanceSums[index] / bin.disengageSampleCount;
        else
            bin.averageDisengageDistance = 0f;

        graphBins[index] = bin;

        disengageDistanceSum += sample.distance * direction;
        disengageAngleSum += sample.playerNoseToEnemyAngle * direction;
    }

    void UpdateDisengageSummary()
    {
        int count = disengageSampleCount;
        if (count == 0)
        {
            disengageFrequencyPerMinute = 0f;
            averageDisengageDistance = 0f;
            averageDisengageAngle = 0f;
            return;
        }

        float elapsedMinutes = Mathf.Max(0.001f, (Time.time - recordingStartTime) / 60f);
        disengageFrequencyPerMinute = count / elapsedMinutes;
        averageDisengageDistance = disengageDistanceSum / count;
        averageDisengageAngle = disengageAngleSum / count;
    }

    void OnValidate()
    {
        minSampleInterval = Mathf.Max(0f, minSampleInterval);
        correctionStartAzimuthThreshold = Mathf.Clamp(correctionStartAzimuthThreshold, 0f, 180f);
        correctionEndAzimuthThreshold = Mathf.Clamp(correctionEndAzimuthThreshold, 0f, correctionStartAzimuthThreshold);
        correctionInputThreshold = Mathf.Clamp01(correctionInputThreshold);
        correctionPitchThreshold = Mathf.Clamp01(correctionPitchThreshold);
        correctionSampleInterval = Mathf.Max(0.001f, correctionSampleInterval);
        correctionMaxDuration = Mathf.Max(correctionSampleInterval, correctionMaxDuration);
        maxCorrectionEpisodes = Mathf.Max(1, maxCorrectionEpisodes);
        maxCorrectionPointsPerEpisode = Mathf.Max(2, maxCorrectionPointsPerEpisode);
        rollControlStopThreshold = Mathf.Clamp01(rollControlStopThreshold);
        rollReverseThreshold = Mathf.Clamp01(rollReverseThreshold);
        overshootInputHoldTime = Mathf.Max(0f, overshootInputHoldTime);
        delayedCorrelationMaxLag = Mathf.Max(0f, delayedCorrelationMaxLag);
        turnPlaneGainBinCount = Mathf.Max(1, turnPlaneGainBinCount);
        iqrEpisodeWindowSize = Mathf.Max(1, iqrEpisodeWindowSize);
        disengageAngleThreshold = Mathf.Clamp(disengageAngleThreshold, 0f, 180f);
        disengageResetAngle = Mathf.Clamp(disengageResetAngle, 0f, disengageAngleThreshold);
        disengageConfirmHoldTime = Mathf.Max(0f, disengageConfirmHoldTime);
        disengageMinDistance = Mathf.Max(0f, disengageMinDistance);
        binCount = Mathf.Max(1, binCount);
        if (maxAngle <= minAngle)
            maxAngle = minAngle + 1f;

        if (!Application.isPlaying)
            RebuildGraphBins();
    }
}
