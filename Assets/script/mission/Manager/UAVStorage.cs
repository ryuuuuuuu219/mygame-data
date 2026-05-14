using System.Collections.Generic;
using UnityEngine;

public class UAVStorage : MonoBehaviour
{
    public float launchDelay = 0.5f;

    UAVStorageMissionController controller;
    float timer;
    bool launched;

    public void Initialize(UAVStorageMissionController owner)
    {
        controller = owner;
        timer = launchDelay;
    }

    void Update()
    {
        if (launched) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        launched = true;
        controller.LaunchFighters(transform.position, transform.rotation);
    }
}

public interface IUAVStoragePresentation
{
    void OnUnknownActivatedAsStorage(GameObject storage, int previousWaveId, bool wasTransitionTarget);
}

public class UAVStorageMissionController
{
    public int FinalWaveId { get; set; } = 1;
    public float StartDistance { get; set; } = 6000f;
    public float ApproachDistance { get; set; } = 2000f;
    public float StorageAltitudeOffset { get; set; } = 3f;
    public float AaGunRadius { get; set; } = 300f;
    public int FighterCount { get; set; } = 3;
    public float FighterSpacingAngle { get; set; } = 45f;
    public float FighterSpawnRadius { get; set; } = 80f;
    public float FighterSpawnAltitude { get; set; } = 220f;
    public float FighterSpeed { get; set; } = 350f;
    public string FighterPrefabType { get; set; } = "fighter";
    public string AntiAirPrefabType { get; set; } = "AA_GUN";

    readonly List<UnknownPhaseTrigger> unknowns = new();
    readonly List<GameObject> pendingStorages = new();
    readonly HashSet<int> startedWaves = new();

    SpawnTableManager spawnTableManager;
    SpawnPlacementManager placementManager;
    SpawnPrefabRegistry prefabRegistry;
    GameObject player;
    IUAVStoragePresentation presentation;
    bool initialized;

    public void Initialize(
        SpawnTableManager manager,
        SpawnPlacementManager placement,
        GameObject playerObject,
        IUAVStoragePresentation storagePresentation)
    {
        spawnTableManager = manager;
        placementManager = placement;
        prefabRegistry = placementManager != null ? placementManager.prefabRegistry : null;
        player = playerObject;
        presentation = storagePresentation;
        initialized = true;
    }

    public void StartWave(int waveId, IReadOnlyList<Vector3> storagePositions)
    {
        if (!initialized || !startedWaves.Add(waveId)) return;
        if (storagePositions == null || storagePositions.Count == 0) return;

        if (waveId == 0)
        {
            PositionPlayerForOpening(storagePositions[0]);
            SpawnStorageSet(storagePositions[0], waveId, true);
            return;
        }

        if (waveId == FinalWaveId)
        {
            RegisterPendingStoragesAsFinalTargets();

            for (int i = 1; i < storagePositions.Count; i++)
                SpawnStorageSet(storagePositions[i], waveId, false);

            spawnTableManager.ReserveRuntimeTargets(FinalWaveId, Mathf.Max(0, storagePositions.Count - 1));
        }
    }

    void PositionPlayerForOpening(Vector3 storagePosition)
    {
        if (player == null) return;

        Vector3 center = GroundPosition(storagePosition);
        player.transform.position = center + new Vector3(0f, 1200f, -StartDistance);
        player.transform.rotation = Quaternion.LookRotation((center - player.transform.position).normalized, Vector3.up);

        if (player.TryGetComponent(out Rigidbody rb))
        {
            Vector3 velocity = player.transform.forward * Mathf.Max(200f, rb.linearVelocity.magnitude);
            rb.linearVelocity = velocity;
        }
    }

    void SpawnStorageSet(Vector3 storagePosition, int waveId, bool transitionTarget)
    {
        Vector3 groundPosition = GroundPosition(storagePosition);
        SpawnUnknown(groundPosition, waveId, transitionTarget);
        SpawnAntiAirRing(groundPosition, waveId);
    }

    void SpawnUnknown(Vector3 position, int waveId, bool transitionTarget)
    {
        var unknown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unknown.name = "unknown";
        unknown.tag = "enemy";
        unknown.transform.position = position + Vector3.up * StorageAltitudeOffset;
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
        trigger.Initialize(this, player, ApproachDistance);
        unknowns.Add(trigger);

        spawnTableManager.RegisterRuntimeEnemy(unknown, waveId, transitionTarget);
    }

    void SpawnAntiAirRing(Vector3 center, int waveId)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector3 position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * AaGunRadius;
            Quaternion rotation = Quaternion.LookRotation((center - position).normalized, Vector3.up);

            SpawnRegisteredEnemy(AntiAirPrefabType, position, rotation, false, true, Vector3.zero, waveId);
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
        presentation?.OnUnknownActivatedAsStorage(unknown, previousWaveId, wasTransitionTarget);

        var storage = unknown.AddComponent<UAVStorage>();
        storage.Initialize(this);

        if (status != null)
        {
            status.missionObjective = previousWaveId == FinalWaveId;
            status.issortie = true;
            status.isVisible = true;
            status.lifeTime = 0f;
            status.waveID = FinalWaveId;
            status.hp = 900f;
            status.maxhp = 900f;
        }

        if (previousWaveId == FinalWaveId)
        {
            spawnTableManager.RegisterRuntimeEnemy(unknown, FinalWaveId, false);
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
        for (int i = 0; i < FighterCount; i++)
        {
            float angle = i * FighterSpacingAngle;
            Quaternion rotation = storageRotation * Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 position = center + direction * FighterSpawnRadius + Vector3.up * FighterSpawnAltitude;
            Vector3 velocity = direction * FighterSpeed;

            SpawnRegisteredEnemy(FighterPrefabType, position, rotation, false, false, velocity, FinalWaveId);
        }
    }

    void RegisterPendingStoragesAsFinalTargets()
    {
        foreach (var storage in pendingStorages)
        {
            if (storage == null) continue;

            if (storage.TryGetComponent(out AugumentStatus status))
            {
                status.waveID = FinalWaveId;
                status.missionObjective = true;
            }

            spawnTableManager.RegisterRuntimeEnemy(storage, FinalWaveId, true);
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
            Debug.LogWarning($"[UAVStorageMission] Prefab type not found: {prefabType}");
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

        SpawnResult result = placementManager.SpawnEnemyGroup(new List<GameObject>(), definition, waveId);
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
