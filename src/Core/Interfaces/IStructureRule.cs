using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Defines when and how often a particular structure type may appear on a cell.
/// Register implementations in DI and let <see cref="Core.Services.Structures.StructureTransformer"/>
/// evaluate them in priority order.
/// </summary>
public interface IStructureRule
{
    /// <summary>The structure this rule places.</summary>
    StructureType StructureType { get; }

    /// <summary>
    /// Priority: rules are evaluated in descending order so that rarer, more
    /// specific structures (higher priority) get first refusal on a cell.
    /// </summary>
    int Priority { get; }

    /// <summary>Probability [0–1] that the structure is placed when the rule matches.</summary>
    float Chance { get; }

    /// <summary>Returns true when the cell's terrain is suitable for this structure.</summary>
    bool IsPlaceable(TerrainCell cell);
}
