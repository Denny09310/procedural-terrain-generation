namespace Core.Models;

public sealed record TerrainContext(
    TerrainConfiguration Configuration,
    TerrainGrid Grid,
    int ChunkX,
    int ChunkY)
{
    public int OffsetX => ChunkX * Configuration.ChunkSize;
    public int OffsetY => ChunkY * Configuration.ChunkSize;
}
