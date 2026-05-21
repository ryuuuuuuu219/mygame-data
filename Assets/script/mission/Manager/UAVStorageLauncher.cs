using System.Collections.Generic;
using UnityEngine;

public class UAVStorageLauncher : MonoBehaviour
{
    SpawnPlacementManager placementManager;
    UAVLaunchDefinition launch;
    float timer;
    bool armed;
    public int capacity = 15;
    public int launchnum;
    public GameObject[] UAV = new GameObject[3];

    public void Initialize(SpawnPlacementManager manager, UAVLaunchDefinition definition)
    {
        placementManager = manager;
        launch = definition;
        capacity = launch != null && launch.capacity > 0 ? launch.capacity : 15;
        int launchCount = launch != null ? Mathf.Max(1, launch.fighterCount) : 3;
        UAV = new GameObject[launchCount];
        timer = launch != null ? Mathf.Max(0f, launch.launchDelay) : 0f;
        armed = launch != null && launch.enabled && !launch.launchOnPhaseActivate;
        launchnum = 0;
    }

    public void BeginLaunch()
    {
        if (launch == null || !launch.enabled || launchnum >= capacity)
            return;

        timer = Mathf.Max(0f, launch.launchDelay);
        armed = true;
    }

    void Update()
    {
        if (!armed || launchnum >= capacity)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        LaunchFighters();
        armed = launchnum < capacity;
        timer = Mathf.Max(0f, launch.launchDelay);
    }

    void LaunchFighters()
    {
        if (placementManager == null || launch == null)
            return;

        int count = Mathf.Min(Mathf.Max(0, launch.fighterCount), capacity - launchnum);
        if (UAV == null || UAV.Length != Mathf.Max(1, launch.fighterCount))
            UAV = new GameObject[Mathf.Max(1, launch.fighterCount)];

        string prefabType = string.IsNullOrEmpty(launch.fighterPrefabType)
            ? "fighter"
            : launch.fighterPrefabType;
        int waveId = launch.waveId;

        for (int i = 0; i < count; i++)
        {
            float angle = i * launch.fighterSpacingAngle;
            Quaternion rotation = transform.rotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 position = transform.position +
                direction * launch.fighterSpawnRadius +
                Vector3.up * launch.fighterSpawnAltitude;
            Vector3 velocity = direction * launch.fighterSpeed;

            var definition = new EnemySpawnDefinition
            {
                prefabType = prefabType,
                missionTarget = false,
                lifetime = 0f,
                placement = new PlacementDefinition
                {
                    mode = "fixed",
                    count = 1,
                    position = new SerializableVector3 { x = position.x, y = position.y, z = position.z },
                    rotate = new SerializableVector3 { x = rotation.eulerAngles.x, y = rotation.eulerAngles.y, z = rotation.eulerAngles.z },
                    vector = new SerializableVector3 { x = velocity.x, y = velocity.y, z = velocity.z },
                    isstoped = velocity.sqrMagnitude <= 0.01f,
                    snapToTerrain = false
                }
            };

            SpawnResult result = placementManager.SpawnEnemyGroup(new List<GameObject>(), definition, waveId);
            UAV[i] = result.spawnedEnemy;
            launchnum += result.aliveEnemy;
        }
    }
}
