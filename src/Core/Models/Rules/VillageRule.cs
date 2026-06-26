using Core.Interfaces;

namespace Core.Models;

public sealed class VillageRule : IStructureRule
{
    public StructureType StructureType => StructureType.Village;

    public int Priority => 100;

    public float Chance => 0.015f;

    public bool IsPlaceable(TerrainCell cell)
    {
        return cell.Biome is TerrainBiome.Grassland
            or TerrainBiome.Steppa
            or TerrainBiome.TropicalForest
            or TerrainBiome.TemperateForest
            or TerrainBiome.Desert;
    }
}
