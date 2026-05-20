using System.Collections.Generic;
using UnityEngine;

public class SpawnPlacementManager : MonoBehaviour
{
    public SpawnPrefabRegistry prefabRegistry;
    public WorldGenerator worldGenerator;
    System.Random random;

    void Awake()
    {
        if (prefabRegistry == null)
            prefabRegistry = GetComponent<SpawnPrefabRegistry>();
        if (worldGenerator == null)
            worldGenerator = FindFirstObjectByType<WorldGenerator>();
    }

    public SpawnResult SpawnEnemyGroup(List<GameObject> enemies, EnemySpawnDefinition definition, int waveId)
    {
        var result = new SpawnResult();

        if (definition == null) return result;

        GameObject prefab = prefabRegistry != null
            ? prefabRegistry.GetPrefab(definition.prefabType)
            : null;

        GameObject source = prefab != null
            ? prefab
            : GetSceneEnemy(enemies, definition.enemyId);
        if (source == null) return result;

        int count = definition.placement != null
            ? Mathf.Max(1, definition.placement.count)
            : 1;

        for (int i = 0; i < count; i++)
        {
            GameObject enemy = prefab != null || i > 0
                ? Instantiate(source)
                : source;
            result.Add(SpawnEnemy(enemy, definition, waveId));
        }

        return result;
    }

    public SpawnResult SpawnEnemy(GameObject enemy, EnemySpawnDefinition definition, int waveId)
    {
        var result = new SpawnResult();
        if (enemy == null || definition == null) return result;

        ApplyPlacement(enemy, definition.placement);
        ApplyInitialMotion(enemy, definition.placement);

        GameObject registeredEnemy = enemy;
        if (definition.prefabType == "AIR_BATTLESHIP" &&
            !enemy.TryGetComponent(out AirBattleshipBase _))
        {
            enemy.AddComponent<AirBattleshipBase>();
        }

        if (enemy.TryGetComponent(out AirBattleshipBase battleshipBase))
        {
            registeredEnemy = battleshipBase.EnsureCoreBlockEntity();
        }
        else if (!enemy.TryGetComponent(out AugumentStatus _))
        {
            return result;
        }

        if (!registeredEnemy.TryGetComponent(out AugumentStatus aug)) return result;

        aug.missionObjective = definition.missionTarget;
        aug.isEnemy = true;
        aug.isPlayer = false;
        aug.lifeTime = definition.lifetime;
        aug.issortie = true;
        aug.waveID = waveId;

        enemy.SetActive(true);
        registeredEnemy.SetActive(true);
        ObjectManager.Instance.RegisterEnemy(registeredEnemy, waveId);

        result.aliveEnemy = 1;
        result.aliveTarget = definition.missionTarget ? 1 : 0;
        return result;
    }

    void ApplyPlacement(GameObject enemy, PlacementDefinition placement)
    {
        if (placement == null) return;

        if (placement.position != null)
            enemy.transform.position = placement.position.ToVector3();

        if (placement.mode == "terrainRandom")
            ApplyTerrainRandomPosition(enemy, placement);

        if (placement.snapToTerrain)
            SnapToTerrain(enemy, placement);

        if (placement.rotate != null)
            enemy.transform.rotation = Quaternion.Euler(placement.rotate.ToVector3());
    }

    public void SetRandomSeed(int seed)
    {
        random = new System.Random(seed);
    }

    void ApplyTerrainRandomPosition(GameObject enemy, PlacementDefinition placement)
    {
        if (placement.position == null) return;

        Vector3 center = placement.position.ToVector3();
        float radius = Mathf.Max(0f, placement.radius);
        bool hasAltitudeRange = placement.minAltitude < placement.maxAltitude;

        for (int i = 0; i < 64; i++)
        {
            Vector2 offset = RandomInsideCircle(radius);
            enemy.transform.position = new Vector3(center.x + offset.x, center.y, center.z + offset.y);

            if (placement.snapToTerrain)
                SnapToTerrain(enemy, placement);

            if (!hasAltitudeRange ||
                (enemy.transform.position.y >= placement.minAltitude &&
                 enemy.transform.position.y <= placement.maxAltitude))
            {
                return;
            }
        }

        enemy.transform.position = center;
        if (placement.snapToTerrain)
            SnapToTerrain(enemy, placement);
    }

    Vector2 RandomInsideCircle(float radius)
    {
        if (radius <= 0f) return Vector2.zero;

        random ??= new System.Random();
        double angle = random.NextDouble() * System.Math.PI * 2d;
        double distance = System.Math.Sqrt(random.NextDouble()) * radius;

        return new Vector2(
            (float)(System.Math.Cos(angle) * distance),
            (float)(System.Math.Sin(angle) * distance)
        );
    }

    void SnapToTerrain(GameObject enemy, PlacementDefinition placement)
    {
        if (TrySnapToUnityTerrain(enemy))
            return;

        Vector3 origin = enemy.transform.position + Vector3.up * 10000f;
        int layerMask = Physics.DefaultRaycastLayers;

        if (!string.IsNullOrEmpty(placement.terrainLayer))
        {
            int layer = LayerMask.NameToLayer(placement.terrainLayer);
            if (layer >= 0)
                layerMask = 1 << layer;
        }

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20000f, layerMask) &&
            hit.collider is TerrainCollider)
        {
            enemy.transform.position = hit.point;
        }
    }

    bool TrySnapToUnityTerrain(GameObject enemy)
    {
        Terrain terrain = GetTerrainAt(enemy.transform.position);
        if (terrain == null) return false;

        Vector3 position = enemy.transform.position;
        position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        enemy.transform.position = position;
        return true;
    }

    Terrain GetTerrainAt(Vector3 position)
    {
        if (worldGenerator == null)
            worldGenerator = FindFirstObjectByType<WorldGenerator>();

        if (worldGenerator != null && worldGenerator.terrain != null &&
            IsInsideTerrain(worldGenerator.terrain, position))
        {
            return worldGenerator.terrain;
        }

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, position))
                return terrain;
        }

        return null;
    }

    bool IsInsideTerrain(Terrain terrain, Vector3 position)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        return position.x >= terrainPosition.x &&
               position.x <= terrainPosition.x + terrainSize.x &&
               position.z >= terrainPosition.z &&
               position.z <= terrainPosition.z + terrainSize.z;
    }

    void ApplyInitialMotion(GameObject enemy, PlacementDefinition placement)
    {
        if (placement == null) return;
        if (!placement.isstoped && placement.vector == null) return;

        Vector3 velocity = placement.isstoped || placement.vector == null
            ? Vector3.zero
            : placement.vector.ToVector3();

        if (enemy.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = velocity;

        if (enemy.TryGetComponent(out AugumentStatus aug))
            aug.Velocity = velocity;

        if (enemy.TryGetComponent(out AircraftController aircraft))
            aircraft.Velocity = velocity;
    }

    GameObject GetSceneEnemy(List<GameObject> enemies, int id)
    {
        if (enemies == null) return null;
        if (id < 0 || id >= enemies.Count) return null;

        return enemies[id];
    }
}

public class SpawnResult
{
    public int aliveEnemy;
    public int aliveTarget;

    public void Add(SpawnResult other)
    {
        if (other == null) return;

        aliveEnemy += other.aliveEnemy;
        aliveTarget += other.aliveTarget;
    }
}
