using UnityEngine;

public class CloudCellularGenerator
{
    private readonly System.Random rng;

    public CloudCellularGenerator(int seed)
    {
        rng = new System.Random(seed);
    }

    public CloudPreset GenerateCumulonimbus(
        string presetName,
        int sizeX,
        int sizeY,
        int sizeZ,
        float cellSize,
        int steps,
        float fillRate)
    {
        CloudPreset preset = new CloudPreset(presetName, sizeX, sizeY, sizeZ, cellSize);
        SeedCloud(preset, Mathf.Clamp01(fillRate));

        for (int i = 0; i < steps; i++)
        {
            Step(preset);
        }

        return preset;
    }

    private void SeedCloud(CloudPreset preset, float fillRate)
    {
        float noiseOffsetX = RandomOffset();
        float noiseOffsetY = RandomOffset();
        float noiseOffsetZ = RandomOffset();
        Vector3 center = new Vector3(
            (preset.sizeX - 1) * 0.5f,
            (preset.sizeY - 1) * 0.38f,
            (preset.sizeZ - 1) * 0.5f
        );

        for (int z = 0; z < preset.sizeZ; z++)
        {
            for (int y = 0; y < preset.sizeY; y++)
            {
                for (int x = 0; x < preset.sizeX; x++)
                {
                    float t = preset.sizeY <= 1 ? 0f : (float)y / (preset.sizeY - 1);
                    float radius = GetLayerRadius(preset, t);
                    float dx = (x - center.x) / radius;
                    float dz = (z - center.z) / radius;
                    float horizontal = Mathf.Sqrt(dx * dx + dz * dz);
                    float vertical = Mathf.Abs(y - center.y) / Mathf.Max(1f, preset.sizeY * 0.52f);
                    float body = 1f - Mathf.Clamp01(horizontal * 0.85f + vertical * 0.45f);
                    float noise = Noise3D((x + noiseOffsetX) * 0.14f, (y + noiseOffsetY) * 0.14f, (z + noiseOffsetZ) * 0.14f);
                    float chance = body * Mathf.Lerp(1.25f, 0.75f, t) + noise * 0.35f;

                    if (chance > 1f - fillRate)
                    {
                        preset.SetFilled(x, y, z, true);
                    }
                }
            }
        }
    }

    public void Step(CloudPreset preset)
    {
        bool[,,] next = new bool[preset.sizeX, preset.sizeY, preset.sizeZ];

        for (int z = 0; z < preset.sizeZ; z++)
        {
            for (int y = 0; y < preset.sizeY; y++)
            {
                for (int x = 0; x < preset.sizeX; x++)
                {
                    int neighbors = CountFilledNeighbors(preset, x, y, z);
                    bool current = preset.IsFilled(x, y, z);
                    float t = preset.sizeY <= 1 ? 0f : (float)y / (preset.sizeY - 1);
                    float layerBias = GetLayerGrowthBias(t, x, z, preset);
                    bool survives = current && neighbors >= Mathf.RoundToInt(Mathf.Lerp(5f, 9f, t));
                    bool grows = !current && neighbors >= Mathf.RoundToInt(Mathf.Lerp(7f, 11f, 1f - layerBias));

                    if (survives || grows)
                    {
                        next[x, y, z] = true;
                    }
                }
            }
        }

        preset.cells = next;
    }

    public int CountFilledNeighbors(CloudPreset preset, int cellX, int cellY, int cellZ)
    {
        int count = 0;

        for (int z = -1; z <= 1; z++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0 && z == 0)
                    {
                        continue;
                    }

                    if (preset.IsFilled(cellX + x, cellY + y, cellZ + z))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private float GetLayerRadius(CloudPreset preset, float t)
    {
        float halfX = preset.sizeX * 0.5f;
        float halfZ = preset.sizeZ * 0.5f;
        float baseRadius = Mathf.Min(halfX, halfZ) * 0.72f;
        float trunkRadius = Mathf.Min(halfX, halfZ) * 0.42f;
        float anvilRadius = Mathf.Min(halfX, halfZ) * 0.86f;

        if (t < 0.25f)
        {
            return Mathf.Lerp(baseRadius, trunkRadius, t / 0.25f);
        }

        if (t < 0.72f)
        {
            return Mathf.Lerp(trunkRadius, trunkRadius * 1.12f, (t - 0.25f) / 0.47f);
        }

        return Mathf.Lerp(trunkRadius * 1.12f, anvilRadius, (t - 0.72f) / 0.28f);
    }

    private float GetLayerGrowthBias(float t, int x, int z, CloudPreset preset)
    {
        float centerX = (preset.sizeX - 1) * 0.5f;
        float centerZ = (preset.sizeZ - 1) * 0.5f;
        float radius = GetLayerRadius(preset, t);
        float horizontal = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ)) / Mathf.Max(1f, radius);
        float centerBias = 1f - Mathf.Clamp01(horizontal);

        if (t < 0.25f)
        {
            return Mathf.Lerp(1.15f, 0.9f, horizontal);
        }

        if (t < 0.72f)
        {
            return Mathf.Lerp(1.35f, 0.65f, horizontal) * Mathf.Lerp(0.95f, 1.15f, t);
        }

        return Mathf.Lerp(1.15f, 0.75f, horizontal) + centerBias * 0.15f;
    }

    private float Noise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float yz = Mathf.PerlinNoise(y, z);
        float xz = Mathf.PerlinNoise(x, z);
        return (xy + yz + xz) / 3f;
    }

    private float RandomOffset()
    {
        return (float)rng.NextDouble() * 1000f;
    }
}
