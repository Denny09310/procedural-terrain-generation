using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public sealed class TemperatureTransformer(INoiseSource noise)
{
    public void Apply(TerrainGrid grid, TerrainContext ctx)
    {
        float invW = 1f / ctx.Config.WorldWidth;
        float invH = 1f / ctx.Config.WorldHeight;

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

            climate *= 0.5f + 0.5f;

            float lapse = 1f - MathF.Pow(MathF.Max(0f, cell.Elevation - 0.3f), 1.5f);
            cell.Temperature = Math.Clamp(climate * lapse, 0f, 1f);
        }
    }
}
