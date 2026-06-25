namespace Core.Models;

public sealed record TerrainSettings(
    int ChunkSize,
    int Octaves,
    double Persistence,
    TerrainShape Shape,
    TerrainLayer Elevation,
    TerrainLayer Moisture,
    TerrainLayer Temperature);
