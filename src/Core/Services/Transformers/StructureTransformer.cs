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
    ITerrainRandomizer randomizer)
{
    private readonly IStructureRule[] _rules =
        [.. rules.OrderByDescending(r => r.Priority)];

    /// <summary>
    /// Walks every cell in <paramref name="grid"/> and, for each rule in priority order,
    /// places the structure if <c>CanPlace</c> passes and a random roll beats
    /// <c>SpawnChance</c>.  Only one structure is placed per cell.
    /// </summary>
    public void Apply(TerrainGrid grid)
    {
        foreach (var cell in grid)
        {
            foreach (var rule in _rules)
            {
                if (!rule.IsPlaceable(cell)) continue;
                if (randomizer.NextFloat() > rule.Chance) continue;

                cell.Structure = rule.StructureType;
                break; // one structure per cell
            }
        }
    }
}