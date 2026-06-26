namespace Core.Models;

public enum TerrainBiome
{
    // Water
    Ocean,
    DeepOcean,
    ShallowWater,
    River,

    // Coastal / Low elevation
    Beach,
    Wetland,
    Swamp,
    Mangrove,

    // Arid / Hot
    Desert,
    Savanna,
    Steppa,

    // Temperate
    Grassland,
    Shrubland,
    TemperateForest,
    TemperateRainforest,

    // Tropical / High moisture
    TropicalForest,
    TropicalRainforest,
    Jungle,

    // Cold
    Taiga,
    BorealForest,
    Tundra,

    // High elevation
    Alpine,
    SubAlpine,
    Mountain,
    Snow,
    Glacier,

    // Special
    Volcanic,
    Unknown
}
