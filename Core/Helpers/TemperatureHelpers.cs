using Core.Models;
using Core.Services;

namespace Core.Helpers;

public sealed class TemperatureHelpers
{
    public static void AddTemperature(Cell[,] world, TerrainContext context)
    {
        var settings = context.Settings.Temperature;
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
                Cell cell = world[y, x];
                int worldX = TerrainCoordinates.WorldX(context, x);

                double fractal = noise.CoreFractal(
                    worldX,
                    worldY,
                    octaves,
                    persistence);

                double temperature = 0.7 - (cell.Elevation * 0.5) + (fractal * 0.2);

                cell.Temperature = Math.Clamp(temperature, 0.0, 1.0);
            }
        });
    }
}