public readonly struct WorldTerrainGenerationSettings
{
    public WorldTerrainGenerationSettings(
        int heightmapResolution,
        float noiseScale,
        int octaves,
        float persistence,
        float lacunarity)
    {
        HeightmapResolution = heightmapResolution;
        NoiseScale = noiseScale;
        Octaves = octaves;
        Persistence = persistence;
        Lacunarity = lacunarity;
    }

    public int HeightmapResolution { get; }
    public float NoiseScale { get; }
    public int Octaves { get; }
    public float Persistence { get; }
    public float Lacunarity { get; }
}
