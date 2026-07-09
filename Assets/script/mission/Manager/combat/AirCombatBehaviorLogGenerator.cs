using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[AddComponentMenu("Air Combat/Air Combat Behavior Log Generator")]
public class AirCombatBehaviorLogGenerator : MonoBehaviour
{
    public const string CsvHeader = "Index,StartTime,EndTime,Duration,InitialAzimuth,InitialAzimuthSign,MinimumAzimuthAbs,FinalAzimuth,MinimumRelativeAngle,TimeToMinimumRelativeAngle,RollStopCount,RollReverseCount,YawReverseCount,AzimuthZeroCrossCount,OvershootCount,RollMaxAbs,RollMeanAbs,RollRms,RollSignedIntegral,RollAbsoluteIntegral,RollQ1,RollMedian,RollQ3,RollIqr,YawMaxAbs,YawMeanAbs,YawRms,YawSignedIntegral,YawAbsoluteIntegral,YawQ1,YawMedian,YawQ3,YawIqr,YawRollEffortRatio,YawRollRmsRatio,CorrectionEfficiency,ReactionDelay,ReleaseDelay,PostMinimumInputDuration,AverageReversePeriod,MinReversePeriod,AverageHysteresisWidth,AverageReverseImpulseRatio,AverageReversePeakRatio,AverageReverseDurationRatio,RollBestLagSeconds,RollBestLagCorrelation,YawBestLagSeconds,YawBestLagCorrelation,PointCount,RollEventCount,IsValidForSummary,InvalidReason,CorrectionClass,EndReason,AzimuthWrapCount,IntermediateRollStopCount,RollReinputCount,TerminalRollRelease,RollLagCorrelationValid,RollLagPairCount,YawLagCorrelationValid,YawLagPairCount,InitialTotalRelativeAngle,FinalTotalRelativeAngle,InitialElevationError,FinalElevationError,InitialDistance,FinalDistance,InitialClosureRate,FinalClosureRate,InitialBankAngle,FinalBankAngle,InitialLocalRollRate,FinalLocalRollRate,LocalRollRateMaxAbs,LocalRollRateMeanAbs,LocalRollRateRms,LocalRollRateSignedIntegral,LocalRollRateAbsoluteIntegral,PitchMaxAbs,PitchMeanAbs,PitchRms,PitchSignedIntegral,PitchAbsoluteIntegral,PitchQ1,PitchMedian,PitchQ3,PitchIqr,InitialVelocityDirectionAngle,InitialVelocityLineRelation,InitialVelocityEncounterType,InitialVelocityLineClosestDistance,InitialNormalizedVelocityLineSeparation,InitialNormalizedVelocityTripleProduct,InitialTimeToClosestApproach,InitialPredictedClosestApproachDistance,FinalVelocityDirectionAngle,FinalVelocityLineRelation,FinalVelocityEncounterType,FinalVelocityLineClosestDistance,FinalNormalizedVelocityLineSeparation,FinalNormalizedVelocityTripleProduct,FinalTimeToClosestApproach,FinalPredictedClosestApproachDistance";
    public const string PointCsvHeader = "TimeFromStart,PitchInput,RollInput,YawInput,SignedAzimuthError,SignedElevationError,TotalRelativeAngle,BankAngle,LocalAngularVelocityX,LocalAngularVelocityY,LocalAngularVelocityZ,Distance,ClosureRate";

    [SerializeField] AirCombatBehaviorServer server;

    public AirCombatBehaviorServer Server => server;
    public string CurrentLog => GenerateCsv(server);

    void Reset()
    {
        server = GetComponent<AirCombatBehaviorServer>();
    }

    public void SetServer(AirCombatBehaviorServer value)
    {
        server = value;
    }

    public static string GenerateCsv(AirCombatBehaviorServer source)
    {
        return GenerateCsv(source != null ? source.TurnPlaneCorrectionEpisodes : null);
    }

    public static string GenerateCsv(
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes,
        int startIndex = 0,
        int endIndex = int.MaxValue)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(CsvHeader);
        int count = episodes?.Count ?? 0;
        if (count == 0)
            return builder.ToString();

        int start = Mathf.Clamp(startIndex, 0, count - 1);
        int end = Mathf.Clamp(endIndex, 0, count - 1);
        if (start > end)
            (start, end) = (end, start);

        for (int i = start; i <= end; i++)
            AppendEpisode(builder, i, episodes[i]);
        return builder.ToString();
    }

    static void AppendEpisode(
        StringBuilder builder,
        int index,
        AirCombatBehaviorServer.TurnPlaneCorrectionEpisode e)
    {
        builder.Append(index).Append(',')
            .Append(F(e.startTime, "F3")).Append(',').Append(F(e.endTime, "F3")).Append(',')
            .Append(F(e.duration, "F3")).Append(',').Append(F(e.initialAzimuth, "F3")).Append(',')
            .Append(e.initialAzimuthSign).Append(',').Append(F(e.minimumAzimuthAbs, "F3")).Append(',')
            .Append(F(e.finalAzimuth, "F3")).Append(',').Append(F(e.minimumRelativeAngle, "F3")).Append(',')
            .Append(F(e.timeToMinimumRelativeAngle, "F3")).Append(',').Append(e.rollStopCount).Append(',')
            .Append(e.rollReverseCount).Append(',').Append(e.yawReverseCount).Append(',')
            .Append(e.azimuthZeroCrossCount).Append(',').Append(e.overshootCount).Append(',')
            .Append(F(e.rollMaxAbs)).Append(',').Append(F(e.rollMeanAbs)).Append(',')
            .Append(F(e.rollRms)).Append(',').Append(F(e.rollSignedIntegral)).Append(',')
            .Append(F(e.rollAbsoluteIntegral)).Append(',').Append(F(e.rollQ1)).Append(',')
            .Append(F(e.rollMedian)).Append(',').Append(F(e.rollQ3)).Append(',').Append(F(e.rollIqr)).Append(',')
            .Append(F(e.yawMaxAbs)).Append(',').Append(F(e.yawMeanAbs)).Append(',')
            .Append(F(e.yawRms)).Append(',').Append(F(e.yawSignedIntegral)).Append(',')
            .Append(F(e.yawAbsoluteIntegral)).Append(',').Append(F(e.yawQ1)).Append(',')
            .Append(F(e.yawMedian)).Append(',').Append(F(e.yawQ3)).Append(',').Append(F(e.yawIqr)).Append(',')
            .Append(F(e.yawRollEffortRatio)).Append(',').Append(F(e.yawRollRmsRatio)).Append(',')
            .Append(F(e.correctionEfficiency)).Append(',').Append(F(e.reactionDelay, "F3")).Append(',')
            .Append(F(e.releaseDelay, "F3")).Append(',').Append(F(e.postMinimumInputDuration, "F3")).Append(',')
            .Append(F(e.averageReversePeriod, "F3")).Append(',').Append(F(e.minReversePeriod, "F3")).Append(',')
            .Append(F(e.averageHysteresisWidth, "F3")).Append(',').Append(F(e.averageReverseImpulseRatio)).Append(',')
            .Append(F(e.averageReversePeakRatio)).Append(',').Append(F(e.averageReverseDurationRatio)).Append(',')
            .Append(F(e.rollBestLagSeconds, "F3")).Append(',').Append(F(e.rollBestLagCorrelation)).Append(',')
            .Append(F(e.yawBestLagSeconds, "F3")).Append(',').Append(F(e.yawBestLagCorrelation)).Append(',')
            .Append(e.points?.Count ?? 0).Append(',').Append(e.rollEvents?.Count ?? 0).Append(',')
            .Append(e.isValidForSummary).Append(',').Append(e.invalidReason).Append(',')
            .Append(e.correctionClass).Append(',').Append(e.endReason).Append(',')
            .Append(e.azimuthWrapCount).Append(',').Append(e.intermediateRollStopCount).Append(',')
            .Append(e.rollReinputCount).Append(',').Append(e.terminalRollRelease).Append(',')
            .Append(e.rollLagCorrelationValid).Append(',').Append(e.rollLagPairCount).Append(',')
            .Append(e.yawLagCorrelationValid).Append(',').Append(e.yawLagPairCount).Append(',')
            .Append(F(e.initialTotalRelativeAngle)).Append(',').Append(F(e.finalTotalRelativeAngle)).Append(',')
            .Append(F(e.initialElevationError)).Append(',').Append(F(e.finalElevationError)).Append(',')
            .Append(F(e.initialDistance)).Append(',').Append(F(e.finalDistance)).Append(',')
            .Append(F(e.initialClosureRate)).Append(',').Append(F(e.finalClosureRate)).Append(',')
            .Append(F(e.initialBankAngle)).Append(',').Append(F(e.finalBankAngle)).Append(',')
            .Append(F(e.initialLocalAngularVelocity.z)).Append(',').Append(F(e.finalLocalAngularVelocity.z)).Append(',')
            .Append(F(e.localRollRateMaxAbs)).Append(',').Append(F(e.localRollRateMeanAbs)).Append(',')
            .Append(F(e.localRollRateRms)).Append(',').Append(F(e.localRollRateSignedIntegral)).Append(',')
            .Append(F(e.localRollRateAbsoluteIntegral)).Append(',').Append(F(e.pitchMaxAbs)).Append(',')
            .Append(F(e.pitchMeanAbs)).Append(',').Append(F(e.pitchRms)).Append(',')
            .Append(F(e.pitchSignedIntegral)).Append(',').Append(F(e.pitchAbsoluteIntegral)).Append(',')
            .Append(F(e.pitchQ1)).Append(',').Append(F(e.pitchMedian)).Append(',')
            .Append(F(e.pitchQ3)).Append(',').Append(F(e.pitchIqr)).Append(',')
            .Append(F(e.initialVelocityDirectionAngle)).Append(',').Append(e.initialVelocityLineRelation).Append(',')
            .Append(e.initialVelocityEncounterType).Append(',').Append(F(e.initialVelocityLineClosestDistance)).Append(',')
            .Append(F(e.initialNormalizedVelocityLineSeparation)).Append(',').Append(F(e.initialNormalizedVelocityTripleProduct)).Append(',')
            .Append(F(e.initialTimeToClosestApproach)).Append(',').Append(F(e.initialPredictedClosestApproachDistance)).Append(',')
            .Append(F(e.finalVelocityDirectionAngle)).Append(',').Append(e.finalVelocityLineRelation).Append(',')
            .Append(e.finalVelocityEncounterType).Append(',').Append(F(e.finalVelocityLineClosestDistance)).Append(',')
            .Append(F(e.finalNormalizedVelocityLineSeparation)).Append(',').Append(F(e.finalNormalizedVelocityTripleProduct)).Append(',')
            .Append(F(e.finalTimeToClosestApproach)).Append(',').Append(F(e.finalPredictedClosestApproachDistance)).AppendLine();
    }

    public static string GeneratePointCsv(AirCombatBehaviorServer.TurnPlaneCorrectionEpisode episode)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(PointCsvHeader);
        if (episode.points == null) return builder.ToString();
        foreach (AirCombatBehaviorServer.TurnPlaneCorrectionPoint p in episode.points)
            builder.Append(F(p.timeFromStart, "F3")).Append(',').Append(F(p.pitchInput)).Append(',')
                .Append(F(p.rollInput)).Append(',').Append(F(p.yawInput)).Append(',')
                .Append(F(p.signedAzimuthError)).Append(',').Append(F(p.signedElevationError)).Append(',')
                .Append(F(p.totalRelativeAngle)).Append(',').Append(F(p.bankAngle)).Append(',')
                .Append(F(p.localAngularVelocity.x)).Append(',').Append(F(p.localAngularVelocity.y)).Append(',')
                .Append(F(p.localAngularVelocity.z)).Append(',').Append(F(p.distance)).Append(',')
                .Append(F(p.closureRate)).AppendLine();
        return builder.ToString();
    }

    static string F(float value, string format = "F4")
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            value = 0f;
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
