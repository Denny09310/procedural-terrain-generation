namespace Core.Models;

public sealed record TerrainSettings(
    int Size,
    int Octaves,
    double Persistence,
    TerrainShape Shape,
    TerrainLayerSetting Elevation,
    TerrainLayerSetting Moisture,
    TerrainLayerSetting Temperature);
