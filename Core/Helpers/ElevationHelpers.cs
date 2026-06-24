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

        // Detail noise for local textures, ridges, and ruggedness
        var detailNoise = new TerrainNoise(
            blockSize: settings.BlockSize,
            seed: settings.NoiseSeed);

        // Macro noise (10x larger) for sweeping geographic features (continents, massive seas)
        var macroNoise = new TerrainNoise(
            blockSize: settings.BlockSize * 10,
            seed: settings.NoiseSeed + 1337); // Offset seed so layers don't mirror each other

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            int worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                double detail = detailNoise.SampleFractal(worldX, worldY, octaves, persistence);
                double macro = macroNoise.SampleFractal(worldX, worldY, octaves: 2, persistence: 0.5);

                double elevation = 0.0;

                switch (shape)
                {
                    case TerrainShape.Continent:
                        // Balanced: Sprawling landmasses split by deep global oceans
                        elevation = (macro * 0.6) + (detail * 0.4);
                        break;

                    case TerrainShape.Archipelago:
                        // Water-heavy: Squaring the detail creates isolated peaks, 
                        // and subtracting 0.25 submerges the valleys underwater
                        double islandPeaks = Math.Pow(detail, 2.0);
                        elevation = islandPeaks - 0.25 + (macro * 0.20);
                        break;

                    case TerrainShape.InlandSea:
                        // Land-heavy: We raise the baseline up by +0.25 so the world is mostly land,
                        // but the lowest structural troughs of the macro noise dip down into massive seas
                        elevation = (macro * 0.55) + 0.25 + (detail * 0.20);
                        break;

                    case TerrainShape.Plains:
                        // Low variance: Clamps the elevation tightly between ~0.35 and ~0.60
                        // results in endless meadows and forests with no massive oceans or alpine peaks
                        elevation = 0.45 + (detail * 0.15);
                        break;

                    case TerrainShape.Mountains:
                        // Sharp and high: Inverts the detail noise to create razor-sharp mountain ridges,
                        // and keeps the base elevation high so valleys are rare
                        double sharpRidge = 1.0 - Math.Abs(detail - 0.5) * 2.0;
                        elevation = 0.35 + (sharpRidge * 0.5) + (macro * 0.15);
                        break;

                    default:
                        throw new NotImplementedException($"Terrain shape {shape} is not configured.");
                }

                // Write directly to the array element to avoid any struct copy mutation bugs
                world[y, x].Elevation = Math.Clamp(elevation, 0.0, 1.0);
            }
        });
    }
}