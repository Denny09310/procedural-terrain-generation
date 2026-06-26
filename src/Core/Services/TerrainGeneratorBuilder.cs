using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainGeneratorBuilder
{
    private readonly ICollection<TerrainTransformerHandler> _handlers = [];

    private TerrainGeneratorBuilder()
    {

    }

    public ServiceCollection Services { get; } = new();

    public static TerrainGeneratorBuilder Create()
    {
        var builder = new TerrainGeneratorBuilder();

        builder.Services.AddSingleton<INoiseSource, PerlinNoise>();
        builder.Services.AddSingleton<ITerrainRandomizer, SeededRandomizer>();

        return builder;
    }

    public TerrainGeneratorBuilder AddTransfomer<T>(T handler)
        where T : Delegate
    {
        _handlers.Add(TerrainTransformerHandler.Create(handler));
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