using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class ElevationHelpers
{
    public static void AddElevation(Cell[,] world, TerrainContext context)
    {
        var settings = context.Settings.Elevation;
        var shape = context.Settings.Shape;
        var octaves = context.Settings.Octaves;
        var persistence = context.Settings.Persistence;

        var detailNoise = new TerrainNoise(
            blockSize: settings.BlockSize,
            seed: settings.NoiseSeed);

        var macroNoise = new TerrainNoise(
            blockSize: settings.BlockSize * 10,
            seed: settings.NoiseSeed + 1337);

        var height = world.GetLength(0);
        var width = world.GetLength(1);

        switch (shape)
        {
            case TerrainShape.Continent:
                Parallel.For(0, height, RunContinent);
                break;

            case TerrainShape.Archipelago:
                Parallel.For(0, height, RunArchipelago);
                break;

            case TerrainShape.InlandSea:
                Parallel.For(0, height, RunInlandSea);
                break;

            case TerrainShape.Plains:
                Parallel.For(0, height, RunPlains);
                break;

            case TerrainShape.Mountains:
                Parallel.For(0, height, RunMountains);
                break;

            default:
                throw new NotImplementedException(
                    $"Terrain shape {shape} is not configured.");
        }

        void RunContinent(int y)
        {
            var worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                var detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);
                var macro = macroNoise.SampleFractal(worldX, worldY, octaves: 2, persistence: 0.5);

                ref var cell = ref world[y, x];
                cell.Elevation = Math.Clamp(macro * 0.6 + detail * 0.4, 0.0, 1.0);
            }
        }

        void RunArchipelago(int y)
        {
            var worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                var detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);
                var macro = macroNoise.SampleFractal(worldX, worldY, octaves: 2, persistence: 0.5);

                // detail * detail instead of Math.Pow(detail, 2.0) — identical result,
                // avoids the general-purpose power function in a hot path
                var peaks = detail * detail;

                ref var cell = ref world[y, x];
                cell.Elevation = Math.Clamp(peaks - 0.25 + macro * 0.20, 0.0, 1.0);
            }
        }

        void RunInlandSea(int y)
        {
            var worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                var detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);
                var macro = macroNoise.SampleFractal(worldX, worldY, octaves: 2, persistence: 0.5);

                ref var cell = ref world[y, x];
                cell.Elevation = Math.Clamp(macro * 0.55 + 0.25 + detail * 0.20, 0.0, 1.0);
            }
        }

        void RunPlains(int y)
        {
            var worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                // Plains doesn't use macro at all — skip sampling it entirely.
                // Original code sampled it unconditionally and threw the result away.
                var detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);

                ref var cell = ref world[y, x];
                cell.Elevation = Math.Clamp(0.45 + detail * 0.15, 0.0, 1.0);
            }
        }

        void RunMountains(int y)
        {
            var worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                var detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);
                var macro = macroNoise.SampleFractal(worldX, worldY, octaves: 2, persistence: 0.5);

                var ridge = 1.0 - Math.Abs(detail - 0.5) * 2.0;

                ref var cell = ref world[y, x];
                cell.Elevation = Math.Clamp(0.35 + ridge * 0.5 + macro * 0.15, 0.0, 1.0);
            }
        }
    }
}