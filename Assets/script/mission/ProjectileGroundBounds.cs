using UnityEngine;

public static class ProjectileGroundBounds
{
    public static bool IsBelowWorldOrTerrain(Vector3 position)
    {
        if (position.y < 0f) return true;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null) return false;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float localX = position.x - terrainPos.x;
        float localZ = position.z - terrainPos.z;

        if (localX < 0f || localZ < 0f || localX > size.x || localZ > size.z)
            return false;

        float groundY = terrain.SampleHeight(position) + terrainPos.y;
        return position.y <= groundY;
    }
}
