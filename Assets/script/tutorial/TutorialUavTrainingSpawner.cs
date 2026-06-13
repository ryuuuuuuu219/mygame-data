using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialUavTrainingSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public TextMeshProUGUI progressText;
    public Vector3 center = new(-800f, 1500f, -950f);
    public float spawnRadius = 280f;
    public float spawnAltitudeJitter = 80f;
    public int activeCount = 4;
    public int requiredBurstKills = 3;
    public float burstWindow = 4f;
    public bool scoreZero = true;

    readonly List<GameObject> activeEnemies = new();
    readonly Queue<float> recentKillTimes = new();
    bool completed;

    void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void Update()
    {
        CleanupMissingEnemies();
        TrimRecentKills();

        while (!completed && activeEnemies.Count < activeCount)
            SpawnUav();

        UpdateText();
    }

    void SpawnUav()
    {
        GameObject enemy = enemyPrefab != null
            ? Instantiate(enemyPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        float angle = activeEnemies.Count * (360f / Mathf.Max(1, activeCount)) + Time.time * 24f;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * spawnRadius;
        enemy.name = "TutorialMultiLockUAV";
        enemy.transform.position = center + offset + Vector3.up * Random.Range(-spawnAltitudeJitter, spawnAltitudeJitter);
        enemy.transform.rotation = Quaternion.LookRotation((center - enemy.transform.position).normalized, Vector3.up);
        enemy.transform.localScale = Vector3.one;
        enemy.SetActive(true);

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
            rb = enemy.AddComponent<Rigidbody>();
        TutorialRigidbodyStabilizer.Configure(rb, makeKinematic: false, freezeRotation: false);
        TutorialRigidbodyStabilizer stabilizer = enemy.GetComponent<TutorialRigidbodyStabilizer>();
        if (stabilizer == null)
            stabilizer = enemy.AddComponent<TutorialRigidbodyStabilizer>();
        stabilizer.makeKinematic = false;
        stabilizer.freezeRotation = false;
        rb.linearVelocity = enemy.transform.forward * 160f;

        AugumentStatus status = enemy.GetComponent<AugumentStatus>();
        if (status == null)
            status = enemy.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.isPlayer = false;
        status.issortie = true;
        status.isVisible = true;
        status.missionObjective = false;
        status.waveID = -1;
        status.lifeTime = 0f;
        status.hp = 180f;
        status.maxhp = 180f;
        if (scoreZero)
            status.SetScoreReward(0f);
        status.OnDestroyed += OnUavDestroyed;

        Orbitcruise orbit = enemy.GetComponent<Orbitcruise>();
        if (orbit == null)
            orbit = enemy.AddComponent<Orbitcruise>();
        orbit.center = center;
        orbit.orbitRadius = spawnRadius;
        orbit.cruiseThrottle = 0.8f;
        orbit.lowSpeedThrottle = 1.2f;

        ObjectManager.Instance?.RegisterEnemy(enemy, -1);
        activeEnemies.Add(enemy);
    }

    void OnUavDestroyed()
    {
        recentKillTimes.Enqueue(Time.time);
        TrimRecentKills();
        completed = recentKillTimes.Count >= requiredBurstKills;
    }

    void CleanupMissingEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
        }
    }

    void TrimRecentKills()
    {
        while (recentKillTimes.Count > 0 && Time.time - recentKillTimes.Peek() > burstWindow)
            recentKillTimes.Dequeue();
    }

    void UpdateText()
    {
        if (progressText == null) return;

        if (completed)
        {
            progressText.text = "マルチロック練習完了: 複数UAVの同時撃破を確認しました。";
            return;
        }

        progressText.text =
            $"マルチロック練習: {burstWindow:F0}秒以内にUAVを{requiredBurstKills}機撃破 " +
            $"({recentKillTimes.Count}/{requiredBurstKills})";
    }
}
