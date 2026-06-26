using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public sealed class ElevationTransformer(
    INoiseSource noise)
{
    public void Apply(TerrainGrid grid, TerrainContext ctx)
    {
        float invW = 1f / ctx.Config.WorldWidth;
        float invH = 1f / ctx.Config.WorldHeight;

        foreach (var cell in grid)
        {
            int wx = ctx.OffsetX + cell.X;
            int wy = ctx.OffsetY + cell.Y;

            float n = noise.SampleFractal(
                wx * invW, wy * invH,
                octaves: 8,
                frequency: 4f,
                persistence: 0.5f,
                lacunarity: 2f);

            float normalized = Math.Clamp((n + 1f) * 0.5f, 0f, 1f);
            cell.Elevation = MathF.Pow(normalized, 1.7f);
        }
    }
}