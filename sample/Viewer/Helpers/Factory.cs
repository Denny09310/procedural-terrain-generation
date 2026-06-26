using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Viewer.Helpers;

internal static class Factory
{
    public static TerrainGenerator BuildGenerator(int seed)
    {
        var builder = TerrainGeneratorBuilder.Create();

        builder.Services.AddSingleton(new TerrainConfiguration
        {
            ChunkSize = 32,
            WorldWidth = 2048,
            WorldHeight = 2048,
            Seed = seed,
        });

        builder.AddTransfomer((TerrainGrid grid, TerrainContext ctx, ElevationTransformer elevation)
            => elevation.Apply(grid, ctx));

        builder.AddTransfomer((TerrainGrid grid, TerrainContext ctx, MoistureTransformer moisture)
            => moisture.Apply(grid, ctx));

        builder.AddTransfomer((TerrainGrid grid, TerrainContext ctx, TemperatureTransformer temperature)
            => temperature.Apply(grid, ctx));

        builder.AddTransfomer((TerrainGrid grid, ClassifierTransformer classifier)
            => classifier.Apply(grid));

        builder.AddTransfomer((TerrainGrid grid, StructureTransformer structure)
            => structure.Apply(grid));

        return builder.Build();
    }
}