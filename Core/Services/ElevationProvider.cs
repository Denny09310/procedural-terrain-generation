using Core.Models;

namespace Core.Services;

public sealed class ElevationProvider(TerrainSettings settings)
{
    private readonly TerrainNoise _detailNoise = new(
        settings.Elevation.BlockSize,
        settings.Elevation.NoiseSeed);
    private readonly TerrainNoise _macroNoise = new(
        settings.Elevation.BlockSize * 10,
        settings.Elevation.NoiseSeed + 1337);

    public double GetElevation(int worldX, int worldY)
    {
        var octaves = settings.Octaves;
        var persistence = settings.Persistence;

        var detail = _detailNoise.SampleFractal(
            worldX,
            worldY,
            octaves,
            persistence);

        var macro = _macroNoise.SampleFractal(
            worldX,
            worldY,
            2,
            0.5);

        return settings.Shape switch
        {
            TerrainShape.Continent =>
                Math.Clamp(macro * 0.6 + detail * 0.4, 0, 1),

            TerrainShape.Archipelago =>
                Math.Clamp(detail * detail - 0.25 + macro * 0.20, 0, 1),

            TerrainShape.InlandSea =>
                Math.Clamp(macro * 0.55 + 0.25 + detail * 0.20, 0, 1),

            TerrainShape.Plains =>
                Math.Clamp(0.45 + detail * 0.15, 0, 1),

            TerrainShape.Mountains =>
                Math.Clamp(0.35 + Math.Abs(detail - 0.5) * 2.0 * 0.5 + macro * 0.15, 0, 1),

            _ => throw new NotImplementedException()
        };
    }
}