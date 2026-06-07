using UnityEngine;

public class TutorialTargetSwitchSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public float triggerZ = -800f;
    public float spawnDistance = 700f;
    public float spawnAltitudeOffset = 0f;
    public float secondEnemyClockAngle = 60f;
    public bool scoreZero = true;
    public bool missionObjective;

    bool spawned;

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
        if (spawned || player == null || enemyPrefab == null) return;
        if (player.position.z >= triggerZ) return;

        SpawnPair();
        spawned = true;
    }

    void SpawnPair()
    {
        SpawnEnemy(player.forward, "TutorialTargetSwitchEnemy_0");
        Vector3 secondDirection = Quaternion.AngleAxis(secondEnemyClockAngle, Vector3.up) * player.forward;
        SpawnEnemy(secondDirection, "TutorialTargetSwitchEnemy_2");
    }

    void SpawnEnemy(Vector3 direction, string objectName)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        Vector3 position = player.position + direction.normalized * spawnDistance;
        position.y += spawnAltitudeOffset;

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.LookRotation(-direction.normalized, Vector3.up));
        enemy.name = objectName;

        if (enemy.TryGetComponent(out AugumentStatus status))
        {
            status.isEnemy = true;
            status.isPlayer = false;
            status.missionObjective = missionObjective;
            status.issortie = true;
            if (scoreZero)
                status.SetScoreReward(0f);
        }

        if (ObjectManager.Instance != null)
            ObjectManager.Instance.RegisterEnemy(enemy, -1);
    }
}
