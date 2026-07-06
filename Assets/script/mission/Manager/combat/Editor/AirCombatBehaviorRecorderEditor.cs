using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AirCombatBehaviorRecorder))]
public class AirCombatBehaviorRecorderEditor : Editor
{
    const float GraphHeight = 180f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AirCombatBehaviorRecorder recorder = (AirCombatBehaviorRecorder)target;

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Samples"))
            {
                Undo.RecordObject(recorder, "Clear Air Combat Samples");
                recorder.ClearSamples();
                EditorUtility.SetDirty(recorder);
            }

            if (GUILayout.Button("Rebuild Bins"))
            {
                Undo.RecordObject(recorder, "Rebuild Air Combat Graph Bins");
                recorder.RebuildGraphBins();
                EditorUtility.SetDirty(recorder);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            "Roll Correction Correlation",
            recorder.RollCorrectionCorrelation.ToString("F3"),
            EditorStyles.boldLabel);

        DrawRollCorrectionGraph(
            "Roll Start Pitch Axis Angle -> Roll Stop Pitch Axis Angle",
            recorder,
            new Color(0.95f, 0.55f, 0.25f),
            new Color(1f, 0.85f, 0.25f));

        EditorGUILayout.Space(8f);
        DrawGraph(
            "Pitch Axis Angle -> Roll/Pitch Input Ratio",
            recorder,
            sample => sample.rollPitchInputRatio,
            bin => bin.averageRollPitchInputRatio,
            bin => bin.lowPitchSampleCount,
            new Color(0.25f, 0.65f, 1f),
            new Color(1f, 0.8f, 0.25f),
            0f,
            GetMaxSampleValue(recorder.LowPitchAxisSamples, sample => sample.rollPitchInputRatio, 1f));

        EditorGUILayout.Space(8f);
        DrawGraph(
            "Pitch Axis Angle -> Axis Precision",
            recorder,
            sample => sample.axisPrecision,
            bin => bin.averageAxisPrecision,
            bin => bin.lowPitchSampleCount,
            new Color(0.35f, 0.9f, 0.55f),
            new Color(1f, 0.8f, 0.25f),
            0f,
            1f);
    }

    static void DrawGraph(
        string title,
        AirCombatBehaviorRecorder recorder,
        System.Func<AirCombatBehaviorRecorder.LowPitchAxisSample, float> sampleYSelector,
        System.Func<AirCombatBehaviorRecorder.GraphBin, float> binYSelector,
        System.Func<AirCombatBehaviorRecorder.GraphBin, int> binCountSelector,
        Color pointColor,
        Color curveColor,
        float yMin,
        float yMax)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, GraphHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.09f, 0.09f, 0.09f));

        DrawGrid(rect);
        DrawAxes(rect);
        DrawSamples(rect, recorder, sampleYSelector, pointColor, yMin, yMax);
        DrawBinCurve(rect, recorder, binYSelector, binCountSelector, curveColor, yMin, yMax);
        DrawGraphLabels(rect, recorder.MinAngle, recorder.MaxAngle, yMin, yMax);
    }

    static void DrawRollCorrectionGraph(
        string title,
        AirCombatBehaviorRecorder recorder,
        Color pointColor,
        Color curveColor)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, GraphHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.09f, 0.09f, 0.09f));

        DrawGrid(rect);
        DrawAxes(rect);
        DrawRollCorrectionSamples(rect, recorder, pointColor);
        DrawBinCurve(
            rect,
            recorder,
            bin => bin.averageRollEndPitchAxisAngle,
            bin => bin.rollCorrectionSampleCount,
            curveColor,
            recorder.MinAngle,
            recorder.MaxAngle);
        DrawGraphLabels(rect, recorder.MinAngle, recorder.MaxAngle, recorder.MinAngle, recorder.MaxAngle);
    }

    static void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / 4f);
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));

            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }
    }

    static void DrawAxes(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.35f);
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax));
    }

    static void DrawSamples(
        Rect rect,
        AirCombatBehaviorRecorder recorder,
        System.Func<AirCombatBehaviorRecorder.LowPitchAxisSample, float> sampleYSelector,
        Color color,
        float yMin,
        float yMax)
    {
        IReadOnlyList<AirCombatBehaviorRecorder.LowPitchAxisSample> samples = recorder.LowPitchAxisSamples;
        if (samples == null || samples.Count == 0)
            return;

        Handles.color = color;
        for (int i = 0; i < samples.Count; i++)
        {
            Vector2 point = ToGraphPoint(
                rect,
                samples[i].pitchAxisToEnemyAngle,
                sampleYSelector(samples[i]),
                recorder.MinAngle,
                recorder.MaxAngle,
                yMin,
                yMax);

            Handles.DrawSolidDisc(point, Vector3.forward, 2f);
        }
    }

    static void DrawBinCurve(
        Rect rect,
        AirCombatBehaviorRecorder recorder,
        System.Func<AirCombatBehaviorRecorder.GraphBin, float> binYSelector,
        System.Func<AirCombatBehaviorRecorder.GraphBin, int> binCountSelector,
        Color color,
        float yMin,
        float yMax)
    {
        IReadOnlyList<AirCombatBehaviorRecorder.GraphBin> bins = recorder.GraphBins;
        if (bins == null || bins.Count == 0)
            return;

        List<Vector3> points = new();
        for (int i = 0; i < bins.Count; i++)
        {
            AirCombatBehaviorRecorder.GraphBin bin = bins[i];
            if (binCountSelector(bin) <= 0)
                continue;

            points.Add(ToGraphPoint(
                rect,
                bin.centerAngle,
                binYSelector(bin),
                recorder.MinAngle,
                recorder.MaxAngle,
                yMin,
                yMax));
        }

        if (points.Count < 2)
            return;

        Handles.color = color;
        Handles.DrawAAPolyLine(2.5f, points.ToArray());
    }

    static void DrawRollCorrectionSamples(
        Rect rect,
        AirCombatBehaviorRecorder recorder,
        Color color)
    {
        IReadOnlyList<AirCombatBehaviorRecorder.RollCorrectionSample> samples = recorder.RollCorrectionSamples;
        if (samples == null || samples.Count == 0)
            return;

        Handles.color = color;
        for (int i = 0; i < samples.Count; i++)
        {
            Vector2 point = ToGraphPoint(
                rect,
                samples[i].rollStartPitchAxisAngle,
                samples[i].rollEndPitchAxisAngle,
                recorder.MinAngle,
                recorder.MaxAngle,
                recorder.MinAngle,
                recorder.MaxAngle);

            Handles.DrawSolidDisc(point, Vector3.forward, 2f);
        }
    }

    static void DrawGraphLabels(Rect rect, float xMin, float xMax, float yMin, float yMax)
    {
        GUIStyle labelStyle = EditorStyles.miniLabel;
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 18f, 80f, 16f), xMin.ToString("F0"), labelStyle);
        GUI.Label(new Rect(rect.xMax - 44f, rect.yMax - 18f, 80f, 16f), xMax.ToString("F0"), labelStyle);
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMin + 2f, 80f, 16f), yMax.ToString("F2"), labelStyle);
        GUI.Label(new Rect(rect.xMin + 4f, rect.yMax - 34f, 80f, 16f), yMin.ToString("F2"), labelStyle);
    }

    static Vector2 ToGraphPoint(
        Rect rect,
        float x,
        float y,
        float xMin,
        float xMax,
        float yMin,
        float yMax)
    {
        float x01 = Mathf.InverseLerp(xMin, xMax, x);
        float y01 = Mathf.InverseLerp(yMin, Mathf.Max(yMin + 0.001f, yMax), y);
        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, x01),
            Mathf.Lerp(rect.yMax, rect.yMin, y01));
    }

    static float GetMaxSampleValue(
        IReadOnlyList<AirCombatBehaviorRecorder.LowPitchAxisSample> samples,
        System.Func<AirCombatBehaviorRecorder.LowPitchAxisSample, float> selector,
        float fallback)
    {
        if (samples == null || samples.Count == 0)
            return fallback;

        float max = fallback;
        for (int i = 0; i < samples.Count; i++)
            max = Mathf.Max(max, selector(samples[i]));

        return max;
    }
}
