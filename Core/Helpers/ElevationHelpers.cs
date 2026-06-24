using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class ElevationHelpers
{
    public static void AddElevation(
    Cell[,] world,
    TerrainContext context)
    {
        var settings = context.Settings.Elevation;

        var noise = new TerrainNoise(
            blockSize: settings.BlockSize,
            seed: settings.NoiseSeed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        double centerX = (width - 1) / 2.0;
        double centerY = (height - 1) / 2.0;

        double maxDistance =
            Math.Sqrt(
                centerX * centerX +
                centerY * centerY);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                world[y, x].Elevation =
                    Math.Clamp(
                        GetElevation(
                            context.Settings.Shape,
                            noise,
                            context,
                            width,
                            height,
                            x,
                            y,
                            centerX,
                            centerY,
                            maxDistance),
                        0.0,
                        1.0);
            }
        });
    }

    private static double GetElevation(
        TerrainShape shape,
        TerrainNoise noise,
        TerrainContext context,
        int width,
        int height,
        int x,
        int y,
        double centerX,
        double centerY,
        double maxDistance)
    {
        return shape switch
        {
            TerrainShape.Continent =>
                GetContinentElevation(
                    noise,
                    context,
                    width,
                    x,
                    y),

            TerrainShape.Archipelago =>
                GetArchipelagoElevation(
                    noise,
                    context,
                    x,
                    y),

            TerrainShape.InlandSea =>
                GetInlandSeaElevation(
                    noise,
                    context,
                    x,
                    y,
                    centerX,
                    centerY,
                    maxDistance),

            TerrainShape.Plains =>
                GetPlainsElevation(
                    noise,
                    context,
                    x,
                    y),

            TerrainShape.Mountains =>
                GetMountainRangeElevation(
                    noise,
                    context,
                    height,
                    x,
                    y),

            _ => throw new NotImplementedException()
        };
    }

    private static double GetContinentElevation(
        TerrainNoise noise,
        TerrainContext context,
        int width,
        int x,
        int y)
    {
        double continent =
            1.0 - (double)x / width;

        double variation =
            GetVariation(noise, context, x, y);

        return continent * 0.5 + variation * 0.5;
    }

    private static double GetArchipelagoElevation(
    TerrainNoise noise,
    TerrainContext context,
    int x,
    int y)
    {
        double value =
            GetVariation(
                noise,
                context,
                x,
                y);

        return Math.Pow(value, 3.0);
    }

    private static double GetInlandSeaElevation(
        TerrainNoise noise,
        TerrainContext context,
        int x,
        int y,
        double centerX,
        double centerY,
        double maxDistance)
    {
        double dx = x - centerX;
        double dy = y - centerY;

        double distance =
            Math.Sqrt(dx * dx + dy * dy)
            / maxDistance;

        distance =
            Math.Clamp(distance, 0.0, 1.0);

        double basin =
            Math.Pow(distance, 3.0);

        double variation =
            GetVariation(noise, context, x, y);

        return basin * 0.7 + variation * 0.3;
    }

    private static double GetPlainsElevation(
        TerrainNoise noise,
        TerrainContext context,
        int x,
        int y)
    {
        double variation = GetVariation(
            noise,
            context,
            x,
            y);

        return 0.35 + variation * 0.3;
    }

    private static double GetMountainRangeElevation(
        TerrainNoise noise,
        TerrainContext context,
        int height,
        int x,
        int y)
    {
        double center = height / 2.0;

        double distance = Math.Abs(y - center) / center;

        double ridge = 1.0 - distance;

        double variation = GetVariation(
            noise,
            context,
            x,
            y);

        return ridge * 0.7 + variation * 0.3;
    }

    private static double GetVariation(
        TerrainNoise noise,
        TerrainContext context,
        int x,
        int y)
    {
        int worldX = TerrainCoordinates.WorldX(
            context,
            x);

        int worldY = TerrainCoordinates.WorldY(
            context,
            y);

        var octaves = context.Settings.Octaves;
        var persistence = context.Settings.Persistence;

        double variation =
            noise.CoreFractal(
                worldX,
                worldY,
                octaves,
                persistence);

        return variation;
    }
}
