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
        ChunkSize = 32,      // Generates in manageable chunks
        WorldWidth = 2048,   // Noise stretches across a macro map size
        WorldHeight = 2048,
        Seed = 12345
    });

    // Elevation Transformer
    builder.AddTransfomer((TerrainGrid grid, TerrainContext context, INoiseSource noise) =>
    {
        float invWidth = 1f / context.Config.WorldWidth;
        float invHeight = 1f / context.Config.WorldHeight;

        foreach (var cell in grid)
        {
            // 1. Calculate absolute world position
            int worldX = context.OffsetX + cell.X;
            int worldY = context.OffsetY + cell.Y;

            // 2. Sample noise based on world position
            float rawNoise = noise.SampleFractal(
                worldX * invWidth,
                worldY * invHeight,
                octaves: 8,
                frequency: 4f,
                persistence: 0.5f,
                lacunarity: 2f);

            // 3. Analytical Normalization: Map (-1 to 1) to (0 to 1) safely without knowing min/max
            float normalized = Math.Clamp((rawNoise + 1f) * 0.5f, 0f, 1f);

            cell.Elevation = MathF.Pow(normalized, 1.3f);
        }
    });

    // Moisture Transformer
    builder.AddTransfomer((TerrainGrid grid, TerrainContext context, INoiseSource noise) =>
    {
        float invWidth = 1f / context.Config.WorldWidth;
        float invHeight = 1f / context.Config.WorldHeight;

        foreach (var cell in grid)
        {
            int worldX = context.OffsetX + cell.X;
            int worldY = context.OffsetY + cell.Y;

            float baseNoise = noise.SampleFractal(
                worldX * invWidth + 500f,
                worldY * invHeight + 500f,
                octaves: 6,
                frequency: 3f,
                persistence: 0.6f,
                lacunarity: 2f);

            float normalizedNoise = Math.Clamp((baseNoise + 1f) * 0.5f, 0f, 1f);
            float rain = 1f - MathF.Pow(cell.Elevation, 2f) * 0.6f;

            cell.Moisture = normalizedNoise * MathF.Max(0f, rain);
        }
    });

    // Temperature Transformer
    builder.AddTransfomer((TerrainGrid grid, TerrainContext context, INoiseSource noise) =>
    {
        float invWidth = 1f / context.Config.WorldWidth;
        float invHeight = 1f / context.Config.WorldHeight;

        foreach (var cell in grid)
        {
            int worldX = context.OffsetX + cell.X;
            int worldY = context.OffsetY + cell.Y;

            float latitude = 1f - MathF.Abs(worldY * invHeight - 0.5f) * 2f;
            float lapse = 1f - MathF.Pow(MathF.Max(0f, cell.Elevation - 0.3f), 1.5f);

            float variation = noise.SampleFractal(
                worldX * invWidth + 1000f,
                worldY * invHeight + 1000f,
                octaves: 3,
                frequency: 2f,
                persistence: 0.4f,
                lacunarity: 2f);

            float normalizedVariation = variation * 0.1f;

            cell.Temperature = Math.Clamp(latitude * lapse + normalizedVariation, 0f, 1f);
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