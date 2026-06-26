using System.Diagnostics;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

var sw = Stopwatch.StartNew();

var builder = TerrainGeneratorBuilder.Create();

builder.Services.AddSingleton(sp => new TerrainConfiguration
{
    Width = 512,
    Height = 512,
    Seed = 42
});

// Elevation Transformer
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise, TerrainConfiguration config) =>
{
    float invWidth = 1f / config.Width;
    float invHeight = 1f / config.Height;

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
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise, TerrainConfiguration config) =>
{
    float invWidth = 1f / config.Width;
    float invHeight = 1f / config.Height;

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
builder.AddTransfomer((TerrainGrid grid, INoiseSource noise, TerrainConfiguration config) =>
{
    float invWidth = 1f / config.Width;
    float invHeight = 1f / config.Height;

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

var generator = builder.Build();
var terrain = await generator.GenerateAsync();

var elapsed = sw.ElapsedMilliseconds;
Console.WriteLine($"Completed in {elapsed}ms.");

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