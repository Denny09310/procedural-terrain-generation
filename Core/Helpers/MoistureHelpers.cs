using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class MoistureHelpers
{
    public static void AddMoisture(Cell[,] world, TerrainContext context)
    {
        var noise = new TerrainNoise(
            blockSize: context.Settings.Moisture.BlockSize,
            seed: context.Settings.Moisture.NoiseSeed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                var cell = world[y, x];

                if (cell.Elevation < 0.2)
                {
                    cell.Moisture = 1.0;
                    continue;
                }

                int worldX = TerrainCoordinates.WorldX(
                    context,
                    x);

                int worldY = TerrainCoordinates.WorldY(
                    context,
                    y);

                double moisture = noise.CoreFractal(
                    worldX, worldY,
                    octaves: context.Settings.Octaves,
                    persistence: context.Settings.Persistence);

                cell.Moisture = Math.Clamp(moisture - cell.Elevation * 0.3, 0.0, 1.0);
            }
        });
    }

    public static void RecalculateMoisture(Cell[,] world, TerrainContext context)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (world[y, x].River <= 0)
                {
                    continue;
                }

                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx < 0 || nx >= width ||
                            ny < 0 || ny >= height)
                        {
                            continue;
                        }

                        var cell = world[ny, nx];

                        cell.Moisture = Math.Clamp(cell.Moisture + 0.15, 0.0, 1.0);
                    }
                }
            }
        }
    }
}
