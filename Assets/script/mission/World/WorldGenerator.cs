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
    public int cloudParticleCount = 400;
    public float cloudHeight = 420f;
    public float cloudBaseRadius = 320f;
    public float cloudTopRadius = 220f;
    public float cloudBulge = 240f;
    public float cloudShapeScale = 10f;
    public float cloudParticleSizeScale = 5f;

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

            ParticleSystem particleSystem = cloud.AddComponent<ParticleSystem>();
            ConfigureCloudParticleSystem(particleSystem);

            CumulonimbusParticleCloud cloudShape = cloud.AddComponent<CumulonimbusParticleCloud>();
            cloudShape.particleCount = cloudParticleCount;
            cloudShape.height = cloudHeight * cloudShapeScale;
            cloudShape.baseRadius = cloudBaseRadius * cloudShapeScale;
            cloudShape.topRadius = cloudTopRadius * cloudShapeScale;
            cloudShape.bulge = cloudBulge * cloudShapeScale;
            cloudShape.minSize *= cloudParticleSizeScale;
            cloudShape.maxSize *= cloudParticleSizeScale;
            cloudShape.GenerateCloud();
        }
    }

    private float RandomRange(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)rng.NextDouble());
    }

    private void ConfigureCloudParticleSystem(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = cloudParticleCount;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer =
            particleSystem.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sharedMaterial = CreateCloudParticleMaterial();
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

    private Material generatedCloudParticleMaterial;
    private Texture2D generatedCloudParticleTexture;

    private Material CreateCloudParticleMaterial()
    {
        if (generatedCloudParticleMaterial != null)
        {
            return generatedCloudParticleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        generatedCloudParticleMaterial = new Material(shader);
        generatedCloudParticleMaterial.name = "GeneratedCloudParticleMaterial";

        Texture2D particleTexture = CreateCloudParticleTexture();
        generatedCloudParticleMaterial.mainTexture = particleTexture;
        generatedCloudParticleMaterial.SetTexture("_BaseMap", particleTexture);
        generatedCloudParticleMaterial.SetTexture("_MainTex", particleTexture);
        generatedCloudParticleMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.55f));
        generatedCloudParticleMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.55f));
        
        generatedCloudParticleMaterial.SetFloat("_Surface", 1f); // Transparent
        generatedCloudParticleMaterial.SetFloat("_Blend", 0f);   // Alpha
        generatedCloudParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        generatedCloudParticleMaterial.renderQueue =
            (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return generatedCloudParticleMaterial;
    }

    private Texture2D CreateCloudParticleTexture()
    {
        if (generatedCloudParticleTexture != null)
        {
            return generatedCloudParticleTexture;
        }

        const int textureSize = 64;
        generatedCloudParticleTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        generatedCloudParticleTexture.name = "GeneratedCloudParticleTexture";
        generatedCloudParticleTexture.wrapMode = TextureWrapMode.Clamp;
        generatedCloudParticleTexture.filterMode = FilterMode.Bilinear;

        float center = (textureSize - 1) * 0.5f;
        float radius = center;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.InverseLerp(0.68f, 1f, distance);
                alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(alpha));

                if (distance > 1f)
                {
                    alpha = 0f;
                }

                generatedCloudParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        generatedCloudParticleTexture.Apply();
        return generatedCloudParticleTexture;
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
