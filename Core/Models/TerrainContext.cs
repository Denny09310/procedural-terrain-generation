namespace Core.Models;

public sealed record TerrainContext(
    int Seed,
    int ChunkX,
    int ChunkY,
    TerrainSettings Settings);
