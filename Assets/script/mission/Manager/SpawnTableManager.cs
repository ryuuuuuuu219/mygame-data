using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SpawnTableManager : MonoBehaviour
{
    public GameObject Player;
    public List<GameObject> enemies;

    [Tooltip("例: https://ユーザー名.github.io/mygame-data/stage_spawns.json")]
    public string jsonUrl;

    public bool isUseRemoteJSON = false;
    public bool isInit = false;
    public bool isStageClear = false;

    private StageRoot stageRoot;
    private StageData currentStage;

    public int currentWave = 0;

    public class WaveRuntime
    {
        public int waveId;
        public bool started;
        public bool cleared;

        public int aliveEnemy;
        public int aliveTarget;
    }

    public List<(int ID, WaveRuntime rt)> waveRuntime = new();
    public List<WaveDefinition> waveDefinitions = new();



    void Start()
    {
        currentStage = new();

        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.SetActive(false);
        }

        currentWave = -1;
        StartCoroutine(LoadJSON());
    }

    public float toResultTimer = 5f;

    void Update()
    {
        if (currentStage == null) return;
        if (!isInit) return;

        // Wave開始チェック
        foreach (var wave in waveDefinitions)
        {
            var runtime = waveRuntime[wave.waveId];
            if (!runtime.rt.started && CanStartWave(wave))
            {
                StartWave(wave);
            }
        }

        // Wave終了チェック
        foreach (var runtime in waveRuntime)
        {
            if (runtime.rt.started && !runtime.rt.cleared &&
                runtime.rt.aliveTarget <= 0)
            {
                runtime.rt.cleared = true;
            }
        }

        // 全Waveクリア判定
        if (AllWavesCleared())
        {
            FinishStage();
        }

        if (isStageClear)
        {
            toResultTimer -= Time.deltaTime;
            if (toResultTimer < 0f)
                SceneManager.LoadScene("Result");
        }
    }
    void StartWave(WaveDefinition wave)
    {
        var runtime = waveRuntime[wave.waveId];

        runtime.rt.started = true;
        runtime.rt.cleared = false;
        runtime.rt.aliveEnemy = 0;
        runtime.rt.aliveTarget = 0;

        ActivateWave(wave);

        Debug.Log($"[SpawnManager] Wave {wave.waveId} started");
    }


    bool CanStartWave(WaveDefinition wave)
    {
        if (wave.requireClearedWaves == null) return true;

        foreach (int w in wave.requireClearedWaves)
        {
            if (w < 0 || w >= waveRuntime.Count) continue;

            if (!waveRuntime[w].rt.cleared)
                return false;
        }
        return true;
    }

    bool AllWavesCleared()
    {
        foreach (var rt in waveRuntime)
        {
            if (!rt.rt.cleared)
                return false;
        }
        return true;
    }




    #region === JSON Loading and Parsing ===
    IEnumerator LoadJSON()
    {
        // ① まずリモートJSONを試す
        if (!string.IsNullOrEmpty(jsonUrl) && isUseRemoteJSON)
        {
            Debug.Log($"[SpawnTableManager] Try Remote URL: {jsonUrl}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(jsonUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[SpawnTableManager] Loaded JSON from Remote");
                    ParseJSON(request.downloadHandler.text);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[SpawnTableManager] Remote failed: {request.error}");
                }
            }
        }

        // ② フォールバック：StreamingAssets
        string localPath = GetStreamingAssetsPath();
        Debug.Log($"[SpawnTableManager] Try StreamingAssets: {localPath}");

        using (UnityWebRequest request = UnityWebRequest.Get(localPath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SpawnTableManager] StreamingAssets failed: {request.error}");
                yield break;
            }

            Debug.Log("[SpawnTableManager] Loaded JSON from StreamingAssets");
            ParseJSON(request.downloadHandler.text);
        }

    }
    void ParseJSON(string json)
    {
        stageRoot = JsonUtility.FromJson<StageRoot>(json);

        if (stageRoot == null || stageRoot.stages == null)
        {
            Debug.LogError("[SpawnTableManager] JSON parse failed or no stages found");
            return;
        }

        Debug.Log($"[SpawnTableManager] Loaded {stageRoot.stages.Count} stages from JSON");
        foreach (var stage in stageRoot.stages)
        {
            Debug.Log($"[Stage] '{stage.sceneName}'");
        }

        InitializeStage();
    }

    void InitializeStage()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        currentStage = stageRoot.stages.Find(s => s.sceneName == currentScene);

        if (currentStage == null)
        {
            Debug.LogWarning($"No spawn data for scene: {currentScene}");
            return;
        }

        Debug.Log($"[SpawnManager] Initializing stage: {currentStage.sceneName}");
        waveRuntime.Clear();
        waveDefinitions.Clear();
        int i=0;
        foreach (var wave in currentStage.spawns)
        {
            wave.Normalize();

            waveRuntime.Add(new()
            {
                ID = i,
                rt = new WaveRuntime
                {
                    waveId = wave.waveId,
                    started = false,
                    cleared = false,
                    aliveEnemy = 0,
                    aliveTarget = 0
                }
            }
            );
            waveDefinitions.Add(wave);
            i++;
        }

        isInit = true;

    }


    #endregion

    [Header("スコア重みづけ")]
    public float Weight_hp = 10f;
    public float Weight_time = 10f;

    public void NotifyEnemyDestroyed(int waveId, bool isTarget)
    {
        if (waveId < 0 || waveId >= waveRuntime.Count) return;

        var runtime = waveRuntime[waveId];
        runtime.rt.aliveEnemy = Mathf.Max(0, runtime.rt.aliveEnemy - 1);
        if (isTarget)
            runtime.rt.aliveTarget = Mathf.Max(0, runtime.rt.aliveTarget - 1);
    }

    void FinishStage()
    {
        Debug.Log("[SpawnManager] All waves completed.");
        float currentScore = ObjectManager.Instance.score;
        PlayerPrefs.SetFloat("LastScore", currentScore);
        PlayerPrefs.SetString("LastScene", currentStage.sceneName);
        PlayerPrefs.SetFloat("TimeBonus", Time.time * Weight_time);

        Player.GetComponent<AugumentStatus>().TryGetHP(out float hp, out float max);
        PlayerPrefs.SetFloat("hpBonus", hp * Weight_hp);
        PlayerPrefs.Save();

        isStageClear = true;
        return;
    }

    //spawn内のwaveIDに基づいて敵を有効化
    void ActivateWave(WaveDefinition spawn)
    {
        int waveId = spawn.waveId;
        if (waveId < 0 || waveId >= waveRuntime.Count) return;
        if (spawn.enemyIds == null) return;

        var runtime = waveRuntime[waveId];

        for (int i = 0; i < spawn.enemyIds.Count; i++)
        {
            int id = spawn.enemyIds[i];
            if (id < 0 || id >= enemies.Count) continue;

            GameObject enemy = enemies[id];
            if (enemy == null) continue;
            if (!enemy.TryGetComponent(out AugumentStatus aug)) continue;

            bool isTarget =
                spawn.isMissionTarget != null &&
                i < spawn.isMissionTarget.Count &&
                spawn.isMissionTarget[i];

            aug.missionObjective = isTarget;

            if (spawn.lifetimes != null && i < spawn.lifetimes.Count)
                aug.lifeTime = spawn.lifetimes[i];

            enemy.SetActive(true);
            ObjectManager.Instance.RegisterEnemy(enemy, waveId);
            aug.issortie = true;
            aug.waveID = waveId;


            runtime.rt.aliveEnemy++;
            if (isTarget)
                runtime.rt.aliveTarget++;
        }

    }

    string GetStreamingAssetsPath()
    {
        return System.IO.Path.Combine(
            Application.streamingAssetsPath,
            "stage_spawns.json"
        );
    }
}




[System.Serializable]
public class WaveDefinition
{
    // StreamingAssets/stage_spawns.json uses these legacy names.
    public int WaveId = -1;
    public int triggerTargetWaveId = -1;

    public int waveId;

    // このWaveが開始する条件
    public List<int> requireClearedWaves;

    // このWaveで出す敵
    public List<int> enemyIds;

    // どれがターゲットか
    public List<bool> isMissionTarget;

    // 敵のライフタイム（秒）。0以下なら無制限
    public List<float> lifetimes;

    public void Normalize()
    {
        if (WaveId >= 0)
            waveId = WaveId;

        requireClearedWaves ??= new List<int>();

        if (triggerTargetWaveId >= 0 &&
            !requireClearedWaves.Contains(triggerTargetWaveId))
        {
            requireClearedWaves.Add(triggerTargetWaveId);
        }
    }
}

[System.Serializable]
public class StageData
{
    public string sceneName;
    public List<WaveDefinition> spawns;
}

[System.Serializable]
public class StageRoot
{
    public List<StageData> stages;
}
