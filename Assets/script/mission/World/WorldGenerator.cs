using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    private enum TerrainGenerationMode
    {
        Mode1,
        Canyon
    }

    [Header("Seed")]
    public int seed = 12345;
    public bool usePlayerPrefsSeed = true;

    [Header("Replace Existing Terrain")]
    public bool disableExistingTerrains = true;
    public bool generateOnStart = true;

    [Header("Terrain Settings")]
    public int terrainSize = 12000;
    public int heightmapResolution = 1025;
    public float heightScale = 600f;
    public float noiseScale = 0.006f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Canyon Mode")]
    [Range(0f, 1f)] public float canyonThreshold = 0.45f;
    [Range(0f, 1f)] public float canyonMinimumHeight = 0.08f;

    [Header("Block Terrain")]
    [Min(1)] public int blockSampleStep = 10;
    [Min(1)] public int blockHeightSteps = 10;
    public float blockBaseHeight = 0f;
    public float blockSideThickness = 1f;

    [Header("Water")]
    public GameObject water;
    public float waterHeight = 8f;

    [Header("Boundary")]
    public float boundaryOffset = 2f;
    public float ceilingHeight = 2000f;

    [Header("Clouds")]
    public bool generateClouds = true;
    public int cloudCount = 12;
    public float cloudMinAltitude = 900f;
    public float cloudMaxAltitude = 1600f;
    public float cloudAreaPadding = 150f;
    public int cloudSizeX = 18;
    public int cloudSizeY = 26;
    public int cloudSizeZ = 18;
    public float cloudCellSize = 55f;
    public int cloudCellularSteps = 4;
    [Range(0.05f, 0.95f)] public float cloudInitialFillRate = 0.42f;
    [Range(0f, 1f)] public float cloudAlpha = 0.3f;
    [Range(0.25f, 8f)] public float cloudAngleFadePower = 2f;
    [Range(0f, 1f)] public float cloudMinAngleAlphaFactor = 0.12f;

    [Header("Color Control")]
    public float playerHueShift = 0f;
    public float playerSaturation = 0.8f;
    public float playerValue = 0.6f;
    public float randomHueRange = 0.1f;

    public float[,] blockHeights;
    public bool isgenerating = false;

    private System.Random rng;
    private GameObject blockTerrainRoot;
    private float blockSize;

    private void Start()
    {
        if (!generateOnStart)
        {
            return;
        }

        if (usePlayerPrefsSeed)
        {
            seed = PlayerPrefs.GetInt("world_seed", seed);
        }

        GenerateWorld();
    }

    public void GenerateWorld()
    {
        rng = new System.Random(seed);
        isgenerating = false;

        if (disableExistingTerrains)
        {
            DisableExistingTerrains();
        }

        CreateTerrain();
        CreateWater();
        CreateBoundaries();

        if (generateClouds)
        {
            CreateClouds();
        }

        RefreshMaterials();
        isgenerating = true;
    }

    private void DisableExistingTerrains()
    {
        foreach (Terrain sceneTerrain in Terrain.activeTerrains)
        {
            if (sceneTerrain == null)
            {
                continue;
            }

            Destroy(sceneTerrain.gameObject);
        }
    }

    private void CreateTerrain()
    {
        float[,] heights = GenerateHeights();
        CreateBlockTerrain(heights);
    }

    private float[,] GenerateHeights()
    {
        WorldTerrainGenerationSettings settings = new WorldTerrainGenerationSettings(
            heightmapResolution,
            noiseScale,
            octaves,
            persistence,
            lacunarity
        );

        switch (GetTerrainGenerationModeForActiveScene())
        {
            case TerrainGenerationMode.Canyon:
                return WorldTerrainCanyonGenerator.Generate(settings, rng, canyonThreshold, canyonMinimumHeight);
            case TerrainGenerationMode.Mode1:
            default:
                return WorldTerrainMode1Generator.Generate(settings, rng);
        }
    }

    private TerrainGenerationMode GetTerrainGenerationModeForActiveScene()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "M01":
            case "M02":
            case "M03":
                return TerrainGenerationMode.Mode1;
            case "Canyon":
            case "M04":
                return TerrainGenerationMode.Canyon;
            default:
                return TerrainGenerationMode.Mode1;
        }
    }

    private void CreateBlockTerrain(float[,] sourceHeights)
    {
        if (blockTerrainRoot != null)
        {
            Destroy(blockTerrainRoot);
        }

        int step = Mathf.Max(1, blockSampleStep);
        int sampleCount = Mathf.Max(2, Mathf.CeilToInt((heightmapResolution - 1f) / step) + 1);
        blockHeights = new float[sampleCount, sampleCount];
        blockSize = terrainSize / (sampleCount - 1f);

        blockTerrainRoot = new GameObject("GeneratedBlockTerrain");
        blockTerrainRoot.AddComponent<WorldBlockTerrainCollider>();

        for (int z = 0; z < sampleCount; z++)
        {
            for (int x = 0; x < sampleCount; x++)
            {
                int sourceX = Mathf.Min(x * step, heightmapResolution - 1);
                int sourceZ = Mathf.Min(z * step, heightmapResolution - 1);
                float roundedHeight = RoundHeight(sourceHeights[sourceX, sourceZ]);
                blockHeights[x, z] = roundedHeight * heightScale;
            }
        }

        BuildBlockTerrainMesh(sampleCount, CreateTerrainMaterial());
    }

    public bool TryGetBlockHeight(Vector3 worldPosition, out float height)
    {
        height = 0f;
        if (blockHeights == null || blockHeights.Length == 0 || blockSize <= 0f)
        {
            return false;
        }

        float half = terrainSize * 0.5f;
        float localX = worldPosition.x + half;
        float localZ = worldPosition.z + half;
        if (localX < 0f || localZ < 0f || localX > terrainSize || localZ > terrainSize)
        {
            return false;
        }

        int maxX = blockHeights.GetLength(0) - 1;
        int maxZ = blockHeights.GetLength(1) - 1;
        int x = Mathf.Clamp(Mathf.RoundToInt(localX / blockSize), 0, maxX);
        int z = Mathf.Clamp(Mathf.RoundToInt(localZ / blockSize), 0, maxZ);
        height = blockHeights[x, z];
        return true;
    }

    private float RoundHeight(float normalizedHeight)
    {
        int steps = Mathf.Max(1, blockHeightSteps);
        return Mathf.Clamp01(Mathf.Round(normalizedHeight * steps) / steps);
    }

    private Vector3 GetBlockCenter(int x, int z, float blockSize)
    {
        float half = terrainSize * 0.5f;
        return new Vector3(
            -half + x * blockSize,
            0f,
            -half + z * blockSize
        );
    }

    private void CreateBlockColumn(int x, int z, float height, float blockSize, Material material)
    {
        float bottom = blockBaseHeight;
        float columnHeight = Mathf.Max(0.01f, height - bottom);
        GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cube);
        column.name = $"BlockTerrain_Column_{x:000}_{z:000}";
        column.AddComponent<WorldBlockTerrainCollider>();
        column.transform.SetParent(blockTerrainRoot.transform, false);
        column.transform.position = GetBlockCenter(x, z, blockSize) + Vector3.up * (bottom + columnHeight * 0.5f);
        column.transform.localScale = new Vector3(blockSize, columnHeight, blockSize);
        column.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void BuildBlockTerrainMesh(int sampleCount, Material material)
    {
        List<Vector3> vertices = new(sampleCount * sampleCount * 12);
        List<int> triangles = new(sampleCount * sampleCount * 18);
        List<Vector3> normals = new(sampleCount * sampleCount * 12);

        for (int z = 0; z < sampleCount; z++)
        {
            for (int x = 0; x < sampleCount; x++)
            {
                AddBlockColumnMesh(vertices, triangles, normals, x, z, sampleCount);
            }
        }

        Mesh mesh = new()
        {
            name = "GeneratedBlockTerrainMesh",
            indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();

        MeshFilter meshFilter = blockTerrainRoot.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = blockTerrainRoot.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        MeshCollider meshCollider = blockTerrainRoot.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void AddBlockColumnMesh(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        int x,
        int z,
        int sampleCount)
    {
        float top = Mathf.Max(blockBaseHeight + 0.01f, blockHeights[x, z]);
        float bottom = blockBaseHeight;
        Vector3 center = GetBlockCenter(x, z, blockSize);
        float half = blockSize * 0.5f;

        float minX = center.x - half;
        float maxX = center.x + half;
        float minZ = center.z - half;
        float maxZ = center.z + half;

        AddDoubleSidedQuad(vertices, triangles, normals,
            new Vector3(minX, top, minZ),
            new Vector3(minX, top, maxZ),
            new Vector3(maxX, top, maxZ),
            new Vector3(maxX, top, minZ),
            Vector3.up);

        AddSideIfExposed(vertices, triangles, normals, x, z, sampleCount, top, bottom, minX, maxX, minZ, maxZ, Vector3.left);
        AddSideIfExposed(vertices, triangles, normals, x, z, sampleCount, top, bottom, minX, maxX, minZ, maxZ, Vector3.right);
        AddSideIfExposed(vertices, triangles, normals, x, z, sampleCount, top, bottom, minX, maxX, minZ, maxZ, Vector3.back);
        AddSideIfExposed(vertices, triangles, normals, x, z, sampleCount, top, bottom, minX, maxX, minZ, maxZ, Vector3.forward);
    }

    private void AddSideIfExposed(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        int x,
        int z,
        int sampleCount,
        float top,
        float bottom,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        Vector3 direction)
    {
        int nx = x + Mathf.RoundToInt(direction.x);
        int nz = z + Mathf.RoundToInt(direction.z);
        float sideBottom = bottom;

        if (nx >= 0 && nz >= 0 && nx < sampleCount && nz < sampleCount)
        {
            sideBottom = Mathf.Max(bottom, blockHeights[nx, nz]);
            if (sideBottom >= top - 0.01f)
                return;
        }

        if (direction == Vector3.left)
        {
            AddDoubleSidedQuad(vertices, triangles, normals,
                new Vector3(minX, sideBottom, maxZ),
                new Vector3(minX, top, maxZ),
                new Vector3(minX, top, minZ),
                new Vector3(minX, sideBottom, minZ),
                Vector3.left);
        }
        else if (direction == Vector3.right)
        {
            AddDoubleSidedQuad(vertices, triangles, normals,
                new Vector3(maxX, sideBottom, minZ),
                new Vector3(maxX, top, minZ),
                new Vector3(maxX, top, maxZ),
                new Vector3(maxX, sideBottom, maxZ),
                Vector3.right);
        }
        else if (direction == Vector3.back)
        {
            AddDoubleSidedQuad(vertices, triangles, normals,
                new Vector3(minX, sideBottom, minZ),
                new Vector3(minX, top, minZ),
                new Vector3(maxX, top, minZ),
                new Vector3(maxX, sideBottom, minZ),
                Vector3.back);
        }
        else
        {
            AddDoubleSidedQuad(vertices, triangles, normals,
                new Vector3(maxX, sideBottom, maxZ),
                new Vector3(maxX, top, maxZ),
                new Vector3(minX, top, maxZ),
                new Vector3(minX, sideBottom, maxZ),
                Vector3.forward);
        }
    }

    private static void AddDoubleSidedQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector3 normal)
    {
        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);

        start = vertices.Count;
        vertices.Add(a);
        vertices.Add(d);
        vertices.Add(c);
        vertices.Add(b);
        normals.Add(-normal);
        normals.Add(-normal);
        normals.Add(-normal);
        normals.Add(-normal);
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private void CreateHeightGapFill(
        string objectName,
        Vector3 currentCenter,
        Vector3 neighborCenter,
        float currentHeight,
        float neighborHeight,
        bool xAxisGap,
        float blockSize,
        Material material)
    {
        float heightDifference = Mathf.Abs(currentHeight - neighborHeight);
        if (heightDifference <= 0.01f)
        {
            return;
        }

        float lowerHeight = Mathf.Min(currentHeight, neighborHeight);
        float thickness = Mathf.Clamp(blockSideThickness, 0.01f, blockSize);
        Vector3 position = (currentCenter + neighborCenter) * 0.5f;
        position.y = lowerHeight + heightDifference * 0.5f;

        Vector3 scale = xAxisGap
            ? new Vector3(thickness, heightDifference, blockSize)
            : new Vector3(blockSize, heightDifference, thickness);

        GameObject filler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        filler.name = objectName;
        filler.AddComponent<WorldBlockTerrainCollider>();
        filler.transform.SetParent(blockTerrainRoot.transform, false);
        filler.transform.position = position;
        filler.transform.localScale = scale;
        filler.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void CreateWater()
    {
        water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "GeneratedWater";
        water.layer = LayerMask.NameToLayer("Water");
        water.AddComponent<WorldWaterCollider>();
        water.transform.localScale = new Vector3(terrainSize / 10f, 1f, terrainSize / 10f);
        water.transform.position = new Vector3(0f, waterHeight, 0f);
    }

    private void CreateBoundaries()
    {
        float half = terrainSize * 0.5f;
        float innerSize = terrainSize - boundaryOffset * 2f;

        CreateBoundary("GeneratedBoundary_North", new Vector3(0f, ceilingHeight * 0.5f, half - boundaryOffset), new Vector3(innerSize, ceilingHeight, 1f));
        CreateBoundary("GeneratedBoundary_South", new Vector3(0f, ceilingHeight * 0.5f, -half + boundaryOffset), new Vector3(innerSize, ceilingHeight, 1f));
        CreateBoundary("GeneratedBoundary_East", new Vector3(half - boundaryOffset, ceilingHeight * 0.5f, 0f), new Vector3(1f, ceilingHeight, innerSize));
        CreateBoundary("GeneratedBoundary_West", new Vector3(-half + boundaryOffset, ceilingHeight * 0.5f, 0f), new Vector3(1f, ceilingHeight, innerSize));
        CreateBoundary("GeneratedBoundary_Ceil", new Vector3(0f, ceilingHeight, 0f), new Vector3(innerSize, 1f, innerSize));
        CreateBoundary("GeneratedBoundary_Floor", new Vector3(0f, -0.5f, 0f), new Vector3(innerSize, 1f, innerSize));
    }

    private void CreateBoundary(string objectName, Vector3 position, Vector3 size)
    {
        GameObject boundary = new GameObject(objectName);
        boundary.AddComponent<WorldBoundaryCollider>();

        BoxCollider boxCollider = boundary.AddComponent<BoxCollider>();
        boxCollider.size = size;
        boundary.transform.position = position;
    }

    private void CreateClouds()
    {
        float half = terrainSize * 0.5f - cloudAreaPadding;
        if (half <= 0f)
        {
            return;
        }

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloud = new GameObject($"GeneratedCloud_{i + 1:00}");
            cloud.transform.position = new Vector3(
                RandomRange(-half, half),
                RandomRange(cloudMinAltitude, cloudMaxAltitude),
                RandomRange(-half, half)
            );

            int cloudSeed = seed + 10000 + i * 997;
            CloudCellularGenerator cloudGenerator = new CloudCellularGenerator(cloudSeed);
            CloudPreset preset = cloudGenerator.GenerateCumulonimbus(
                $"Cumulonimbus_{i + 1:00}",
                Mathf.Max(3, cloudSizeX),
                Mathf.Max(3, cloudSizeY),
                Mathf.Max(3, cloudSizeZ),
                Mathf.Max(1f, cloudCellSize),
                Mathf.Max(0, cloudCellularSteps),
                cloudInitialFillRate
            );

            CloudVoxelRenderer renderer = cloud.AddComponent<CloudVoxelRenderer>();
            renderer.alpha = cloudAlpha;
            renderer.Render(
                preset,
                CloudMaterialFactory.CreateVoxelCloudMaterial(
                    new Color(1f, 1f, 1f, cloudAlpha),
                    cloudAngleFadePower,
                    cloudMinAngleAlphaFactor
                )
            );
        }
    }

    private float RandomRange(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    private Color GenerateColor(System.Random colorRng)
    {
        float baseHue = (float)colorRng.NextDouble();
        float randomOffset = ((float)colorRng.NextDouble() * 2f - 1f) * randomHueRange;
        float finalHue = Mathf.Repeat(baseHue + randomOffset + playerHueShift, 1f);

        return Color.HSVToRGB(
            finalHue,
            Mathf.Clamp01(playerSaturation),
            Mathf.Clamp01(playerValue)
        );
    }

    private Material CreateTerrainMaterial()
    {
        Shader shader = FindWorldShader();
        Material mat = new Material(shader);

        System.Random colorRng = new System.Random(seed + 1000);
        mat.SetColor("_BaseColor", GenerateColor(colorRng));
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    private Material CreateWaterMaterial()
    {
        Shader shader = FindWorldShader();
        Material mat = new Material(shader);

        System.Random colorRng = new System.Random(seed + 2000);
        float hue = 0.55f + ((float)colorRng.NextDouble() - 0.5f) * 0.1f;
        Color waterColor = Color.HSVToRGB(hue, 0.8f, 0.6f);
        waterColor.a = 0.6f;

        mat.SetColor("_BaseColor", waterColor);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_Smoothness", 0.9f);
        mat.SetFloat("_Metallic", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    private Shader FindWorldShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        return shader;
    }

    private void ConfigureTransparentAlphaMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Mode", 2f);
        material.SetFloat("_AlphaClip", 0f);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    public void RefreshMaterials()
    {
        if (water != null)
        {
            water.GetComponent<Renderer>().material = CreateWaterMaterial();
        }
    }
}
