using Core.Interfaces;

namespace Core.Models;

public sealed class VillageRule : IStructureRule
{
    public StructureType Type => StructureType.Village;

    public int Priority => 100;

    public float Chance => 0.015f;

    public bool IsPlaceable(TerrainCell cell) => cell.Biome is BiomeType.Grassland
        or BiomeType.Steppe
        or BiomeType.TropicalForest
        or BiomeType.TemperateForest
        or BiomeType.Desert;

    public TerrainSize GenerateSize(ITerrainRandomizer random)
    {
        var size = random.NextInt(6, 13);
        return new TerrainSize(size, size);
    }
}
