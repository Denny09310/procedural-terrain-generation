using System.Diagnostics;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

var sw = Stopwatch.StartNew();

var builder = TerrainGeneratorBuilder.Create();

builder.Services.AddSingleton(sp => new TerrainConfiguration
{
    Width = 100,
    Height = 40,
    Seed = 12345
});

// Elevation Transformer
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise) =>
{
    float invWidth = 1f / grid.Width;
    float invHeight = 1f / grid.Height;

    foreach (var cell in grid)
    {
        cell.Elevation = noise.SampleFractal(
            cell.X * invWidth,
            cell.Y * invHeight,
            octaves: 8,
            frequency: 4f,
            persistence: 0.5f,
            lacunarity: 2f);
    }

    Normalization.Normalize(grid,
        get: c => c.Elevation,
        set: (c, v) => c.Elevation = MathF.Pow(v, 1.3f));
});

// Moisture Transformer
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise) =>
{
    float invWidth = 1f / grid.Width;
    float invHeight = 1f / grid.Height;

    foreach (var cell in grid)
    {
        float base_ = noise.SampleFractal(
            cell.X * invWidth + 500f,
            cell.Y * invHeight + 500f,
            octaves: 6,
            frequency: 3f,
            persistence: 0.6f,
            lacunarity: 2f);

        float rain = 1f - MathF.Pow(cell.Elevation, 2f) * 0.6f;

        cell.Moisture = base_ * MathF.Max(0f, rain);
    }

    Normalization.Normalize(grid,
        get: c => c.Moisture,
        set: (c, v) => c.Moisture = v);
});

// Temperature Transformer
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise) =>
{
    float invWidth = 1f / grid.Width;
    float invHeight = 1f / grid.Height;

    foreach (var cell in grid)
    {
        float latitude = 1f - MathF.Abs(cell.Y * invHeight - 0.5f) * 2f;

        float lapse = 1f - MathF.Pow(MathF.Max(0f, cell.Elevation - 0.3f), 1.5f);

        float variation = noise.SampleFractal(
            cell.X * invWidth + 1000f,
            cell.Y * invHeight + 1000f,
            octaves: 3,
            frequency: 2f,
            persistence: 0.4f,
            lacunarity: 2f) * 0.1f;

        cell.Temperature = Math.Clamp(latitude * lapse + variation, 0f, 1f);
    }
});

builder.AddTransfomer((TerrainGrid grid) =>
{
    foreach (var cell in grid)
    {
        cell.Biome = Classification.Classify(cell);
    }
});

var generator = builder.Build();
var terrain = await generator.GenerateAsync();

var elapsed = sw.ElapsedMilliseconds;
Console.WriteLine($"Completed in {elapsed}ms.");

Printer.Print(terrain);

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

internal static class Classification
{
    private const float DeepOcean = 0.20f;
    private const float Ocean = 0.30f;
    private const float ShallowWater = 0.34f;
    private const float Beach = 0.36f;

    private const float Alpine = 0.70f;
    private const float Mountain = 0.87f;
    private const float Snow = 0.94f;
    private const float Glacier = 0.98f;

    private static readonly TerrainBiome[,] Climate =
    {
        // Polar
        {
            TerrainBiome.Tundra,
            TerrainBiome.Tundra,
            TerrainBiome.Tundra,
            TerrainBiome.Snow,
            TerrainBiome.Glacier
        },

        // Cold
        {
            TerrainBiome.DryGrassland,
            TerrainBiome.Grassland,
            TerrainBiome.BorealForest,
            TerrainBiome.Taiga,
            TerrainBiome.Taiga
        },

        // Temperate
        {
            TerrainBiome.Shrubland,
            TerrainBiome.Grassland,
            TerrainBiome.TemperateForest,
            TerrainBiome.TemperateRainforest,
            TerrainBiome.TemperateRainforest
        },

        // Warm
        {
            TerrainBiome.Desert,
            TerrainBiome.DryGrassland,
            TerrainBiome.Savanna,
            TerrainBiome.TropicalForest,
            TerrainBiome.Jungle
        },

        // Tropical
        {
            TerrainBiome.Desert,
            TerrainBiome.Savanna,
            TerrainBiome.TropicalForest,
            TerrainBiome.Jungle,
            TerrainBiome.TropicalRainforest
        }
    };

    public static TerrainBiome Classify(TerrainCell cell)
    {
        float elevation = cell.Elevation;
        float temperature = cell.Temperature;
        float moisture = cell.Moisture;

        // ----------------------------
        // Water
        // ----------------------------

        if (elevation < DeepOcean)
            return TerrainBiome.DeepOcean;

        if (elevation < Ocean)
            return TerrainBiome.Ocean;

        if (elevation < ShallowWater)
            return TerrainBiome.ShallowWater;

        if (elevation < Beach)
            return TerrainBiome.Beach;

        // ----------------------------
        // Mountains
        // ----------------------------

        if (elevation >= Glacier)
            return TerrainBiome.Glacier;

        if (elevation >= Snow)
            return TerrainBiome.Snow;

        if (elevation >= Mountain)
            return TerrainBiome.Mountain;

        if (elevation >= Alpine)
        {
            if (temperature < 0.30f)
                return TerrainBiome.Alpine;

            return TerrainBiome.SubAlpine;
        }

        // ----------------------------
        // Climate
        // ----------------------------

        int t = TemperatureBand(temperature);
        int m = MoistureBand(moisture);

        TerrainBiome biome = Climate[t, m];

        // ----------------------------
        // Local overrides
        // ----------------------------

        // Wet lowlands become wetlands.
        if (elevation < 0.45f && moisture > 0.90f)
        {
            if (temperature > 0.75f)
                biome = TerrainBiome.Swamp;
            else
                biome = TerrainBiome.Wetland;
        }

        // Tropical coastal wetlands.
        if (elevation < 0.40f &&
            moisture > 0.85f &&
            temperature > 0.80f)
        {
            biome = TerrainBiome.Mangrove;
        }

        return biome;
    }

    private static int TemperatureBand(float t)
    {
        if (t < 0.15f) return 0; // Polar
        if (t < 0.35f) return 1; // Cold
        if (t < 0.60f) return 2; // Temperate
        if (t < 0.80f) return 3; // Warm
        return 4;                // Tropical
    }

    private static int MoistureBand(float m)
    {
        if (m < 0.15f) return 0;
        if (m < 0.35f) return 1;
        if (m < 0.55f) return 2;
        if (m < 0.75f) return 3;
        return 4;
    }
}

internal static class Printer
{
    public static void Print(TerrainGrid grid)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var cell = grid[x, y];

                (ConsoleColor color, char glyph) = GetStyle(cell.Biome);

                Console.ForegroundColor = color;
                Console.Write(glyph);
            }

            Console.WriteLine();
        }

        Console.ResetColor();
    }

    private static (ConsoleColor, char) GetStyle(TerrainBiome biome) =>
        biome switch
        {
            // Water
            TerrainBiome.DeepOcean => (ConsoleColor.DarkBlue, '~'),
            TerrainBiome.Ocean => (ConsoleColor.Blue, '~'),
            TerrainBiome.ShallowWater => (ConsoleColor.Cyan, '~'),
            TerrainBiome.River => (ConsoleColor.Blue, '≈'),

            // Coast
            TerrainBiome.Beach => (ConsoleColor.Yellow, '.'),
            TerrainBiome.Wetland => (ConsoleColor.DarkGreen, ','),
            TerrainBiome.Swamp => (ConsoleColor.DarkGreen, '#'),
            TerrainBiome.Mangrove => (ConsoleColor.Green, '&'),

            // Dry
            TerrainBiome.Desert => (ConsoleColor.DarkYellow, '░'),
            TerrainBiome.Savanna => (ConsoleColor.Yellow, '"'),
            TerrainBiome.DryGrassland => (ConsoleColor.DarkYellow, ';'),

            // Temperate
            TerrainBiome.Grassland => (ConsoleColor.Green, '"'),
            TerrainBiome.Shrubland => (ConsoleColor.DarkGreen, ':'),
            TerrainBiome.TemperateForest => (ConsoleColor.Green, '♣'),
            TerrainBiome.TemperateRainforest => (ConsoleColor.DarkGreen, '♠'),

            // Tropical
            TerrainBiome.TropicalForest => (ConsoleColor.Green, '♣'),
            TerrainBiome.TropicalRainforest => (ConsoleColor.DarkGreen, '▓'),
            TerrainBiome.Jungle => (ConsoleColor.DarkGreen, '█'),

            // Cold
            TerrainBiome.Taiga => (ConsoleColor.DarkCyan, '▲'),
            TerrainBiome.BorealForest => (ConsoleColor.Cyan, '▲'),
            TerrainBiome.Tundra => (ConsoleColor.Gray, '·'),

            // Mountains
            TerrainBiome.SubAlpine => (ConsoleColor.DarkGray, '^'),
            TerrainBiome.Alpine => (ConsoleColor.Gray, '^'),
            TerrainBiome.Mountain => (ConsoleColor.DarkGray, 'M'),
            TerrainBiome.Snow => (ConsoleColor.White, '▲'),
            TerrainBiome.Glacier => (ConsoleColor.White, '█'),

            // Special
            TerrainBiome.Volcanic => (ConsoleColor.Red, 'V'),

            _ => (ConsoleColor.Magenta, '?')
        };
}