namespace Core.Models;

/// <summary>
/// A structure placed in the world at absolute world-space coordinates.
/// Not tied to any specific <see cref="TerrainCell"/> or <see cref="TerrainGrid"/> —
/// a structure can span chunk boundaries and outlive individual chunk lifetimes.
/// </summary>
public sealed class TerrainStructure
{
    /// <summary>World-space X coordinate of the structure's origin (top-left cell).</summary>
    public int X { get; init; }

    /// <summary>World-space Y coordinate of the structure's origin (top-left cell).</summary>
    public int Y { get; init; }

    /// <summary>Footprint width in world cells.</summary>
    public int Width { get; init; } = 1;

    /// <summary>Footprint height in world cells.</summary>
    public int Height { get; init; } = 1;

    public StructureType Type { get; init; }
}
