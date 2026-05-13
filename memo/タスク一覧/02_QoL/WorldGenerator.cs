using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed = 12345;

    [Header("Terrain Settings")]
    public int terrainSize = 40000;
    public int heightmapResolution = 1025;
    public float heightScale = 600f;
    public float noiseScale = 0.006f;

    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Water")]
    public GameObject water;
    public float waterHeight = 8f;

    public System.Random rng;
    public Terrain terrain;

    public bool isgenerating = false;

    void Start()
    {
        seed = PlayerPrefs.GetInt("world_seed", seed);
        GenerateWorld();
        isgenerating = true;
    }

    public void GenerateWorld()
    {
        rng = new System.Random(seed);

        CreateTerrain();
        CreateWater();
        SettingInvisibleWall();
        RefreshMaterials();
    }

    void SettingInvisibleWall()
    {
        float offset = 2f; // �ǂ��n�`�̒[���班�������悤�ɂ��邽�߂̃I�t�Z�b�g
        float ceilHeight = 1000f; // �\���ȍ�����m��
        GameObject invisibleWall = new GameObject("InvisibleWall_north");
        invisibleWall.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider = invisibleWall.AddComponent<BoxCollider>();
        collider.size = new Vector3(terrainSize - offset * 2, ceilHeight, 1);
        collider.center = new Vector3(terrainSize / 2f, ceilHeight / 2f, terrainSize - offset);

        GameObject invisibleWall2 = new GameObject("InvisibleWall_south");
        invisibleWall2.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider2 = invisibleWall2.AddComponent<BoxCollider>();
        collider2.size = new Vector3(terrainSize - offset * 2, ceilHeight, 1);
        collider2.center = new Vector3(terrainSize / 2f, ceilHeight / 2f, offset);

        GameObject invisibleWall3 = new GameObject("InvisibleWall_east");
        invisibleWall3.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider3 = invisibleWall3.AddComponent<BoxCollider>();
        collider3.size = new Vector3(1, ceilHeight, terrainSize - offset * 2);
        collider3.center = new Vector3(terrainSize - offset, ceilHeight / 2f, terrainSize / 2f);

        GameObject invisibleWall4 = new GameObject("InvisibleWall_west");
        invisibleWall4.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider4 = invisibleWall4.AddComponent<BoxCollider>();
        collider4.size = new Vector3(1, ceilHeight, terrainSize - offset * 2);
        collider4.center = new Vector3(offset, ceilHeight / 2f, terrainSize / 2f);

        GameObject invisibleceil = new GameObject("InvisibleWall_ceil");
        invisibleceil.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider5 = invisibleceil.AddComponent<BoxCollider>();
        collider5.size = new Vector3(terrainSize - offset * 2, 1, terrainSize - offset * 2);
        collider5.center = new Vector3(terrainSize / 2f, ceilHeight, terrainSize / 2f);

        GameObject invisiblefloor = new GameObject("InvisibleWall_floor");
        invisiblefloor.AddComponent<WorldBoundaryCollider>();
        BoxCollider collider6 = invisiblefloor.AddComponent<BoxCollider>();
        collider6.size = new Vector3(terrainSize - offset * 2, 1, terrainSize - offset * 2);
        collider6.center = new Vector3(terrainSize / 2f, -0.5f, terrainSize / 2f);


    }

    void CreateTerrain()
    {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(terrainSize, heightScale, terrainSize);

        float[,] heights = GenerateHeights();

        terrainData.SetHeights(0, 0, heights);

        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "GeneratedTerrain";
        terrain = terrainObj.GetComponent<Terrain>();

        // TerrainCollider�͎����ŕt��

#if UNITY_EDITOR
        DisableScenePicking(terrainObj);
#endif
    }
    float[,] GenerateHeights()
    {
        float[,] heights = new float[heightmapResolution, heightmapResolution];

        float offsetX = rng.Next(-100000, 100000);
        float offsetY = rng.Next(-100000, 100000);

        for (int x = 0; x < heightmapResolution; x++)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + offsetX) * noiseScale * frequency;
                    float sampleY = (y + offsetY) * noiseScale * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlin * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[x, y] = Mathf.InverseLerp(-1, 1, noiseHeight);
            }
        }

        return heights;
    }
    void CreateWater()
    {
        water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.layer = LayerMask.NameToLayer("Water");
        water.AddComponent<WorldWaterCollider>();

        water.transform.localScale = new Vector3(
            terrainSize / 10f,
            1,
            terrainSize / 10f);

        water.transform.position = new Vector3(
            terrainSize / 2f,
            waterHeight,
            terrainSize / 2f);

#if UNITY_EDITOR
        DisableScenePicking(water);
#endif
    }


    [Header("Color Control")]
    public float playerHueShift = 0f;        // -1�`1
    public float playerSaturation = 0.8f;    // 0�`1
    public float playerValue = 0.6f;         // 0�`1
    public float randomHueRange = 0.1f;      // seed�h�炬��
    Color GenerateColor(System.Random rng)
    {
        float baseHue = (float)rng.NextDouble();  // 0�`1
        float randomOffset = ((float)rng.NextDouble() * 2f - 1f) * randomHueRange;

        float finalHue = Mathf.Repeat(baseHue + randomOffset + playerHueShift, 1f);

        return Color.HSVToRGB(
            finalHue,
            Mathf.Clamp01(playerSaturation),
            Mathf.Clamp01(playerValue)
        );
    }
    Material CreateTerrainMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);

        System.Random rng = new System.Random(seed + 1000);
        Color terrainColor = GenerateColor(rng);

        mat.SetColor("_BaseColor", terrainColor);
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }
    Material CreateWaterMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader);

        System.Random rng = new System.Random(seed + 2000);

        float hue = 0.55f + ((float)rng.NextDouble() - 0.5f) * 0.1f; // �n���S
        Color waterColor = Color.HSVToRGB(hue, 0.8f, 0.6f);
        waterColor.a = 0.6f;

        mat.SetColor("_BaseColor", waterColor);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_Smoothness", 0.9f);
        mat.SetFloat("_Metallic", 0f);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    public void RefreshMaterials()
    {

        if (terrain != null)
            terrain.materialTemplate = CreateTerrainMaterial();
        if (water != null)
            water.GetComponent<Renderer>().material = CreateWaterMaterial();
    }

#if UNITY_EDITOR
    void DisableScenePicking(GameObject obj)
    {
        SceneVisibilityManager.instance.DisablePicking(obj, true);
    }
#endif


}

