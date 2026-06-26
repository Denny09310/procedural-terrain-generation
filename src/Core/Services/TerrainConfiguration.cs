namespace Core.Services;

public sealed class TerrainConfiguration
{
    public int ChunkSize { get; init; } = 32;
    public int WorldWidth { get; init; } = 2048;
    public int WorldHeight { get; init; } = 2048;
    public int Seed { get; init; }
}