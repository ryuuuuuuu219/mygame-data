using System.Collections.Generic;
using UnityEngine;

public class SpawnPrefabRegistry : MonoBehaviour
{
    public List<SpawnPrefabEntry> entries = new();

    public GameObject GetPrefab(string prefabTypeName)
    {
        if (string.IsNullOrEmpty(prefabTypeName)) return null;

        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (entry.prefabTypeName == prefabTypeName)
                return entry.prefab;
        }

        return null;
    }
}

[System.Serializable]
public class SpawnPrefabEntry
{
    public string prefabTypeName;
    public GameObject prefab;
}
