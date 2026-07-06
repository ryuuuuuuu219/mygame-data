using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class AirCombatBehaviorRecorder : MonoBehaviour
{
    [System.Serializable]
    public struct LowPitchAxisSample
    {
        public float time;
        public float pitchAxisToEnemyAngle;
        public float rollPitchInputRatio;
        public float axisPrecision;
        public float distance;
        public float closureRate;
        public float playerThrottle;
        public float enemyThrottle;
    }

    [System.Serializable]
    public struct RollCorrectionSample
    {
        public float time;
        public float rollStartPitchAxisAngle;
        public float rollEndPitchAxisAngle;
        public float rollDuration;
        public float maxRollInput;
        public float distanceAtStart;
        public float distanceAtEnd;
        public float closureRateAtStart;
        public float closureRateAtEnd;
        public float pitchInputAtEnd;
    }

    [System.Serializable]
    public struct GraphBin
    {
        public float minAngle;
        public float maxAngle;
        public float centerAngle;
        public int sampleCount;
        public int lowPitchSampleCount;
        public int rollCorrectionSampleCount;
        public float averageRollPitchInputRatio;
        public float averageAxisPrecision;
        public float averageRollEndPitchAxisAngle;
    }

    [Header("Source")]
    public AirCombatBehaviorAnalyzer analyzer;

    [Header("Recording")]
    [SerializeField] bool record = true;
    [SerializeField] int maxSamples = 512;
    [SerializeField] float minSampleInterval = 0.08f;
    [SerializeField] bool requireEnemyInView;
    [SerializeField] float rollStartThreshold = 0.55f;
    [SerializeField] float rollStopThreshold = 0.2f;

    [Header("Graph Bins")]
    [SerializeField] float minAngle = -90f;
    [SerializeField] float maxAngle = 90f;
    [SerializeField] int binCount = 18;

    [Header("Samples")]
    [SerializeField] List<LowPitchAxisSample> lowPitchAxisSamples = new();
    [SerializeField] List<RollCorrectionSample> rollCorrectionSamples = new();
    [SerializeField] GraphBin[] graphBins;
    [SerializeField] float rollCorrectionCorrelation;

    float nextSampleTime;
    bool rollCorrectionActive;
    float previousAbsRollInput;
    float rollStartPitchAxisAngle;
    float rollStartDistance;
    float rollStartClosureRate;
    float rollStartTime;
    float maxRollInputDuringCorrection;

    public IReadOnlyList<LowPitchAxisSample> LowPitchAxisSamples => lowPitchAxisSamples;
    public IReadOnlyList<RollCorrectionSample> RollCorrectionSamples => rollCorrectionSamples;
    public IReadOnlyList<GraphBin> GraphBins => graphBins;
    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public float RollCorrectionCorrelation => rollCorrectionCorrelation;

    void Reset()
    {
        analyzer = GetComponent<AirCombatBehaviorAnalyzer>();
        RebuildGraphBins();
    }

    void Awake()
    {
        if (analyzer == null)
            analyzer = GetComponent<AirCombatBehaviorAnalyzer>();

        RebuildGraphBins();
    }

    void LateUpdate()
    {
        if (!record || analyzer == null || !analyzer.HasValidTargets)
            return;

        RecordLowPitchSample();
        RecordRollCorrectionSample();
    }

    public void ClearSamples()
    {
        lowPitchAxisSamples.Clear();
        rollCorrectionSamples.Clear();
        rollCorrectionActive = false;
        RebuildGraphBins();
    }

    public void RebuildGraphBins()
    {
        int safeBinCount = Mathf.Max(1, binCount);
        graphBins = new GraphBin[safeBinCount];

        float range = Mathf.Max(0.001f, maxAngle - minAngle);
        float binWidth = range / safeBinCount;
        float[] ratioSums = new float[safeBinCount];
        float[] precisionSums = new float[safeBinCount];
        float[] rollEndAngleSums = new float[safeBinCount];

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

        for (int i = 0; i < lowPitchAxisSamples.Count; i++)
        {
            LowPitchAxisSample sample = lowPitchAxisSamples[i];
            int index = Mathf.FloorToInt((sample.pitchAxisToEnemyAngle - minAngle) / binWidth);
            index = Mathf.Clamp(index, 0, safeBinCount - 1);

            GraphBin bin = graphBins[index];
            bin.sampleCount++;
            bin.lowPitchSampleCount++;
            graphBins[index] = bin;
            ratioSums[index] += sample.rollPitchInputRatio;
            precisionSums[index] += sample.axisPrecision;
        }

        for (int i = 0; i < rollCorrectionSamples.Count; i++)
        {
            RollCorrectionSample sample = rollCorrectionSamples[i];
            int index = Mathf.FloorToInt((sample.rollStartPitchAxisAngle - minAngle) / binWidth);
            index = Mathf.Clamp(index, 0, safeBinCount - 1);

            GraphBin bin = graphBins[index];
            bin.sampleCount++;
            bin.rollCorrectionSampleCount++;
            graphBins[index] = bin;
            rollEndAngleSums[index] += sample.rollEndPitchAxisAngle;
        }

        for (int i = 0; i < safeBinCount; i++)
        {
            GraphBin bin = graphBins[i];
            if (bin.lowPitchSampleCount > 0)
            {
                bin.averageRollPitchInputRatio = ratioSums[i] / bin.lowPitchSampleCount;
                bin.averageAxisPrecision = precisionSums[i] / bin.lowPitchSampleCount;
            }

            if (bin.rollCorrectionSampleCount > 0)
                bin.averageRollEndPitchAxisAngle = rollEndAngleSums[i] / bin.rollCorrectionSampleCount;

            graphBins[i] = bin;
        }

        rollCorrectionCorrelation = CalculateRollCorrectionCorrelation();
    }

    void AddSample(LowPitchAxisSample sample)
    {
        lowPitchAxisSamples.Add(sample);
        while (lowPitchAxisSamples.Count > maxSamples)
            lowPitchAxisSamples.RemoveAt(0);

        RebuildGraphBins();
    }

    void AddRollCorrectionSample(RollCorrectionSample sample)
    {
        rollCorrectionSamples.Add(sample);
        while (rollCorrectionSamples.Count > maxSamples)
            rollCorrectionSamples.RemoveAt(0);

        RebuildGraphBins();
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
            time = Time.time,
            pitchAxisToEnemyAngle = analyzer.LowPitchPitchAxisToEnemyAngle,
            rollPitchInputRatio = analyzer.LowPitchRollPitchRatio,
            axisPrecision = analyzer.LowPitchAxisPrecision,
            distance = analyzer.Distance,
            closureRate = analyzer.ClosureRate,
            playerThrottle = analyzer.PlayerThrottle,
            enemyThrottle = analyzer.EnemyThrottle
        });
    }

    void RecordRollCorrectionSample()
    {
        float absRollInput = Mathf.Abs(analyzer.RollInput);
        bool canRecordFrame = !requireEnemyInView || analyzer.EnemyInView;

        if (!rollCorrectionActive
            && canRecordFrame
            && previousAbsRollInput < rollStartThreshold
            && absRollInput >= rollStartThreshold)
        {
            rollCorrectionActive = true;
            rollStartPitchAxisAngle = analyzer.PitchAxisToEnemyAngle;
            rollStartDistance = analyzer.Distance;
            rollStartClosureRate = analyzer.ClosureRate;
            rollStartTime = Time.time;
            maxRollInputDuringCorrection = absRollInput;
        }

        if (rollCorrectionActive)
        {
            maxRollInputDuringCorrection = Mathf.Max(maxRollInputDuringCorrection, absRollInput);

            if (previousAbsRollInput > rollStopThreshold && absRollInput <= rollStopThreshold)
            {
                AddRollCorrectionSample(new RollCorrectionSample
                {
                    time = Time.time,
                    rollStartPitchAxisAngle = rollStartPitchAxisAngle,
                    rollEndPitchAxisAngle = analyzer.PitchAxisToEnemyAngle,
                    rollDuration = Time.time - rollStartTime,
                    maxRollInput = maxRollInputDuringCorrection,
                    distanceAtStart = rollStartDistance,
                    distanceAtEnd = analyzer.Distance,
                    closureRateAtStart = rollStartClosureRate,
                    closureRateAtEnd = analyzer.ClosureRate,
                    pitchInputAtEnd = analyzer.PitchInput
                });

                rollCorrectionActive = false;
            }
        }

        previousAbsRollInput = absRollInput;
    }

    float CalculateRollCorrectionCorrelation()
    {
        int count = rollCorrectionSamples.Count;
        if (count < 2)
            return 0f;

        float sumX = 0f;
        float sumY = 0f;
        for (int i = 0; i < count; i++)
        {
            sumX += rollCorrectionSamples[i].rollStartPitchAxisAngle;
            sumY += rollCorrectionSamples[i].rollEndPitchAxisAngle;
        }

        float meanX = sumX / count;
        float meanY = sumY / count;
        float covariance = 0f;
        float varianceX = 0f;
        float varianceY = 0f;

        for (int i = 0; i < count; i++)
        {
            float dx = rollCorrectionSamples[i].rollStartPitchAxisAngle - meanX;
            float dy = rollCorrectionSamples[i].rollEndPitchAxisAngle - meanY;
            covariance += dx * dy;
            varianceX += dx * dx;
            varianceY += dy * dy;
        }

        float denominator = Mathf.Sqrt(varianceX * varianceY);
        return denominator > 0.0001f ? covariance / denominator : 0f;
    }

    void OnValidate()
    {
        maxSamples = Mathf.Max(1, maxSamples);
        minSampleInterval = Mathf.Max(0f, minSampleInterval);
        rollStartThreshold = Mathf.Clamp01(rollStartThreshold);
        rollStopThreshold = Mathf.Clamp(rollStopThreshold, 0f, rollStartThreshold);
        binCount = Mathf.Max(1, binCount);
        if (maxAngle <= minAngle)
            maxAngle = minAngle + 1f;

        RebuildGraphBins();
    }
}
