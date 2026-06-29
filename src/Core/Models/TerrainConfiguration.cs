namespace Core.Models;

public sealed record TerrainConfiguration
{
    public int ChunkSize { get; init; } = 32;
    public int Seed { get; init; }

    public NoiseConfiguration Noise { get; set; } = new()
    {
        Scale = 2048
    };
}

public sealed record NoiseConfiguration
{
    public int Scale { get; init; }
}