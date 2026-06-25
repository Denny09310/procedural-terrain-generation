using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class MoistureHelpers
{
    public static void AddMoisture(Cell[,] world, TerrainContext context)
    {
        var settings = context.Settings.Moisture;
        var octaves = context.Settings.Octaves;
        var persistence = context.Settings.Persistence;

        var noise = new TerrainNoise(
            blockSize: settings.BlockSize,
            seed: settings.NoiseSeed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            int worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                ref var cell = ref world[y, x];

                if (cell.Elevation < 0.2)
                {
                    cell.Moisture = 1.0;
                    continue;
                }

                int worldX = TerrainCoordinates.WorldX(context, x);

                double moisture = noise.SampleFractal(
                    worldX, worldY,
                    octaves: octaves,
                    persistence: persistence);

                cell.Moisture = Math.Clamp(moisture - cell.Elevation * 0.3, 0.0, 1.0);
            }
        });
    }

    public static void RecalculateMoisture(Cell[,] world, TerrainContext context)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            int minDy = Math.Max(-2, -y);
            int maxDy = Math.Min(2, height - 1 - y);

            for (int x = 0; x < width; x++)
            {
                if (world[y, x].River <= 0)
                    continue;

                int minDx = Math.Max(-2, -x);
                int maxDx = Math.Min(2, width - 1 - x);

                for (int dy = minDy; dy <= maxDy; dy++)
                {
                    int ny = y + dy;

                    for (int dx = minDx; dx <= maxDx; dx++)
                    {
                        ref var cell = ref world[ny, x + dx];
                        cell.Moisture = Math.Clamp(cell.Moisture + 0.15, 0.0, 1.0);
                    }
                }
            }
        });
    }
}