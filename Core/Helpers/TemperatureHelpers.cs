using Core.Models;
using Core.Services;

namespace Core.Helpers;

public sealed class TemperatureHelpers
{
    public static void AddTemperature(Cell[,] world, TerrainContext context)
    {
        var noise = new TerrainNoise(
            blockSize: context.Settings.Temperature.BlockSize,
            seed: context.Settings.Temperature.NoiseSeed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                Cell cell = world[y, x];

                int worldX = TerrainCoordinates.WorldX(
                    context,
                    x);

                int worldY = TerrainCoordinates.WorldY(
                    context,
                    y);

                double temperature =
                    0.8
                    - cell.Elevation * 0.5
                    + (noise.CoreFractal(
                        worldX, worldY,
                        octaves: context.Settings.Octaves,
                        persistence: context.Settings.Persistence) - 0.5) * 0.2;

                cell.Temperature = Math.Clamp(temperature, 0.0, 1.0);
            }
        });
    }
}
