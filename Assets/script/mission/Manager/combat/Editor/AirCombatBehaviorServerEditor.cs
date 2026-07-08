using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AirCombatBehaviorServer), true)]
public class AirCombatBehaviorServerEditor : Editor
{
    const float GraphHeight = 180f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        AirCombatBehaviorServer recorder = (AirCombatBehaviorServer)target;

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Clear Samples"))
        {
            Undo.RecordObject(recorder, "Clear Air Combat Samples");
            recorder.ClearSamples();
            EditorUtility.SetDirty(recorder);
        }

        DrawVisualizations(recorder);
    }

    public static void DrawVisualizations(AirCombatBehaviorServer recorder)
    {
        if (recorder == null)
            return;

        EditorGUILayout.Space(8f);
        DrawLatestTurnPlaneCorrectionSummary(recorder);

        EditorGUILayout.Space(8f);
        DrawPlayerNoseToEnemyAngleHistogram(
            "Player Nose Angle Histogram",
            recorder,
            new Color(0.7f, 0.55f, 1f));

        EditorGUILayout.Space(8f);
        DrawRelativeDirectionRadar(
            "Relative Direction Radar",
            recorder,
            new Color(0.25f, 0.8f, 1f),
            new Color(1f, 0.85f, 0.25f));
    }

    static void DrawLatestTurnPlaneCorrectionSummary(AirCombatBehaviorServer recorder)
    {
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes = recorder.TurnPlaneCorrectionEpisodes;
        EditorGUILayout.LabelField("Turn Plane Correction Episodes", (episodes != null ? episodes.Count : 0).ToString(), EditorStyles.boldLabel);
        if (episodes == null || episodes.Count == 0)
            return;

        AirCombatBehaviorServer.TurnPlaneCorrectionEpisode latest = episodes[episodes.Count - 1];
        EditorGUILayout.LabelField("Duration", latest.duration.ToString("F2"));
        EditorGUILayout.LabelField("Initial / Min / Final Azimuth", $"{latest.initialAzimuth:F1} / {latest.minimumAzimuthAbs:F1} / {latest.finalAzimuth:F1}");
        EditorGUILayout.LabelField("Roll Stops / Roll Rev / Yaw Rev", $"{latest.rollStopCount} / {latest.rollReverseCount} / {latest.yawReverseCount}");
        EditorGUILayout.LabelField("Zero Cross / Overshoot", $"{latest.azimuthZeroCrossCount} / {latest.overshootCount}");
        EditorGUILayout.LabelField("Correction Efficiency", latest.correctionEfficiency.ToString("F2"));
        EditorGUILayout.LabelField("Reaction / Release Delay", $"{latest.reactionDelay:F2} / {latest.releaseDelay:F2}");
    }

    static void DrawPlayerNoseToEnemyAngleHistogram(string title, AirCombatBehaviorServer recorder, Color barColor)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(10f, GraphHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.09f, 0.09f, 0.09f));
        DrawGrid(rect);
        DrawAxes(rect);

        IReadOnlyList<int> bins = recorder.NoseAngleHistogramBins;
        if (bins == null || bins.Count == 0)
        {
            DrawGraphLabels(rect, 0f, 180f, 0f, 1f, "F0");
            return;
        }

        int maxCount = 1;
        for (int i = 0; i < bins.Count; i++)
            maxCount = Mathf.Max(maxCount, bins[i]);

        float binWidth = rect.width / bins.Count;
        for (int i = 0; i < bins.Count; i++)
        {
            float height = Mathf.Lerp(0f, rect.height - 18f, bins[i] / (float)maxCount);
            Rect bar = new Rect(rect.xMin + i * binWidth + 1f, rect.yMax - height, Mathf.Max(1f, binWidth - 2f), height);
            EditorGUI.DrawRect(bar, new Color(barColor.r, barColor.g, barColor.b, 0.75f));
        }

        DrawGraphLabels(rect, 0f, 180f, 0f, maxCount, "F0");
    }

    static void DrawRelativeDirectionRadar(string title, AirCombatBehaviorServer recorder, Color timeColor, Color angleColor)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(10f, GraphHeight + 36f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.09f, 0.09f, 0.09f));

        IReadOnlyList<float> seconds = recorder.RelativeDirectionSectorSeconds;
        IReadOnlyList<float> angleSums = recorder.RelativeDirectionSectorAngleSums;
        Rect radarRect = new Rect(rect.xMin + 12f, rect.yMin + 8f, rect.width - 24f, GraphHeight - 8f);
        DrawRadarGrid(radarRect);
        DrawRadarPolygon(radarRect, seconds, GetMaxValue(seconds), timeColor, 2.5f, 0.22f);

        float[] averageAngles = new float[8];
        for (int i = 0; i < averageAngles.Length; i++)
            averageAngles[i] = seconds != null && angleSums != null && seconds.Count > i && angleSums.Count > i && seconds[i] > 0.0001f
                ? angleSums[i] / seconds[i]
                : 0f;

        DrawRadarPolygon(radarRect, averageAngles, 180f, angleColor, 1.8f, 0.08f);
        DrawRadarDirectionLabels(radarRect);
        DrawRadarLegend(rect, timeColor, angleColor);
    }

    static void DrawRadarGrid(Rect rect)
    {
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.42f;
        Handles.color = new Color(1f, 1f, 1f, 0.12f);
        for (int ring = 1; ring <= 4; ring++)
            Handles.DrawWireDisc(center, Vector3.forward, radius * ring / 4f);
        for (int i = 0; i < 8; i++)
            Handles.DrawLine(center, GetRadarPoint(center, radius, i, 1f));
    }

    static void DrawRadarPolygon(Rect rect, IReadOnlyList<float> values, float maxValue, Color color, float lineWidth, float fillAlpha)
    {
        if (values == null || values.Count == 0 || maxValue <= 0.0001f)
            return;

        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.42f;
        Vector3[] points = new Vector3[values.Count + 1];
        for (int i = 0; i < values.Count; i++)
            points[i] = GetRadarPoint(center, radius, i, Mathf.Clamp01(values[i] / maxValue));
        points[values.Count] = points[0];

        Handles.color = new Color(color.r, color.g, color.b, fillAlpha);
        Handles.DrawAAConvexPolygon(points);
        Handles.color = color;
        Handles.DrawAAPolyLine(lineWidth, points);
    }

    static void DrawRadarDirectionLabels(Rect rect)
    {
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.46f;
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        for (int i = 0; i < 8; i++)
        {
            Vector2 point = GetRadarPoint(center, radius, i, 1f);
            GUI.Label(new Rect(point.x - 32f, point.y - 8f, 64f, 16f), GetSectorLabel(i), style);
        }
    }

    static void DrawRadarLegend(Rect rect, Color timeColor, Color angleColor)
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = timeColor;
        GUI.Label(new Rect(rect.xMin + 12f, rect.yMax - 34f, 160f, 16f), "Time", style);
        style.normal.textColor = angleColor;
        GUI.Label(new Rect(rect.xMin + 12f, rect.yMax - 18f, 160f, 16f), "Avg Angle", style);
    }

    static Vector2 GetRadarPoint(Vector2 center, float radius, int sectorIndex, float normalizedValue)
    {
        float angle = sectorIndex * 45f * Mathf.Deg2Rad;
        return center + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * radius * normalizedValue;
    }

    static float GetMaxValue(IReadOnlyList<float> values)
    {
        float max = 0f;
        if (values != null)
            for (int i = 0; i < values.Count; i++)
                max = Mathf.Max(max, values[i]);
        return max;
    }

    static string GetSectorLabel(int sectorIndex)
    {
        string[] labels = { "Right", "UpRight", "Up", "UpLeft", "Left", "DownLeft", "Down", "DownRight" };
        return sectorIndex >= 0 && sectorIndex < labels.Length ? labels[sectorIndex] : "?";
    }

    static void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 4f);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }
    }

    static void DrawAxes(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.35f);
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax));
    }

    static void DrawGraphLabels(Rect rect, float xMin, float xMax, float yMin, float yMax, string format)
    {
        GUIStyle style = EditorStyles.miniLabel;
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 18f, 80f, 16f), xMin.ToString(format), style);
        GUI.Label(new Rect(rect.xMax - 44f, rect.yMax - 18f, 80f, 16f), xMax.ToString(format), style);
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMin + 2f, 80f, 16f), yMax.ToString(format), style);
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 34f, 80f, 16f), yMin.ToString(format), style);
    }
}
