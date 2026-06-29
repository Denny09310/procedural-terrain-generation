using Core.Interfaces;
using Core.Models;

namespace Core.Services;

/// <summary>
/// Terrain transformer that assigns a <see cref="StructureType"/> to each eligible cell.
///
/// Register this as a transformer in your pipeline <em>after</em> the biome classifier:
/// <code>
/// builder.Services.AddSingleton&lt;IStructureRule, VillageRule&gt;();
/// builder.Services.AddSingleton&lt;IStructureRule, PortRule&gt;();
///
/// builder.AddTransfomer(
///     (TerrainGrid grid, StructureTransformer placer) =>
///         placer.Apply(grid));
/// </code>
///
/// Rules are sorted by <see cref="IStructureRule.Priority"/> (descending) at construction
/// time, so the most specific rules always get first refusal on each cell.
/// </summary>
public sealed class StructureTransformer(
    IEnumerable<IStructureRule> rules,
    ITerrainRandomizer random)
{
    private readonly IStructureRule[] _rules =
        [.. rules.OrderByDescending(r => r.Priority)];

    /// <summary>
    /// Walks every cell in <paramref name="grid"/> and, for each rule in priority order,
    /// places the structure if <c>CanPlace</c> passes and a random roll beats
    /// <c>SpawnChance</c>.  Only one structure is placed per cell.
    /// </summary>
    public void Apply(TerrainWorld world, TerrainGrid grid, TerrainContext ctx)
    {
        var size = ctx.Configuration.ChunkSize;
        random.SetSeed(Hash(ctx.ChunkX, ctx.ChunkY));

        foreach (var rule in _rules)
        {
            var localX = random.NextInt(0, size);
            var localY = random.NextInt(0, size);

            var cell = grid[localX, localY];

            if (!rule.IsPlaceable(cell)) continue;
            if (random.NextFloat() > rule.Chance) continue;

            var worldX = ctx.ChunkX * size + localX;
            var worldY = ctx.ChunkY * size + localY;

            var (width, height) = rule.GenerateSize(random);

            world.Structures.Add(new TerrainStructure
            {
                X = worldX,
                Y = worldY,
                Width = width,
                Height = height,
                Type = rule.Type
            });

            break; // one structure per cell
        }
    }

    static int Hash(int x, int y)
    {
        unchecked
        {
            return (x * 73856093) ^ (y * 19349663);
        }
    }
}