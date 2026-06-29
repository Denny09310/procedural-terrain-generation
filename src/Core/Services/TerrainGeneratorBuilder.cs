using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainGeneratorBuilder
{
    private readonly ICollection<TerrainTransformer> _handlers = [];

    private TerrainGeneratorBuilder() { }

    public ServiceCollection Services { get; } = new();

    public static TerrainGeneratorBuilder Create()
    {
        var builder = new TerrainGeneratorBuilder();

        builder.Services.AddSingleton<ITerrainNoise, PerlinNoise>();
        builder.Services.AddSingleton<ITerrainRandomizer, SeededRandomizer>();

        builder.Services.AddSingleton<ElevationTransformer>();
        builder.Services.AddSingleton<MoistureTransformer>();
        builder.Services.AddSingleton<TemperatureTransformer>();
        builder.Services.AddSingleton<ClassifierTransformer>();

        return builder;
    }

    /// <summary>Adds a transformer delegate to the pipeline.</summary>
    public TerrainGeneratorBuilder AddTransformer<T>(T handler)
        where T : Delegate
    {
        _handlers.Add(TerrainTransformer.Create(handler));
        return this;
    }

    public TerrainGenerator Build()
    {
        var provider = Services.BuildServiceProvider();

        var binder = new TerrainTransformerBinder(provider);
        var invokers = _handlers
            .Select(binder.Bind)
            .ToArray();

        return new TerrainGenerator(provider, invokers);
    }
}
