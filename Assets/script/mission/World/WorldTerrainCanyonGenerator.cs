using UnityEngine;

public static class WorldTerrainCanyonGenerator
{
    public static float[,] Generate(
        WorldTerrainGenerationSettings settings,
        System.Random rng,
        float threshold,
        float minimumHeight)
    {
        float[,] heights = new float[settings.HeightmapResolution, settings.HeightmapResolution];
        float offsetX = rng.Next(-100000, 100000);
        float offsetY = rng.Next(-100000, 100000);
        float clampedThreshold = Mathf.Clamp01(threshold);
        float clampedMinimumHeight = Mathf.Clamp01(minimumHeight);

        for (int x = 0; x < settings.HeightmapResolution; x++)
        {
            for (int y = 0; y < settings.HeightmapResolution; y++)
            {
                float noiseHeight = WorldTerrainMode1Generator.GeneratePerlinNoiseHeight(x, y, offsetX, offsetY, settings);
                float height = Mathf.InverseLerp(-1f, 1f, noiseHeight);
                heights[x, y] = ApplyCanyonHeight(height, clampedThreshold, clampedMinimumHeight);
            }
        }

        return heights;
    }

    internal static float ApplyCanyonHeight(float height, float threshold, float minimumHeight)
    {
        if (height <= threshold)
        {
            float deviation = threshold - height;
            height = threshold - deviation * deviation;
            height = Mathf.Max(height, minimumHeight);
        }

        return Mathf.Clamp01(height);
    }
}
