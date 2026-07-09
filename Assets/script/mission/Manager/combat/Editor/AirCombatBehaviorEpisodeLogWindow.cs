using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AirCombatBehaviorEpisodeLogWindow : EditorWindow
{
    const string CsvHeader = AirCombatBehaviorLogGenerator.CsvHeader;

    AirCombatBehaviorServer recorder;
    string csvText = CsvHeader;
    Vector2 summaryScrollPosition;
    Vector2 csvScrollPosition;
    int startIndex;
    int endIndex;
    int selectedEpisodeIndex;
    bool autoRefresh;
    int cachedEpisodeCount = -1;
    double nextAutoRefreshTime;
    GUIStyle csvStyle;

    [MenuItem("Tools/Air Combat/Correction Episode Log")]
    static void OpenWindow()
    {
        GetWindow<AirCombatBehaviorEpisodeLogWindow>("Air Combat Episode Log");
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        RefreshCsv();
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (!autoRefresh || EditorApplication.timeSinceStartup < nextAutoRefreshTime)
            return;

        nextAutoRefreshTime = EditorApplication.timeSinceStartup + 0.5d;
        int episodeCount = GetEpisodes()?.Count ?? 0;
        if (episodeCount != cachedEpisodeCount)
        {
            RefreshCsv();
            Repaint();
        }
    }

    void OnGUI()
    {
        EnsureStyles();
        DrawRecorderSelection();
        EditorGUILayout.Space();
        DrawStatistics();
        EditorGUILayout.Space();
        DrawActions();
        EditorGUILayout.Space();
        DrawEpisodeSummary();
        EditorGUILayout.Space();
        DrawCsvText();
    }

    void DrawRecorderSelection()
    {
        EditorGUILayout.LabelField("Recorder", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        recorder = (AirCombatBehaviorServer)EditorGUILayout.ObjectField(
            recorder, typeof(AirCombatBehaviorServer), true);
        if (EditorGUI.EndChangeCheck())
            RefreshCsv();

        if (GUILayout.Button("Find Recorder"))
        {
            recorder = FindObjectOfType<AirCombatBehaviorServer>();
            RefreshCsv();
            if (recorder == null)
                ShowNotification(new GUIContent("Recorder not found"));
        }

        if (recorder == null)
            EditorGUILayout.HelpBox("Select an AirCombatBehaviorServer or use Find Server.", MessageType.Warning);
    }

    void DrawStatistics()
    {
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes = GetEpisodes();
        int episodeCount = episodes?.Count ?? 0;

        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Episode Count", episodeCount.ToString(CultureInfo.InvariantCulture));
        EditorGUILayout.LabelField("CSV Character Count", csvText.Length.ToString(CultureInfo.InvariantCulture));
        EditorGUILayout.LabelField("CSV Line Count", (episodeCount + 1).ToString(CultureInfo.InvariantCulture));
        if (episodeCount > 0)
        {
            EditorGUILayout.LabelField("First Episode Start Time", Format(episodes[0].startTime, "F3"));
            EditorGUILayout.LabelField("Last Episode End Time", Format(episodes[episodeCount - 1].endTime, "F3"));
        }
    }

    void DrawActions()
    {
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes = GetEpisodes();
        int episodeCount = episodes?.Count ?? 0;

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
            RefreshCsv();
        if (GUILayout.Button("Copy All"))
        {
            RefreshCsv();
            EditorGUIUtility.systemCopyBuffer = csvText;
            ShowNotification(new GUIContent("CSV copied"));
        }
        if (GUILayout.Button("Copy Header"))
        {
            EditorGUIUtility.systemCopyBuffer = CsvHeader;
            ShowNotification(new GUIContent("Header copied"));
        }
        if (GUILayout.Button("Save CSV"))
            SaveCsv();
        EditorGUILayout.EndHorizontal();

        startIndex = EditorGUILayout.IntField("Start Index", startIndex);
        endIndex = EditorGUILayout.IntField("End Index", endIndex);
        using (new EditorGUI.DisabledScope(episodeCount == 0))
        {
            if (GUILayout.Button("Copy Selected Range"))
                CopySelectedRange(episodes);
            selectedEpisodeIndex = EditorGUILayout.IntSlider("Selected Episode", selectedEpisodeIndex, 0, Mathf.Max(0, episodeCount - 1));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Selected Episode Points"))
            {
                EditorGUIUtility.systemCopyBuffer = AirCombatBehaviorLogGenerator.GeneratePointCsv(episodes[selectedEpisodeIndex]);
                ShowNotification(new GUIContent("Point CSV copied"));
            }
            if (GUILayout.Button("Save Selected Episode Points"))
                SavePointCsv(episodes[selectedEpisodeIndex]);
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawEpisodeSummary()
    {
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes = GetEpisodes();
        int episodeCount = episodes?.Count ?? 0;

        EditorGUILayout.LabelField("Episode Overview", EditorStyles.boldLabel);
        summaryScrollPosition = EditorGUILayout.BeginScrollView(summaryScrollPosition, GUILayout.Height(150f));
        EditorGUILayout.LabelField(
            "Index Valid Class EndReason Duration InitialAz MinimumAz FinalAz Efficiency Wrap ZeroCross Terminal",
            EditorStyles.miniBoldLabel);
        for (int i = 0; i < episodeCount; i++)
        {
            AirCombatBehaviorServer.TurnPlaneCorrectionEpisode episode = episodes[i];
            EditorGUILayout.LabelField(string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} {3} {4:F3} {5:F2} {6:F2} {7:F2} {8:F3} {9} {10} {11}",
                i, episode.isValidForSummary, episode.correctionClass, episode.endReason,
                SanitizeFloat(episode.duration),
                SanitizeFloat(episode.initialAzimuth),
                SanitizeFloat(episode.minimumAzimuthAbs),
                SanitizeFloat(episode.finalAzimuth),
                SanitizeFloat(episode.correctionEfficiency),
                episode.azimuthWrapCount, episode.azimuthZeroCrossCount, episode.terminalRollRelease));
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawCsvText()
    {
        EditorGUILayout.LabelField("CSV", EditorStyles.boldLabel);
        csvScrollPosition = EditorGUILayout.BeginScrollView(
            csvScrollPosition, true, true, GUILayout.ExpandHeight(true));
        Vector2 size = csvStyle.CalcSize(new GUIContent(csvText));
        EditorGUILayout.SelectableLabel(
            csvText,
            csvStyle,
            GUILayout.Width(Mathf.Max(position.width - 35f, size.x + 10f)),
            GUILayout.Height(Mathf.Max(100f, size.y + 10f)));
        EditorGUILayout.EndScrollView();
    }

    void EnsureStyles()
    {
        if (csvStyle != null)
            return;

        csvStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = false,
            richText = false
        };
    }

    void RefreshCsv()
    {
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes = GetEpisodes();
        csvText = BuildCsv(episodes);
        cachedEpisodeCount = episodes?.Count ?? 0;
        if (cachedEpisodeCount == 0)
        {
            startIndex = 0;
            endIndex = 0;
        }
        else
        {
            startIndex = Mathf.Clamp(startIndex, 0, cachedEpisodeCount - 1);
            endIndex = Mathf.Clamp(endIndex, startIndex, cachedEpisodeCount - 1);
        }
    }

    void CopySelectedRange(IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes)
    {
        int count = episodes?.Count ?? 0;
        if (count == 0)
            return;

        int safeStart = Mathf.Clamp(startIndex, 0, count - 1);
        int safeEnd = Mathf.Clamp(endIndex, 0, count - 1);
        if (safeStart > safeEnd)
        {
            int swap = safeStart;
            safeStart = safeEnd;
            safeEnd = swap;
        }

        startIndex = safeStart;
        endIndex = safeEnd;
        EditorGUIUtility.systemCopyBuffer =
            AirCombatBehaviorLogGenerator.GenerateCsv(episodes, safeStart, safeEnd);
        ShowNotification(new GUIContent("Range copied"));
    }

    void SaveCsv()
    {
        RefreshCsv();
        string path = EditorUtility.SaveFilePanel(
            "Save correction episode CSV",
            string.Empty,
            "AirCombatCorrectionEpisodes.csv",
            "csv");
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            File.WriteAllText(path, csvText, new UTF8Encoding(false));
            ShowNotification(new GUIContent("CSV saved"));
        }
        catch (IOException exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Save CSV", "Failed to save the CSV file.", "OK");
        }
    }

    void SavePointCsv(AirCombatBehaviorServer.TurnPlaneCorrectionEpisode episode)
    {
        string path = EditorUtility.SaveFilePanel("Save episode point CSV", string.Empty,
            "AirCombatCorrectionEpisodePoints.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, AirCombatBehaviorLogGenerator.GeneratePointCsv(episode), new UTF8Encoding(false));
        ShowNotification(new GUIContent("Point CSV saved"));
    }

    IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> GetEpisodes()
    {
        return recorder != null ? recorder.TurnPlaneCorrectionEpisodes : null;
    }

    static string BuildCsv(
        IReadOnlyList<AirCombatBehaviorServer.TurnPlaneCorrectionEpisode> episodes)
    {
        return AirCombatBehaviorLogGenerator.GenerateCsv(episodes);
    }

    static void AppendEpisodeCsvRow(
        StringBuilder builder,
        int index,
        AirCombatBehaviorServer.TurnPlaneCorrectionEpisode episode)
    {
        builder.Append(index).Append(',')
            .Append(Format(episode.startTime, "F3")).Append(',')
            .Append(Format(episode.endTime, "F3")).Append(',')
            .Append(Format(episode.duration, "F3")).Append(',')
            .Append(Format(episode.initialAzimuth, "F3")).Append(',')
            .Append(episode.initialAzimuthSign).Append(',')
            .Append(Format(episode.minimumAzimuthAbs, "F3")).Append(',')
            .Append(Format(episode.finalAzimuth, "F3")).Append(',')
            .Append(Format(episode.minimumRelativeAngle, "F3")).Append(',')
            .Append(Format(episode.timeToMinimumRelativeAngle, "F3")).Append(',')
            .Append(episode.rollStopCount).Append(',')
            .Append(episode.rollReverseCount).Append(',')
            .Append(episode.yawReverseCount).Append(',')
            .Append(episode.azimuthZeroCrossCount).Append(',')
            .Append(episode.overshootCount).Append(',')
            .Append(Format(episode.rollMaxAbs, "F4")).Append(',')
            .Append(Format(episode.rollMeanAbs, "F4")).Append(',')
            .Append(Format(episode.rollRms, "F4")).Append(',')
            .Append(Format(episode.rollSignedIntegral, "F4")).Append(',')
            .Append(Format(episode.rollAbsoluteIntegral, "F4")).Append(',')
            .Append(Format(episode.rollQ1, "F4")).Append(',')
            .Append(Format(episode.rollMedian, "F4")).Append(',')
            .Append(Format(episode.rollQ3, "F4")).Append(',')
            .Append(Format(episode.rollIqr, "F4")).Append(',')
            .Append(Format(episode.yawMaxAbs, "F4")).Append(',')
            .Append(Format(episode.yawMeanAbs, "F4")).Append(',')
            .Append(Format(episode.yawRms, "F4")).Append(',')
            .Append(Format(episode.yawSignedIntegral, "F4")).Append(',')
            .Append(Format(episode.yawAbsoluteIntegral, "F4")).Append(',')
            .Append(Format(episode.yawQ1, "F4")).Append(',')
            .Append(Format(episode.yawMedian, "F4")).Append(',')
            .Append(Format(episode.yawQ3, "F4")).Append(',')
            .Append(Format(episode.yawIqr, "F4")).Append(',')
            .Append(Format(episode.yawRollEffortRatio, "F4")).Append(',')
            .Append(Format(episode.yawRollRmsRatio, "F4")).Append(',')
            .Append(Format(episode.correctionEfficiency, "F4")).Append(',')
            .Append(Format(episode.reactionDelay, "F3")).Append(',')
            .Append(Format(episode.releaseDelay, "F3")).Append(',')
            .Append(Format(episode.postMinimumInputDuration, "F3")).Append(',')
            .Append(Format(episode.averageReversePeriod, "F3")).Append(',')
            .Append(Format(episode.minReversePeriod, "F3")).Append(',')
            .Append(Format(episode.averageHysteresisWidth, "F3")).Append(',')
            .Append(Format(episode.averageReverseImpulseRatio, "F4")).Append(',')
            .Append(Format(episode.averageReversePeakRatio, "F4")).Append(',')
            .Append(Format(episode.averageReverseDurationRatio, "F4")).Append(',')
            .Append(Format(episode.rollBestLagSeconds, "F3")).Append(',')
            .Append(Format(episode.rollBestLagCorrelation, "F4")).Append(',')
            .Append(Format(episode.yawBestLagSeconds, "F3")).Append(',')
            .Append(Format(episode.yawBestLagCorrelation, "F4")).Append(',')
            .Append(episode.points?.Count ?? 0).Append(',')
            .Append(episode.rollEvents?.Count ?? 0)
            .AppendLine();
    }

    static string Format(float value, string format)
    {
        return SanitizeFloat(value).ToString(format, CultureInfo.InvariantCulture);
    }

    static float SanitizeFloat(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
