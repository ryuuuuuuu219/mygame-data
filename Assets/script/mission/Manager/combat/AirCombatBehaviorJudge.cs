using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[AddComponentMenu("Air Combat/Air Combat Behavior Judge")]
public class AirCombatBehaviorJudge : MonoBehaviour
{
    [SerializeField] AirCombatBehaviorServer server;
    [SerializeField, Min(1)] int minimumEpisodes = 3;
    [SerializeField] float lowEfficiencyThreshold = 0.5f;
    [SerializeField] float highOvershootAverage = 1f;
    [SerializeField] float highReverseAverage = 2f;
    [TextArea(4, 12), SerializeField] string latestResult;

    public string LatestResult => latestResult;

    void Reset()
    {
        server = GetComponent<AirCombatBehaviorServer>();
    }

    [ContextMenu("Evaluate")]
    public string Evaluate()
    {
        latestResult = Evaluate(server != null ? server.TurnPlaneCorrectionEpisodes : null);
        return latestResult;
    }

    public string Evaluate(
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes)
    {
        int total = episodes?.Count ?? 0;
        List<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> valid = new();
        if (episodes != null)
            for (int i = 0; i < episodes.Count; i++)
                if (episodes[i].isValidForSummary) valid.Add(episodes[i]);
        if (valid.Count < minimumEpisodes)
            return $"判定保留: 有効エピソード不足 ({valid.Count}/{minimumEpisodes}) / 全{total}件";

        int fine = 0, large = 0, rear = 0, overshoot = 0, reverse = 0, stops = 0, releases = 0;
        List<float> fineEfficiency = new(), largeEfficiency = new(), reaction = new();
        foreach (var e in valid)
        {
            if (e.correctionClass == TurnPlaneCorrectionClass.Fine) { fine++; fineEfficiency.Add(Finite(e.correctionEfficiency)); }
            else if (e.correctionClass == TurnPlaneCorrectionClass.Large) { large++; largeEfficiency.Add(Finite(e.correctionEfficiency)); }
            else rear++;
            if (e.overshootCount > 0) overshoot++;
            if (e.rollReverseCount > 0) reverse++;
            if (e.intermediateRollStopCount > 0) stops++;
            if (e.terminalRollRelease) releases++;
            reaction.Add(Mathf.Max(0f, Finite(e.reactionDelay)));
        }
        RobustStats fineStats = Stats(fineEfficiency), largeStats = Stats(largeEfficiency), reactionStats = Stats(reaction);
        StringBuilder result = new StringBuilder();
        result.AppendFormat(CultureInfo.InvariantCulture,
            "全Episode {0} / 有効 {1} / 無効 {2}\nFine {3} / Large {4} / Rear {5} (RearはEfficiency総合評価から除外)\n",
            total, valid.Count, total - valid.Count, fine, large, rear);
        result.AppendFormat(CultureInfo.InvariantCulture,
            "Fine Efficiency Median {0:F3} / IQR {1:F3}\nLarge Efficiency Median {2:F3} / IQR {3:F3}\n",
            fineStats.median, fineStats.iqr, largeStats.median, largeStats.iqr);
        result.AppendFormat(
            CultureInfo.InvariantCulture,
            "Overshoot発生率 {0:P1} / RollReverse発生率 {1:P1} / IntermediateStop発生率 {2:P1} / TerminalRelease率 {3:P1}\nReactionDelay Median {4:F3}秒 / IQR {5:F3}秒",
            (float)overshoot / valid.Count, (float)reverse / valid.Count, (float)stops / valid.Count,
            (float)releases / valid.Count, reactionStats.median, reactionStats.iqr);
        return result.ToString();
    }

    static RobustStats Stats(List<float> values)
    {
        RobustStats result = new RobustStats { count = values.Count };
        if (values.Count == 0) return result;
        values.Sort();
        result.q1 = Percentile(values, 0.25f);
        result.median = Percentile(values, 0.5f);
        result.q3 = Percentile(values, 0.75f);
        result.iqr = result.q3 - result.q1;
        return result;
    }

    static float Percentile(List<float> values, float p)
    {
        float index = (values.Count - 1) * p;
        int lo = Mathf.FloorToInt(index), hi = Mathf.CeilToInt(index);
        return Mathf.Lerp(values[lo], values[hi], index - lo);
    }

    static float Finite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
