using UnityEngine;

public static class ObjectGroundBounds
{
    const float TerrainProbeHeight = 5000f;
    const float TerrainProbeDistance = 10000f;
    static WorldGenerator cachedWorldGenerator;

    public static bool IsGroundCollider(Collider collider)
    {
        if (collider == null) return false;

        return collider is TerrainCollider || collider.GetComponentInParent<WorldBlockTerrainCollider>() != null;
    }

    public static bool IsBelowWorldOrTerrain(Vector3 position)
    {
        if (TryGetTerrainHeight(position, out float terrainHeight))
            return position.y < terrainHeight;

        return position.y < 0f;
    }

    public static bool TryGetTerrainHeight(Vector3 position, out float height)
    {
        height = 0f;

        WorldGenerator worldGenerator = GetWorldGenerator();
        if (worldGenerator != null && worldGenerator.TryGetBlockHeight(position, out height))
            return true;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (position.x < terrainPosition.x || position.z < terrainPosition.z ||
                position.x > terrainPosition.x + size.x || position.z > terrainPosition.z + size.z)
            {
                continue;
            }

            height = terrain.SampleHeight(position) + terrainPosition.y;
            return true;
        }

        Vector3 rayStart = new Vector3(position.x, position.y + TerrainProbeHeight, position.z);
        if (Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            TerrainProbeDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) &&
            IsGroundCollider(hit.collider))
        {
            height = hit.point.y;
            return true;
        }

        return false;
    }

    static WorldGenerator GetWorldGenerator()
    {
        if (cachedWorldGenerator == null)
            cachedWorldGenerator = Object.FindAnyObjectByType<WorldGenerator>();

        return cachedWorldGenerator;
    }
}

public class PlayerDropChecker : MonoBehaviour
{
    public float interval = 0.5f;
    public float damage = 5f;
    public AugumentStatus s;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (ObjectGroundBounds.IsBelowWorldOrTerrain(transform.position))
            ApplyDamageIfReady();
    }

    void OnTriggerStay(Collider other)
    {
        if (!ObjectGroundBounds.IsGroundCollider(other)) return;

        ApplyDamageIfReady();
    }

    void ApplyDamageIfReady()
    {
        if (timer < interval) return;
        timer = 0f;

        if (s == null)
            TryGetComponent(out s);

        if (s != null)
            s.hp -= damage;
    }
}
