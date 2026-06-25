using System.Collections.Concurrent;
using Core.Models;

namespace Core.Services;

public sealed class RiversProvider(
    TerrainSeed seed,
    TerrainSettings settings,
    ElevationProvider elevation)
{
    private const int Margin = 64;

    private readonly ConcurrentDictionary<(int, int), byte> _sources = new();

    private readonly ConcurrentDictionary<(int, int), byte> _regions = new();

    private readonly ConcurrentDictionary<(int, int), double> _cache = new();


    public double GetRiver(int worldX, int worldY)
    {
        EnsureRegion(worldX, worldY);

        return _cache.GetValueOrDefault((worldX, worldY));
    }

    private void EnsureRegion(int worldX, int worldY)
    {
        int regionX = RegionOf(worldX);
        int regionY = RegionOf(worldY);

        if (!_regions.TryAdd((regionX, regionY), 0))
            return;

        GenerateRegion(regionX, regionY);
    }

    private void GenerateRegion(int regionX, int regionY)
    {
        int minX = regionX * settings.ChunkSize - Margin;
        int minY = regionY * settings.ChunkSize - Margin;

        int maxX = minX + settings.ChunkSize + Margin * 2;
        int maxY = minY + settings.ChunkSize + Margin * 2;

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                TrySpawnRiver(x, y);
            }
        }
    }


    private void TrySpawnRiver(int x, int y)
    {
        if (!_sources.TryAdd((x, y), 0))
            return;

        double e = elevation.GetElevation(x, y);

        if (e < 0.75)
            return;

        var hash = TerrainNoise.Combine(seed.Value, x, y);

        if ((hash & 0xFFFF) > 30)
            return;

        TraceRiver(x, y);
    }

    private void TraceRiver(int x, int y)
    {
        const int MaxSteps = 2000;

        for (int step = 0; step < MaxSteps; step++)
        {
            _cache[(x, y)] =
                _cache.GetValueOrDefault((x, y)) + 1;

            if (elevation.GetElevation(x, y) <= 0.30)
                return;

            var next = LowestNeighbor(x, y);

            if (next == (x, y))
                return;

            x = next.X;
            y = next.Y;
        }
    }

    private (int X, int Y) LowestNeighbor(int x, int y)
    {
        double lowest = elevation.GetElevation(x, y);

        int lowestX = x;
        int lowestY = y;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0)
                    continue;

                double e = elevation.GetElevation(x + ox, y + oy);

                if (e < lowest)
                {
                    lowest = e;
                    lowestX = x + ox;
                    lowestY = y + oy;
                }
            }
        }

        return (lowestX, lowestY);
    }

    private int RegionOf(int coordinate)
    {
        if (coordinate >= 0)
            return coordinate / settings.ChunkSize;

        return (coordinate - (settings.ChunkSize - 1)) / settings.ChunkSize;
    }
}