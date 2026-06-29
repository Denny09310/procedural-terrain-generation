using Core.Models;

namespace Core.Interfaces;

public interface IStructureRule
{
    StructureType Type { get; }
    int Priority { get; }
    float Chance { get; }

    bool IsPlaceable(TerrainCell cell);
    TerrainSize GenerateSize(ITerrainRandomizer random);
}
