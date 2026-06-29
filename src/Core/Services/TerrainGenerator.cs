using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainGenerator(
    IServiceProvider provider,
    Func<TerrainContext, ValueTask>[] invokers)
{
    public Dictionary<TerrainCoordinate, TerrainGrid> Chunks { get; } = [];

    /// <summary>
    /// Generates a single chunk at the given chunk-space coordinates.
    /// </summary>
    public async ValueTask<TerrainGrid> GenerateAsync(int chunkX, int chunkY)
    {
        var key = new TerrainCoordinate(chunkX, chunkY);

        if (Chunks.TryGetValue(key, out var existing))
            return existing;

        var config = provider.GetRequiredService<TerrainConfiguration>();
        var size = config.ChunkSize;

        var grid = new TerrainGrid(size, size)
        {
            X = chunkX,
            Y = chunkY,
        };

        Chunks.Add(key, grid);

        var context = new TerrainContext(
            config,
            grid,
            chunkX,
            chunkY);

        foreach (var invoker in invokers)
            await invoker(context);

        return grid;
    }
}
