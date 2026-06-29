using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

/// <summary>
/// Runs the transformer pipeline to produce individual terrain chunks.
/// Call <see cref="CreateWorld"/> to obtain a <see cref="TerrainWorld"/> that caches
/// generated chunks and manages structures — the generator itself holds no world state.
/// </summary>
public sealed class TerrainGenerator(
    IServiceProvider provider,
    Func<TerrainContext, ValueTask>[] invokers)
{
    /// <summary>
    /// Creates a new, empty <see cref="TerrainWorld"/> backed by this generator's pipeline.
    /// </summary>
    public TerrainWorld CreateWorld() => new(GenerateChunkCoreAsync);

    private async ValueTask<TerrainGrid> GenerateChunkCoreAsync(TerrainWorld world, int chunkX, int chunkY)
    {
        var config = provider.GetRequiredService<TerrainConfiguration>();
        var size = config.ChunkSize;

        var grid = new TerrainGrid(size, size)
        {
            X = chunkX,
            Y = chunkY,
        };

        var context = new TerrainContext(config, world, grid, chunkX, chunkY);

        foreach (var invoker in invokers)
            await invoker(context);

        return grid;
    }
}
