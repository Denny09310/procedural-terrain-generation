using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = TerrainGeneratorBuilder.Create();

builder.Services.AddSingleton(_ => new TerrainConfiguration
{
    ChunkSize = 32
});

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, ElevationTransformer elevation)
    => elevation.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, MoistureTransformer moisture)
    => moisture.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, TemperatureTransformer temperature)
    => temperature.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, ClassifierTransformer classifier)
    => classifier.Apply(grid));

builder.AddTransformer((TerrainWorld world, TerrainGrid grid, TerrainContext ctx, StructureTransformer structure)
    => structure.Apply(world, grid, ctx));

var generator = builder.Build();

var world = generator.CreateWorld();

for (int y = 0; y < 5; y++)
{
    for (int x = 0; x < 5; x++)
    {
        await world.GenerateChunkAsync(x, y);
    }
}

System.Console.WriteLine();