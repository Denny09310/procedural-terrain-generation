using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainGenerator(
    IServiceProvider provider,
    Func<TerrainContext, ValueTask>[] invokers)
{
    public TerrainConfiguration Config => provider.GetRequiredService<TerrainConfiguration>();

    public async ValueTask<TerrainGrid> GenerateAsync()
    {
        var grid = new TerrainGrid(Config.Width, Config.Height);
        var context = new TerrainContext(grid, Config);

        foreach (var invoker in invokers)
            await invoker(context);

        return grid;
    }
}