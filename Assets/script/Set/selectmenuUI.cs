using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class selectmenuUI : MonoBehaviour
{
    public TextMeshProUGUI hudText;          // HUDテキスト
    public List<string> stage_name = new List<string>();

    public int selectedstage;

    StageRoot stageRoot;
    readonly Dictionary<string, string> briefingTextCache = new Dictionary<string, string>();


    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        RemoveUnavailableStages();
        if (SceneManager.GetActiveScene().name == "Briefing")
        {
            selectedstage = PlayerPrefs.GetInt("selectedstage", 0);
        }

        selectedstage = ClampStageIndex(selectedstage);
        PlayerPrefs.SetInt("selectedstage", selectedstage);
    }

    bool maruflag = false;
    bool batsuflag = false;

    float interval = 0f;
    float holdtime = 0.5f;

    // Update is called once per frame
    void Update()
    {
        var keyInput = InputManager.Instance;
        if(keyInput == null)
        {
            return;
        }

        if (keyInput != null)
        {
            if(keyInput.cancel)//✕ボタン押下時
            {
                if (!batsuflag)
                {
                    batsuflag = true;
                    if (SceneManager.GetActiveScene().name == "Menu")
                    {
                        SceneManager.LoadScene("Title");
                    }
                    else if (SceneManager.GetActiveScene().name == "Briefing")
                    {
                        SceneManager.LoadScene("Menu");
                    }
                }
            }
            else
            {
                batsuflag = false;
            }

            if (keyInput.submit)//〇ボタン押下時
            {
                if (!maruflag)
                {
                    maruflag = true;
                    if (SceneManager.GetActiveScene().name == "Briefing")
                    {
                        PlayerPrefs.SetInt("selectedstage", selectedstage);

                        PlayerPrefs.Save();

                        SceneManager.LoadScene("SetUp");
                    }
                    else if (SceneManager.GetActiveScene().name == "Menu")
                    {
                        PlayerPrefs.SetInt("selectedstage", selectedstage);

                        PlayerPrefs.Save();

                        SceneManager.LoadScene("Briefing");
                    }
                    else if (SceneManager.GetActiveScene().name == "Title")
                    {
                        SceneManager.LoadScene("Menu");
                    }
                }
            }
            else
            {
                maruflag = false;
            }
        }
        float v = keyInput.verticalL;//Lスティック上下取得→十字キーを優先
        if(keyInput.up)//十字キー上取得
        {
            v = 1f;
        }
        else if (keyInput.down)//十字キー下取得
        {
            v = -1f;
        }
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            hudText.text = "ミッション選択\n\n";

            for (int i = 0; i < stage_name.Count; i++)
            {
                hudText.text += Line(i, "Stage", stage_name[i]) + "\n";
            }

            hudText.text +=
                "〇 決定\n" +
                "× 戻る";
            if (Mathf.Abs(v) > 0.1f)
            {
                interval -= Time.deltaTime;
                if (interval < 0f)
                {
                    interval = holdtime;
                    StageChange(v);
                }
            }
            else
            {
                interval = 0f;
                holdtime = 0.5f;
            }
        }
        else if (SceneManager.GetActiveScene().name == "Briefing")
        {
            string stageName = GetSelectedStageName();

            hudText.text = "ミッション説明\n\n" +
                "ミッション: " + stageName + "\n\n" +
                BuildMissionDescription(stageName) + "\n\n" +
                "〇 決定\n" +
                "× 戻る";
        }
    }

    readonly Dictionary<string, string> missionText = new Dictionary<string, string>()
    {
        {"M01","対空陣地中央の長射程地対空ミサイルを破壊せよ\n一定高度（900）以上を飛ぶと目標から長距離ミサイルが飛んでくるので低空侵入を推奨する" },
        {"M02","作戦空域内の未確認物体を強行偵察せよ" }
    };

    string GetSelectedStageName()
    {
        if (selectedstage < 0 || selectedstage >= stage_name.Count)
            return "";

        return stage_name[selectedstage];
    }

    string BuildMissionDescription(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
            return "ミッション情報を取得できません。";

        if (briefingTextCache.TryGetValue(stageName, out string cachedText))
            return cachedText;

        string description;
        if (missionText.TryGetValue(stageName, out string authoredText))
        {
            description = authoredText;
        }
        else
        {
            StageData stageData = FindStageData(stageName);
            description = stageData == null
                ? GetFallbackDescription(stageName)
                : GenerateDescription(stageData);
        }

        briefingTextCache[stageName] = description;
        return description;
    }

    StageData FindStageData(string stageName)
    {
        LoadStageRoot();

        if (stageRoot == null || stageRoot.stages == null)
            return null;

        foreach (StageData stage in stageRoot.stages)
        {
            if (stage != null && stage.sceneName == stageName)
                return stage;
        }

        return null;
    }

    void LoadStageRoot()
    {
        if (stageRoot != null)
            return;

        string path = Path.Combine(Application.streamingAssetsPath, "stage_spawns.json");
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path).Trim('\uFEFF', '\u200B', '\u0000', ' ', '\r', '\n', '\t');
        try
        {
            stageRoot = JsonUtility.FromJson<StageRoot>(json);
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError("[selectmenuUI] stage_spawns.json parse error: " + ex.Message);
        }
    }

    string GenerateDescription(StageData stageData)
    {
        int waveCount = 0;
        int enemyCount = 0;
        int targetCount = 0;
        bool hasReinforcements = false;
        Dictionary<string, int> enemyTypeCounts = new Dictionary<string, int>();

        if (stageData.spawns != null)
        {
            waveCount = stageData.spawns.Count;

            foreach (WaveDefinition wave in stageData.spawns)
            {
                if (wave == null)
                    continue;

                wave.Normalize();
                if (wave.requireClearedWaves != null && wave.requireClearedWaves.Count > 0)
                    hasReinforcements = true;

                CountNewFormatEnemies(wave, ref enemyCount, ref targetCount, enemyTypeCounts);
                CountLegacyEnemies(wave, ref enemyCount, ref targetCount, enemyTypeCounts);
            }
        }

        string objective = targetCount > 0
            ? "ミッション目標 " + targetCount + " 体をすべて撃破せよ。"
            : "作戦空域内の敵戦力を掃討せよ。";

        string enemySummary = enemyCount > 0
            ? "敵戦力は " + enemyCount + " 体" + BuildEnemyTypeSummary(enemyTypeCounts) + " と推定される。"
            : "敵戦力は不明。";

        string waveSummary = waveCount > 1
            ? waveCount + " 段階の交戦を想定。" + (hasReinforcements ? "重要目標の撃破後、増援が出現する可能性がある。" : "")
            : "単独の交戦を想定。";

        return objective + "\n" + enemySummary + "\n" + waveSummary;
    }

    void CountNewFormatEnemies(WaveDefinition wave, ref int enemyCount, ref int targetCount, Dictionary<string, int> enemyTypeCounts)
    {
        if (wave.enemies == null)
            return;

        foreach (EnemySpawnDefinition enemy in wave.enemies)
        {
            if (enemy == null)
                continue;

            int count = enemy.placement != null ? Mathf.Max(1, enemy.placement.count) : 1;
            enemyCount += count;
            if (enemy.missionTarget)
                targetCount += count;

            string type = EnemyTypeName(enemy.prefabType);
            AddCount(enemyTypeCounts, type, count);
        }
    }

    string EnemyTypeName(string prefabType)
    {
        switch (prefabType)
        {
            case "AA_GUN":
                return "対空砲";
            case "SAM":
                return "地対空ミサイル";
            case "LASM":
                return "長射程地対空ミサイル";
            default:
                return string.IsNullOrEmpty(prefabType) ? "敵" : prefabType;
        }
    }

    void CountLegacyEnemies(WaveDefinition wave, ref int enemyCount, ref int targetCount, Dictionary<string, int> enemyTypeCounts)
    {
        if (wave.enemyIds == null)
            return;

        for (int i = 0; i < wave.enemyIds.Count; i++)
        {
            enemyCount++;
            if (wave.isMissionTarget != null && i < wave.isMissionTarget.Count && wave.isMissionTarget[i])
                targetCount++;

            AddCount(enemyTypeCounts, "敵", 1);
        }
    }

    void AddCount(Dictionary<string, int> counts, string key, int value)
    {
        if (counts.ContainsKey(key))
            counts[key] += value;
        else
            counts.Add(key, value);
    }

    string BuildEnemyTypeSummary(Dictionary<string, int> enemyTypeCounts)
    {
        if (enemyTypeCounts.Count == 0)
            return "";

        List<string> parts = new List<string>();
        foreach (var pair in enemyTypeCounts)
        {
            parts.Add(pair.Key + " x" + pair.Value);
            if (parts.Count >= 3)
                break;
        }

        return " (" + string.Join(", ", parts) + ")";
    }

    string GetFallbackDescription(string stageName)
    {
        if (missionText.TryGetValue(stageName, out string description))
            return description;

        return "作戦空域内のすべての敵目標を撃破せよ。";
    }

    string Line(int index, string label, string value)
    {
        string head = (selectedstage == index) ? "> " : "  ";
        return head + stage_name[index];
    }


    void StageChange(float value)
    {
        if (stage_name.Count == 0)
            return;

        bool increase = value > 0;
        int numSubjects = stage_name.Count - 1;
        if (increase)
        {
            selectedstage++;
            if (selectedstage > numSubjects) selectedstage = 0;
        }
        else
        {
            selectedstage--;
            if (selectedstage < 0) selectedstage = numSubjects;
        }
    }

    void RemoveUnavailableStages()
    {
        for (int i = stage_name.Count - 1; i >= 0; i--)
        {
            if (!IsSceneAvailable(stage_name[i]))
            {
                Debug.LogWarning("[selectmenuUI] Removed unavailable stage from list: " + stage_name[i]);
                stage_name.RemoveAt(i);
            }
        }
    }

    bool IsSceneAvailable(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") >= 0;
    }

    int ClampStageIndex(int index)
    {
        if (stage_name.Count <= 0)
            return 0;

        return Mathf.Clamp(index, 0, stage_name.Count - 1);
    }
}

   
