using UnityEngine;

public static class ObjectGroundBounds
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

public class PlayerDropChecker : MonoBehaviour
{
    public float interval = 0.5f;
    public float damage = 5f;
    public AugumentStatus s;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
    }

    void OnTriggerStay(Collider other)
    {
        if (!ObjectGroundBounds.IsGroundCollider(other)) return;
        if (timer < interval) return;

        timer = 0f;
        if (s == null)
            TryGetComponent(out s);

        if (s != null)
            s.hp -= damage;
    }
}
