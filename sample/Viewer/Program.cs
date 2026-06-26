using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Viewer.Components.Pages.Counter;
using Viewer.Helpers;

[assembly: GenerateMarkupExtensionsForAssembly(typeof(Program))]

var services = new ServiceCollection();

services.AddSingleton(_ =>
{
    var builder = TerrainGeneratorBuilder.Create();

    builder.Services.AddSingleton(sp => new TerrainConfiguration
    {
        Width = 512,
        Height = 512,
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

    // Biome Classifier Transformer
    builder.AddTransfomer((TerrainGrid grid) =>
    {
        foreach (var cell in grid)
        {
            cell.Biome = Classification.Classify(cell);
        }
    });

    return builder.Build();
});

var lifetime = new ClassicDesktopStyleApplicationLifetime
{
    Args = args,
    ShutdownMode = ShutdownMode.OnLastWindowClose
};


var provider = services.BuildServiceProvider();

AppBuilder.Configure<Application>()
    .UsePlatformDetect()
    .AfterSetup(builder => builder.Instance?.Styles.Add(new FluentTheme()))
    .UseServiceProvider(provider)
    .UseComponentControlFactory(type => (Control)ActivatorUtilities.CreateInstance(provider, type))
    .UseViewInitializationStrategy(ViewInitializationStrategy.Lazy)
    .UseHotReload()
    .SetupWithLifetime(lifetime);

lifetime.MainWindow = new Window()
    .Title("Avalonia Procedual Terrain Viewer")
    .Width(800)
    .Height(600)
    .Content(ViewFactory.Create<TerrainViewer>());

lifetime.Start(args);