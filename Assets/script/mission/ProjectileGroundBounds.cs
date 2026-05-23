using UnityEngine;

public static class ProjectileGroundBounds
{
    public static bool IsGroundCollider(Collider collider)
    {
        if (collider == null) return false;

        return collider is TerrainCollider || collider.GetComponentInParent<WorldBlockTerrainCollider>() != null;
    }

    public static bool IsBelowWorldOrTerrain(Vector3 position)
    {
        return position.y < 0f;
    }
}
