using UnityEngine;

public static class WorldTerrainMode1Generator
{
    public static float[,] Generate(WorldTerrainGenerationSettings settings, System.Random rng)
    {
        float[,] heights = new float[settings.HeightmapResolution, settings.HeightmapResolution];
        float offsetX = rng.Next(-100000, 100000);
        float offsetY = rng.Next(-100000, 100000);

        for (int x = 0; x < settings.HeightmapResolution; x++)
        {
            for (int y = 0; y < settings.HeightmapResolution; y++)
            {
                float noiseHeight = GeneratePerlinNoiseHeight(x, y, offsetX, offsetY, settings);
                heights[x, y] = Mathf.InverseLerp(-1f, 1f, noiseHeight);
            }
        }

        return heights;
    }

    internal static float GeneratePerlinNoiseHeight(
        int x,
        int y,
        float offsetX,
        float offsetY,
        WorldTerrainGenerationSettings settings)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < settings.Octaves; i++)
        {
            float sampleX = (x + offsetX) * settings.NoiseScale * frequency;
            float sampleY = (y + offsetY) * settings.NoiseScale * frequency;
            float perlin = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

            noiseHeight += perlin * amplitude;
            amplitude *= settings.Persistence;
            frequency *= settings.Lacunarity;
        }

        return noiseHeight;
    }
}
