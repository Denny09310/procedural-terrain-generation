using Core.Models;

namespace Viewer.Helpers;

internal static class Normalization
{
    internal static void Normalize(
        TerrainGrid grid,
        Func<TerrainCell, float> get,
        Action<TerrainCell, float> set)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var cell in grid)
        {
            float v = get(cell);
            if (v < min) min = v;
            if (v > max) max = v;
        }

        float range = max - min;
        if (range < float.Epsilon) return;

        foreach (var cell in grid)
            set(cell, (get(cell) - min) / range);
    }
}