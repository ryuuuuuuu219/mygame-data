using UnityEngine;

public class TutorialTargetSwitchSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public GameObject enemyPrefabA;
    public GameObject enemyPrefabB;
    public float triggerZ = -1500f;
    public float spawnDistance = 700f;
    public float spawnAltitudeOffset = 0f;
    public float secondEnemyClockAngle = 60f;
    public bool scoreZero = true;
    public bool missionObjective;
    public bool missionObjectiveA;
    public bool missionObjectiveB;

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
        if (spawned || player == null || (enemyPrefab == null && enemyPrefabA == null && enemyPrefabB == null)) return;
        if (player.position.z >= triggerZ) return;

        SpawnPair();
        spawned = true;
    }

    void SpawnPair()
    {
        SpawnEnemy(enemyPrefabA != null ? enemyPrefabA : enemyPrefab, player.forward, "TutorialTargetSwitchEnemy_0", missionObjectiveA || missionObjective);
        Vector3 secondDirection = Quaternion.AngleAxis(secondEnemyClockAngle, Vector3.up) * player.forward;
        SpawnEnemy(enemyPrefabB != null ? enemyPrefabB : enemyPrefab, secondDirection, "TutorialTargetSwitchEnemy_2", missionObjectiveB || missionObjective);
    }

    void SpawnEnemy(GameObject prefab, Vector3 direction, string objectName, bool isMissionObjective)
    {
        if (prefab == null) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        Vector3 position = player.position + direction.normalized * spawnDistance;
        position.y += spawnAltitudeOffset;

        GameObject enemy = Instantiate(prefab, position, Quaternion.LookRotation(-direction.normalized, Vector3.up));
        enemy.name = objectName;
        enemy.SetActive(true);
        SyncVectorWithPlayer(enemy);

        if (enemy.TryGetComponent(out AugumentStatus status))
        {
            status.isEnemy = true;
            status.isPlayer = false;
            status.missionObjective = isMissionObjective;
            status.issortie = true;
            if (scoreZero)
                status.SetScoreReward(0f);
        }

        if (ObjectManager.Instance != null)
            ObjectManager.Instance.RegisterEnemy(enemy, -1);
    }

    void SyncVectorWithPlayer(GameObject enemy)
    {
        if (player == null || enemy == null) return;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        Vector3 velocity = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        if (velocity.sqrMagnitude < 0.0001f)
            velocity = player.forward;

        if (enemy.TryGetComponent(out Rigidbody enemyRb))
            enemyRb.linearVelocity = velocity;

        if (enemy.TryGetComponent(out AircraftController aircraft))
            aircraft.Velocity = velocity;

        if (enemy.TryGetComponent(out AugumentStatus status))
            status.Velocity = velocity;
    }
}
