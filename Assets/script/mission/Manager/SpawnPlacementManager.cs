using System.Collections.Generic;
using UnityEngine;

public class SpawnPlacementManager : MonoBehaviour
{
    public SpawnResult SpawnEnemyGroup(List<GameObject> enemies, EnemySpawnDefinition definition, int waveId)
    {
        var result = new SpawnResult();

        if (enemies == null || definition == null) return result;

        int id = definition.enemyId;
        if (id < 0 || id >= enemies.Count) return result;

        GameObject source = enemies[id];
        if (source == null) return result;

        int count = definition.placement != null
            ? Mathf.Max(1, definition.placement.count)
            : 1;

        for (int i = 0; i < count; i++)
        {
            GameObject enemy = i == 0 ? source : Instantiate(source);
            result.Add(SpawnEnemy(enemy, definition, waveId));
        }

        return result;
    }

    public SpawnResult SpawnEnemy(GameObject enemy, EnemySpawnDefinition definition, int waveId)
    {
        var result = new SpawnResult();
        if (enemy == null || definition == null) return result;
        if (!enemy.TryGetComponent(out AugumentStatus aug)) return result;

        ApplyPlacement(enemy, definition.placement);
        ApplyInitialMotion(enemy, definition.placement);

        aug.missionObjective = definition.missionTarget;
        aug.lifeTime = definition.lifetime;
        aug.issortie = true;
        aug.waveID = waveId;

        enemy.SetActive(true);
        ObjectManager.Instance.RegisterEnemy(enemy, waveId);

        result.aliveEnemy = 1;
        result.aliveTarget = definition.missionTarget ? 1 : 0;
        return result;
    }

    void ApplyPlacement(GameObject enemy, PlacementDefinition placement)
    {
        if (placement == null) return;

        if (placement.position != null)
            enemy.transform.position = placement.position.ToVector3();

        if (placement.rotate != null)
            enemy.transform.rotation = Quaternion.Euler(placement.rotate.ToVector3());
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
