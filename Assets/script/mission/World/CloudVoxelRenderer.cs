using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

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

        Material sharedMaterial = CreateSharedVoxelMaterial(material, alpha);
        ClearChildren();
        BuildCloudMesh(sharedMaterial);
    }

    private void BuildCloudMesh(Material sharedMaterial)
    {
        Vector3 origin = new Vector3(
            -(preset.sizeX - 1) * preset.cellSize * 0.5f,
            0f,
            -(preset.sizeZ - 1) * preset.cellSize * 0.5f
        );

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector3> normals = new();

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

                    AddVisibleVoxelFaces(vertices, triangles, normals, origin, x, y, z, preset.cellSize);
                }
            }
        }

        Mesh mesh = new()
        {
            name = $"{preset.name}_Mesh",
            indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = sharedMaterial;

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        if (addColliders)
        {
            if (meshCollider == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
        else if (meshCollider != null)
        {
            Destroy(meshCollider);
        }
    }

    private Material CreateSharedVoxelMaterial(Material baseMaterial, float voxelAlpha)
    {
        Color color = baseMaterial.HasProperty("_BaseColor")
            ? baseMaterial.GetColor("_BaseColor")
            : baseMaterial.color;
        color.a = voxelAlpha;
        baseMaterial.color = color;
        baseMaterial.SetColor("_BaseColor", color);
        baseMaterial.SetColor("_Color", color);
        return baseMaterial;
    }

    private void AddVisibleVoxelFaces(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 origin,
        int x,
        int y,
        int z,
        float size)
    {
        Vector3 center = origin + new Vector3(x * size, y * size, z * size);
        Vector3 half = Vector3.one * (size * 0.5f);

        if (!IsFilled(x, y + 1, z)) AddFace(vertices, triangles, normals, center, half, Vector3.up);
        if (!IsFilled(x, y - 1, z)) AddFace(vertices, triangles, normals, center, half, Vector3.down);
        if (!IsFilled(x + 1, y, z)) AddFace(vertices, triangles, normals, center, half, Vector3.right);
        if (!IsFilled(x - 1, y, z)) AddFace(vertices, triangles, normals, center, half, Vector3.left);
        if (!IsFilled(x, y, z + 1)) AddFace(vertices, triangles, normals, center, half, Vector3.forward);
        if (!IsFilled(x, y, z - 1)) AddFace(vertices, triangles, normals, center, half, Vector3.back);
    }

    private bool IsFilled(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= preset.sizeX || y >= preset.sizeY || z >= preset.sizeZ)
            return false;

        return preset.IsFilled(x, y, z);
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 center,
        Vector3 half,
        Vector3 normal)
    {
        int start = vertices.Count;
        Vector3 right;
        Vector3 up;

        if (normal == Vector3.up || normal == Vector3.down)
        {
            right = Vector3.right * half.x;
            up = Vector3.forward * half.z;
        }
        else if (normal == Vector3.right || normal == Vector3.left)
        {
            right = Vector3.forward * half.z;
            up = Vector3.up * half.y;
        }
        else
        {
            right = Vector3.right * half.x;
            up = Vector3.up * half.y;
        }

        Vector3 faceCenter = center + Vector3.Scale(normal, half);
        vertices.Add(faceCenter - right - up);
        vertices.Add(faceCenter - right + up);
        vertices.Add(faceCenter + right + up);
        vertices.Add(faceCenter + right - up);

        if (normal == Vector3.down || normal == Vector3.left || normal == Vector3.forward)
        {
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);
        }
        else
        {
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
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
