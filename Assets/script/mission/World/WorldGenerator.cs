using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed = 12345;
    public bool usePlayerPrefsSeed = true;

    [Header("Replace Existing Terrain")]
    public bool disableExistingTerrains = true;
    public bool generateOnStart = true;

    [Header("Terrain Settings")]
    public int terrainSize = 6000;
    public int heightmapResolution = 1025;
    public float heightScale = 600f;
    public float noiseScale = 0.006f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

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

    public Terrain terrain;
    public bool isgenerating = false;

    private System.Random rng;

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
            if (sceneTerrain != null && sceneTerrain != terrain)
            {
                sceneTerrain.gameObject.SetActive(false);
            }
        }
    }

    private void CreateTerrain()
    {
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            size = new Vector3(terrainSize, heightScale, terrainSize)
        };

        terrainData.SetHeights(0, 0, GenerateHeights());

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "GeneratedTerrain";
        terrainObj.transform.position = new Vector3(-terrainSize * 0.5f, 0f, -terrainSize * 0.5f);
        terrain = terrainObj.GetComponent<Terrain>();
    }

    private float[,] GenerateHeights()
    {
        float[,] heights = new float[heightmapResolution, heightmapResolution];
        float offsetX = rng.Next(-100000, 100000);
        float offsetY = rng.Next(-100000, 100000);

        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + offsetX) * noiseScale * frequency;
                    float sampleY = (y + offsetY) * noiseScale * frequency;
                    float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                    noiseHeight += perlin * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[x, y] = Mathf.InverseLerp(-1f, 1f, noiseHeight);
            }
        }

        return heights;
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
        if (terrain != null)
        {
            terrain.materialTemplate = CreateTerrainMaterial();
        }

        if (water != null)
        {
            water.GetComponent<Renderer>().material = CreateWaterMaterial();
        }
    }
}
