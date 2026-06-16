using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SpawnTableManager : MonoBehaviour
{
    public GameObject Player;
    public List<GameObject> enemies;
    public SpawnPlacementManager spawnPlacementManager;
    public bool disableSceneEnemiesOnStart = true;

    [Tooltip("例: https://ユーザー名.github.io/mygame-data/stage_spawns.json")]
    public string jsonUrl;

    public bool isUseRemoteJSON = false;
    public bool isInit = false;
    public bool isStageClear = false;

    private StageRoot stageRoot;
    private StageData currentStage;
    private float missionStartTime;

    public int currentWave = 0;
    public float MissionElapsedTime => isInit ? Mathf.Max(0f, Time.time - missionStartTime) : 0f;

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
        if (spawnPlacementManager == null)
            spawnPlacementManager = GetComponent<SpawnPlacementManager>();
        if (spawnPlacementManager == null)
            spawnPlacementManager = gameObject.AddComponent<SpawnPlacementManager>();

        currentStage = new();
        currentWave = -1;

        if (disableSceneEnemiesOnStart)
            DisableSceneEnemies();

        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.SetActive(false);
        }

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
        if (!isStageClear && AllWavesCleared())
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
                    if (TryParseJSON(request.downloadHandler.text, "Remote"))
                        yield break;

                    Debug.LogWarning("[SpawnTableManager] Remote JSON parse failed. Fallback to StreamingAssets.");
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
            TryParseJSON(request.downloadHandler.text, "StreamingAssets");
        }

    }
    bool TryParseJSON(string json, string source)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError($"[SpawnTableManager] {source} JSON is empty.");
            return false;
        }

        json = SanitizeJson(json);

        try
        {
            stageRoot = JsonUtility.FromJson<StageRoot>(json);
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError($"[SpawnTableManager] {source} JSON parse error: {ex.Message}\nPreview: {BuildJsonPreview(json)}");
            return false;
        }

        if (stageRoot == null || stageRoot.stages == null)
        {
            Debug.LogError($"[SpawnTableManager] {source} JSON parse failed or no stages found.\nPreview: {BuildJsonPreview(json)}");
            return false;
        }

        Debug.Log($"[SpawnTableManager] Loaded {stageRoot.stages.Count} stages from JSON");
        foreach (var stage in stageRoot.stages)
        {
            Debug.Log($"[Stage] '{stage.sceneName}'");
        }

        InitializeStage();
        return true;
    }

    string SanitizeJson(string json)
    {
        return json.Trim('\uFEFF', '\u200B', '\u0000', ' ', '\r', '\n', '\t');
    }

    string BuildJsonPreview(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "";

        int length = Mathf.Min(180, json.Length);
        return json.Substring(0, length).Replace("\r", "\\r").Replace("\n", "\\n");
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

        spawnPlacementManager.SetRandomSeed(currentStage.randomSeed);
        missionStartTime = Time.time;
        isInit = true;

    }

    void DisableSceneEnemies()
    {
        AugumentStatus[] statuses = FindObjectsByType<AugumentStatus>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (var status in statuses)
        {
            if (status != null && status.isEnemy)
                status.gameObject.SetActive(false);
        }
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

    public void RegisterRuntimeEnemy(GameObject enemy, int waveId, bool isTarget)
    {
        if (enemy == null) return;
        if (waveId < 0 || waveId >= waveRuntime.Count) return;

        ObjectManager.Instance.RegisterEnemy(enemy, waveId);

        var runtime = waveRuntime[waveId];
        runtime.rt.aliveEnemy++;
        if (isTarget)
            runtime.rt.aliveTarget++;
    }

    public void ReserveRuntimeTargets(int waveId, int targetCount)
    {
        if (waveId < 0 || waveId >= waveRuntime.Count) return;
        if (targetCount <= 0) return;

        var runtime = waveRuntime[waveId];
        runtime.rt.aliveTarget += targetCount;
    }

    void FinishStage()
    {
        Debug.Log("[SpawnManager] All waves completed.");
        float currentScore = ObjectManager.Instance.score;
        PlayerPrefs.SetFloat("LastScore", currentScore);
        PlayerPrefs.SetString("LastScene", currentStage.sceneName);
        PlayerPrefs.SetFloat("TimeBonus", MissionElapsedTime);

        Player.GetComponent<AugumentStatus>().TryGetHP(out float hp, out float max);
        PlayerPrefs.SetFloat("HpRate", max > 0f ? Mathf.Clamp01(hp / max) : 0f);
        PlayerPrefs.Save();

        isStageClear = true;
        GeneratedAudioManager.Play(GeneratedAudioCue.StageClear, null, 0.9f);
        GeneratedAudioManager.SetBgm(GeneratedBgmState.Clear);
        return;
    }

    //spawn内のwaveIDに基づいて敵を有効化
    void ActivateWave(WaveDefinition spawn)
    {
        if (spawn.enemies != null && spawn.enemies.Count > 0)
        {
            ActivateWaveNew(spawn);
            return;
        }

        ActivateWave_regacy(spawn);
    }

    void ActivateWaveNew(WaveDefinition spawn)
    {
        int waveId = spawn.waveId;
        if (waveId < 0 || waveId >= waveRuntime.Count) return;
        if (spawnPlacementManager == null) return;

        var runtime = waveRuntime[waveId];

        foreach (var enemyDefinition in spawn.enemies)
        {
            SpawnResult result = spawnPlacementManager.SpawnEnemyGroup(enemies, enemyDefinition, waveId);
            runtime.rt.aliveEnemy += result.aliveEnemy;
            runtime.rt.aliveTarget += result.aliveTarget;
        }
    }

    void ActivateWave_regacy(WaveDefinition spawn)
    {
        int waveId = spawn.waveId;
        if (waveId < 0 || waveId >= waveRuntime.Count) return;
        if (spawn.enemyIds == null) return;
        if (spawnPlacementManager == null) return;

        var runtime = waveRuntime[waveId];

        for (int i = 0; i < spawn.enemyIds.Count; i++)
        {
            int id = spawn.enemyIds[i];
            if (id < 0 || id >= enemies.Count) continue;

            GameObject enemy = enemies[id];
            if (enemy == null) continue;

            bool isTarget =
                spawn.isMissionTarget != null &&
                i < spawn.isMissionTarget.Count &&
                spawn.isMissionTarget[i];

            float lifetime = 0f;
            if (spawn.lifetimes != null && i < spawn.lifetimes.Count)
                lifetime = spawn.lifetimes[i];

            var enemyDefinition = new EnemySpawnDefinition
            {
                enemyId = id,
                missionTarget = isTarget,
                lifetime = lifetime
            };

            SpawnResult result = spawnPlacementManager.SpawnEnemy(enemy, enemyDefinition, waveId);
            runtime.rt.aliveEnemy += result.aliveEnemy;
            runtime.rt.aliveTarget += result.aliveTarget;
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

    // 新形式: 敵ごとに目標フラグ、寿命、配置情報をまとめる
    public List<EnemySpawnDefinition> enemies;

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
public class EnemySpawnDefinition
{
    public int enemyId;
    public string prefabType;
    public bool spawnAsAlly;
    public bool missionTarget;
    public float lifetime;
    public PlacementDefinition placement;
    public bool hideFromHud;
    public bool useUnknownPhaseTrigger;
    public bool isPhaseTrrigersParent;
    public string phaseTriggerId;
    public string originName;
    public float approachDistance;
    public UAVLaunchDefinition uavLaunch;
}

[System.Serializable]
public class UAVLaunchDefinition
{
    public bool enabled;
    public bool launchOnPhaseActivate;
    public int capacity = 15;
    public int waveId;
    public float launchDelay = 0.5f;
    public int fighterCount = 3;
    public float fighterSpacingAngle = 45f;
    public float fighterSpawnRadius = 80f;
    public float fighterSpawnAltitude = 220f;
    public float fighterSpeed = 350f;
    public string fighterPrefabType = "fighterGen0";
}

[System.Serializable]
public class PlacementDefinition
{
    public string mode;
    public int count = 1;
    public bool isstoped;
    public SerializableVector3 position;
    public SerializableVector3 vector;
    public SerializableVector3 rotate;
    public bool snapToTerrain;
    public string areaId;
    public string terrainLayer;
    public float radius;
    public float altitudeOffset = 5f;
}

[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public class StageData
{
    public string sceneName;
    public int randomSeed;
    public List<WaveDefinition> spawns;
}

[System.Serializable]
public class StageRoot
{
    public List<StageData> stages;
}
