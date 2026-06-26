using Core.Services;

namespace Core.Models;

public sealed class TerrainContext(TerrainGrid grid, TerrainConfiguration config, int chunkX, int chunkY)
{
    public TerrainGrid Grid { get; } = grid;
    public TerrainConfiguration Config { get; } = config;
    public int ChunkX { get; } = chunkX;
    public int ChunkY { get; } = chunkY;

    public int OffsetX => ChunkX * Config.ChunkSize;
    public int OffsetY => ChunkY * Config.ChunkSize;
}