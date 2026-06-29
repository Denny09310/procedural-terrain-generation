using Core.Interfaces;

namespace Core.Models;

public sealed class DungeonRule : IStructureRule
{
    public StructureType Type => StructureType.Dungeon;

    public int Priority => 90;

    public float Chance => 0.02f;

    public bool IsPlaceable(TerrainCell cell) =>
        cell.Biome == BiomeType.Mountain &&
        cell.Elevation > 0.8f;


    public TerrainSize GenerateSize(ITerrainRandomizer random)
    {
        return new TerrainSize(
            random.NextInt(5, 11),
            random.NextInt(4, 8));
    }
}