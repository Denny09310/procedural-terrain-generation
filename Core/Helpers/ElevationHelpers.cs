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

        var noise = new TerrainNoise(
            blockSize: settings.BlockSize,
            seed: settings.NoiseSeed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        double centerX = (width - 1) / 2.0;
        double centerY = (height - 1) / 2.0;
        double maxDistance = Math.Sqrt(centerX * centerX + centerY * centerY);

        switch (shape)
        {
            case TerrainShape.Continent:
                Parallel.For(0, height, y =>
                {
                    int worldY = TerrainCoordinates.WorldY(context, y);

                    for (int x = 0; x < width; x++)
                    {
                        int worldX = TerrainCoordinates.WorldX(context, x);
                        double variation = noise.CoreFractal(worldX, worldY, octaves, persistence);

                        double continent = 1.0 - (double)x / width;
                        double elevation = continent * 0.5 + variation * 0.5;

                        world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
                    }
                });
                break;

            case TerrainShape.Archipelago:
                Parallel.For(0, height, y =>
                {
                    int worldY = TerrainCoordinates.WorldY(context, y);

                    for (int x = 0; x < width; x++)
                    {
                        int worldX = TerrainCoordinates.WorldX(context, x);
                        double variation = noise.CoreFractal(worldX, worldY, octaves, persistence);

                        double elevation = Math.Pow(variation, 3.0);

                        world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
                    }
                });
                break;

            case TerrainShape.InlandSea:
                Parallel.For(0, height, y =>
                {
                    int worldY = TerrainCoordinates.WorldY(context, y);

                    double dy = y - centerY;
                    double dySq = dy * dy;

                    for (int x = 0; x < width; x++)
                    {
                        int worldX = TerrainCoordinates.WorldX(context, x);
                        double variation = noise.CoreFractal(worldX, worldY, octaves, persistence);

                        double dx = x - centerX;
                        double distance = Math.Sqrt(dx * dx + dySq) / maxDistance;

                        double basin = Math.Pow(distance, 3.0);
                        double elevation = basin * 0.7 + variation * 0.3;

                        world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
                    }
                });
                break;

            case TerrainShape.Plains:
                Parallel.For(0, height, y =>
                {
                    int worldY = TerrainCoordinates.WorldY(context, y);

                    for (int x = 0; x < width; x++)
                    {
                        int worldX = TerrainCoordinates.WorldX(context, x);
                        double variation = noise.CoreFractal(worldX, worldY, octaves, persistence);

                        double elevation = 0.35 + variation * 0.3;

                        world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
                    }
                });
                break;

            case TerrainShape.Mountains:
                double center = height / 2.0;
                Parallel.For(0, height, y =>
                {
                    int worldY = TerrainCoordinates.WorldY(context, y);

                    double distance = Math.Abs(y - center) / center;
                    double ridge = 1.0 - distance;

                    for (int x = 0; x < width; x++)
                    {
                        int worldX = TerrainCoordinates.WorldX(context, x);
                        double variation = noise.CoreFractal(worldX, worldY, octaves, persistence);

                        double elevation = ridge * 0.7 + variation * 0.3;

                        world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
                    }
                });
                break;

            default:
                throw new NotImplementedException();
        }
    }
}