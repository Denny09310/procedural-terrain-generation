using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainGenerator(
    IServiceProvider provider,
    Func<TerrainContext, ValueTask>[] invokers)
{
    public TerrainConfiguration Config => provider.GetRequiredService<TerrainConfiguration>();

    public async ValueTask<TerrainGrid> GenerateChunkAsync(int chunkX, int chunkY)
    {
        var grid = new TerrainGrid(Config.ChunkSize, Config.ChunkSize);
        var context = new TerrainContext(grid, Config, chunkX, chunkY);

        foreach (var invoker in invokers)
            await invoker(context);

        return grid;
    }
}