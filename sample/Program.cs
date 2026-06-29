using Core.Models;
using Core.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = TerrainGeneratorBuilder.Create();

builder.Services.AddSingleton(_ => new TerrainConfiguration
{
    ChunkSize = 128
});

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, ElevationTransformer elevation)
    => elevation.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, MoistureTransformer moisture)
    => moisture.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, TerrainContext ctx, TemperatureTransformer temperature)
    => temperature.Apply(grid, ctx));

builder.AddTransformer((TerrainGrid grid, ClassifierTransformer classifier)
    => classifier.Apply(grid));

var generator = builder.Build();

var chunk = await generator.GenerateAsync(0, 0);
System.Console.WriteLine();