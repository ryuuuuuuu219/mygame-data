using UnityEngine;

public class CloudVoxelRenderer : MonoBehaviour
{
    public CloudPreset preset;
    public float alpha = 0.3f;
    public bool addColliders = false;

    public void Render(CloudPreset sourcePreset, Material material)
    {
        preset = sourcePreset;
        if (preset == null || material == null)
        {
            return;
        }

        ClearChildren();
        Vector3 origin = new Vector3(
            -(preset.sizeX - 1) * preset.cellSize * 0.5f,
            0f,
            -(preset.sizeZ - 1) * preset.cellSize * 0.5f
        );
        for (int z = 0; z < preset.sizeZ; z++)
        {
            for (int y = 0; y < preset.sizeY; y++)
            {
                for (int x = 0; x < preset.sizeX; x++)
                {
                    if (!preset.IsFilled(x, y, z))
                    {
                        continue;
                    }

                    CreateVoxel(origin, x, y, z, material, alpha);
                }
            }
        }
    }

    private void CreateVoxel(Vector3 origin, int x, int y, int z, Material baseMaterial, float voxelAlpha)
    {
        GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxel.name = $"CloudVoxel_{x:00}_{y:00}_{z:00}";
        voxel.transform.SetParent(transform, false);
        voxel.transform.localPosition = origin + new Vector3(
            x * preset.cellSize,
            y * preset.cellSize,
            z * preset.cellSize
        );
        voxel.transform.localScale = Vector3.one * preset.cellSize;

        if (!addColliders)
        {
            Collider collider = voxel.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        Material voxelMaterial = new Material(baseMaterial);
        Color color = voxelMaterial.HasProperty("_BaseColor")
            ? voxelMaterial.GetColor("_BaseColor")
            : voxelMaterial.color;
        color.a = voxelAlpha;
        voxelMaterial.color = color;
        voxelMaterial.SetColor("_BaseColor", color);
        voxelMaterial.SetColor("_Color", color);
        voxel.GetComponent<Renderer>().sharedMaterial = voxelMaterial;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
