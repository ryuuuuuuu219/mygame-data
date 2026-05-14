using System.Collections.Generic;
using UnityEngine;

public class M02DesignController : MonoBehaviour
{
    [System.Serializable]
    public struct StorageSet
    {
        public Vector3 position;
    }

    public int finalWaveId = 1;
    public float startDistance = 6000f;
    public float approachDistance = 2000f;
    public float storageAltitudeOffset = 3f;
    public float aaGunRadius = 300f;

    public StorageSet[] storageSets =
    {
        new StorageSet { position = new Vector3(0f, 0f, 0f) },
        new StorageSet { position = new Vector3(-2800f, 0f, 2400f) },
        new StorageSet { position = new Vector3(2800f, 0f, 2400f) }
    };

    readonly List<UnknownPhaseTrigger> unknowns = new();
    readonly List<GameObject> pendingStorages = new();
    readonly HashSet<int> startedWaves = new();

    SpawnTableManager spawnTableManager;
    SpawnPlacementManager placementManager;
    SpawnPrefabRegistry prefabRegistry;
    GameObject player;
    bool initialized;

    public void Initialize(SpawnTableManager manager, SpawnPlacementManager placement, GameObject playerObject)
    {
        if (initialized) return;

        spawnTableManager = manager;
        placementManager = placement;
        prefabRegistry = placementManager != null ? placementManager.prefabRegistry : null;
        player = playerObject;

        initialized = true;
    }

    public void StartWave(int waveId)
    {
        if (!initialized || !startedWaves.Add(waveId)) return;

        if (waveId == 0)
        {
            PositionPlayerForOpening();
            SpawnM02Set(0, waveId, true);
            return;
        }

        if (waveId == finalWaveId)
        {
            RegisterPendingStoragesAsFinalTargets();
            SpawnM02Set(1, waveId, false);
            SpawnM02Set(2, waveId, false);
            spawnTableManager.ReserveRuntimeTargets(finalWaveId, 2);
        }
    }

    void PositionPlayerForOpening()
    {
        if (player == null || storageSets == null || storageSets.Length == 0) return;

        Vector3 center = GroundPosition(storageSets[0].position);
        player.transform.position = center + new Vector3(0f, 1200f, -startDistance);
        player.transform.rotation = Quaternion.LookRotation((center - player.transform.position).normalized, Vector3.up);

        if (player.TryGetComponent(out Rigidbody rb))
        {
            Vector3 velocity = player.transform.forward * Mathf.Max(200f, rb.linearVelocity.magnitude);
            rb.linearVelocity = velocity;
        }
    }

    void SpawnM02Set(int index, int waveId, bool transitionTarget)
    {
        if (storageSets == null || index < 0 || index >= storageSets.Length) return;

        Vector3 groundPosition = GroundPosition(storageSets[index].position);
        SpawnUnknown(groundPosition, waveId, transitionTarget);
        SpawnAntiAirRing(groundPosition, waveId);
    }

    void SpawnUnknown(Vector3 position, int waveId, bool transitionTarget)
    {
        var unknown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unknown.name = "unknown";
        unknown.tag = "enemy";
        unknown.transform.position = position + Vector3.up * storageAltitudeOffset;
        unknown.transform.localScale = new Vector3(40f, 16f, 40f);

        var rb = unknown.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var status = unknown.AddComponent<AugumentStatus>();
        status.isEnemy = true;
        status.isPlayer = false;
        status.missionObjective = transitionTarget;
        status.issortie = true;
        status.isVisible = true;
        status.lifeTime = 0f;
        status.waveID = waveId;
        status.hp = 100000f;
        status.maxhp = 100000f;

        var trigger = unknown.AddComponent<UnknownPhaseTrigger>();
        trigger.Initialize(this, player, approachDistance);
        unknowns.Add(trigger);

        spawnTableManager.RegisterRuntimeEnemy(unknown, waveId, transitionTarget);
    }

    void SpawnAntiAirRing(Vector3 center, int waveId)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * aaGunRadius;
            Quaternion rotation = Quaternion.LookRotation((center - position).normalized, Vector3.up);

            SpawnRegisteredEnemy("AA_GUN", position, rotation, false, true, Vector3.zero, waveId);
        }
    }

    public void ActivateStorage(GameObject unknown)
    {
        if (unknown == null) return;

        int previousWaveId = -1;
        bool wasTransitionTarget = false;

        if (unknown.TryGetComponent(out AugumentStatus status))
        {
            previousWaveId = status.waveID;
            wasTransitionTarget = status.missionObjective;
            status.missionObjective = false;
        }

        ObjectManager.Instance.UnregisterEnemy(unknown, previousWaveId);

        unknown.name = "UAV_Storage";

        var storage = unknown.AddComponent<UAVStorage>();
        storage.Initialize(this);

        if (status != null)
        {
            status.missionObjective = previousWaveId == finalWaveId;
            status.issortie = true;
            status.isVisible = true;
            status.lifeTime = 0f;
            status.waveID = finalWaveId;
            status.hp = 900f;
            status.maxhp = 900f;
        }

        if (previousWaveId == finalWaveId)
        {
            spawnTableManager.RegisterRuntimeEnemy(unknown, finalWaveId, false);
        }
        else
        {
            pendingStorages.Add(unknown);
            if (wasTransitionTarget)
                spawnTableManager.NotifyEnemyDestroyed(previousWaveId, true);
        }
    }

    public void LaunchFighters(Vector3 center, Quaternion storageRotation)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 position = center + direction * 80f + Vector3.up * 220f;
            Vector3 velocity = direction * 350f + Vector3.up * 35f;

            SpawnRegisteredEnemy("fighter", position, rotation, false, false, velocity, finalWaveId);
        }
    }

    void RegisterPendingStoragesAsFinalTargets()
    {
        foreach (var storage in pendingStorages)
        {
            if (storage == null) continue;

            if (storage.TryGetComponent(out AugumentStatus status))
            {
                status.waveID = finalWaveId;
                status.missionObjective = true;
            }

            spawnTableManager.RegisterRuntimeEnemy(storage, finalWaveId, true);
        }

        pendingStorages.Clear();
    }

    GameObject SpawnRegisteredEnemy(
        string prefabType,
        Vector3 position,
        Quaternion rotation,
        bool missionTarget,
        bool snapToTerrain,
        Vector3 initialVelocity,
        int waveId)
    {
        GameObject prefab = prefabRegistry != null ? prefabRegistry.GetPrefab(prefabType) : null;
        if (prefab == null)
        {
            Debug.LogWarning($"[M02Design] Prefab type not found: {prefabType}");
            return null;
        }

        var definition = new EnemySpawnDefinition
        {
            prefabType = prefabType,
            missionTarget = missionTarget,
            lifetime = 0f,
            placement = new PlacementDefinition
            {
                mode = "fixed",
                count = 1,
                position = new SerializableVector3 { x = position.x, y = position.y, z = position.z },
                rotate = new SerializableVector3 { x = rotation.eulerAngles.x, y = rotation.eulerAngles.y, z = rotation.eulerAngles.z },
                vector = new SerializableVector3 { x = initialVelocity.x, y = initialVelocity.y, z = initialVelocity.z },
                isstoped = initialVelocity.sqrMagnitude <= 0.01f,
                snapToTerrain = snapToTerrain,
                terrainLayer = "Terrain"
            }
        };

        var result = placementManager.SpawnEnemyGroup(new List<GameObject>(), definition, waveId);
        return result.aliveEnemy > 0 ? null : null;
    }

    Vector3 GroundPosition(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
            return position;

        position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        return position;
    }
}
