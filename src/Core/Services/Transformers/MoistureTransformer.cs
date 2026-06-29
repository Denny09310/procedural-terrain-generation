using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public sealed class MoistureTransformer(ITerrainNoise noise)
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

            float baseNoise = noise.SampleFractal(
                wx * invW + 500f, wy * invH + 500f,
                octaves: 6,
                frequency: 3f,
                persistence: 0.6f,
                lacunarity: 2f);

            float normalizedNoise = Math.Clamp((baseNoise + 1f) * 0.5f, 0f, 1f);
            float rain = 1f - MathF.Pow(cell.Elevation, 2f) * 0.6f;
            cell.Moisture = normalizedNoise * MathF.Max(0f, rain);
        }
    }
}
