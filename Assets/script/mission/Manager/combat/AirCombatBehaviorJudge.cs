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
        int count = episodes?.Count ?? 0;
        if (count < minimumEpisodes)
            return $"判定保留: エピソード不足 ({count}/{minimumEpisodes})";

        float efficiency = 0f;
        float overshoots = 0f;
        float reverses = 0f;
        float reactionDelay = 0f;
        for (int i = 0; i < count; i++)
        {
            efficiency += Finite(episodes[i].correctionEfficiency);
            overshoots += episodes[i].overshootCount;
            reverses += episodes[i].rollReverseCount;
            reactionDelay += Mathf.Max(0f, Finite(episodes[i].reactionDelay));
        }

        efficiency /= count;
        overshoots /= count;
        reverses /= count;
        reactionDelay /= count;

        StringBuilder result = new StringBuilder();
        result.Append("旋回面補正: ");
        if (efficiency < lowEfficiencyThreshold)
            result.Append("補正効率が低い傾向");
        else if (overshoots >= highOvershootAverage)
            result.Append("オーバーシュートが多い傾向");
        else if (reverses >= highReverseAverage)
            result.Append("ロール反転が多い傾向");
        else
            result.Append("安定");

        result.AppendLine().AppendFormat(
            CultureInfo.InvariantCulture,
            "対象 {0}件 / 平均効率 {1:F3} / 平均オーバーシュート {2:F2} / 平均ロール反転 {3:F2} / 平均反応遅延 {4:F3}秒",
            count, efficiency, overshoots, reverses, reactionDelay);
        return result.ToString();
    }

    static float Finite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
