namespace Core.Models;

public sealed record TerrainSettings(
    int ChunkSize,
    int Octaves,
    double Persistence,
    TerrainShape Shape,
    TerrainLayerSetting Elevation,
    TerrainLayerSetting Moisture,
    TerrainLayerSetting Temperature);
