using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public sealed class TemperatureTransformer(ITerrainNoise noise)
{
    public void Apply(TerrainGrid grid, TerrainContext ctx)
    {
        int scale = ctx.Configuration.Noise.Scale;

        float invW = 1f / scale;
        float invH = 1f / scale;

        foreach (var cell in grid)
        {
            int wx = ctx.OffsetX + cell.X;
            int wy = ctx.OffsetY + cell.Y;

            float climate = noise.SampleFractal(
                wx * invW, wy * invH,
                octaves: 5,
                frequency: 0.8f,
                persistence: 0.5f,
                lacunarity: 2f);

            climate = climate * 0.5f + 0.5f;

            float altitude = MathF.Max(0f, cell.Elevation - 0.35f) / 0.65f;

            float cooling = altitude * altitude * 0.45f;

            cell.Temperature = Math.Clamp(climate - cooling, 0f, 1f);
        }
    }
}
