using Core.Models;

namespace Core.Services;

public sealed class ClassifierTransformer
{
    private const float DeepOcean = 0.20f;
    private const float Ocean = 0.30f;
    private const float ShallowWater = 0.34f;
    private const float Beach = 0.36f;

    private const float Alpine = 0.70f;
    private const float Mountain = 0.87f;
    private const float Snow = 0.94f;
    private const float Glacier = 0.98f;

    private static readonly BiomeType[,] Climate =
    {
        // Polar
        {
            BiomeType.Tundra,
            BiomeType.Tundra,
            BiomeType.Tundra,
            BiomeType.Snow,
            BiomeType.Glacier
        },

        // Cold
        {
            BiomeType.Steppe,
            BiomeType.Grassland,
            BiomeType.BorealForest,
            BiomeType.Taiga,
            BiomeType.Taiga
        },

        // Temperate
        {
            BiomeType.Shrubland,
            BiomeType.Grassland,
            BiomeType.TemperateForest,
            BiomeType.TemperateRainforest,
            BiomeType.TemperateRainforest
        },

        // Warm
        {
            BiomeType.Desert,
            BiomeType.Steppe,
            BiomeType.Savanna,
            BiomeType.TropicalForest,
            BiomeType.Jungle
        },

        // Tropical
        {
            BiomeType.Desert,
            BiomeType.Savanna,
            BiomeType.TropicalForest,
            BiomeType.Jungle,
            BiomeType.TropicalRainforest
        }
    };

    public static BiomeType Classify(TerrainCell cell)
    {
        float elevation = cell.Elevation;
        float temperature = cell.Temperature;
        float moisture = cell.Moisture;

        // ----------------------------
        // Water
        // ----------------------------

        if (elevation < DeepOcean) return BiomeType.DeepOcean;
        if (elevation < Ocean) return BiomeType.Ocean;
        if (elevation < ShallowWater) return BiomeType.ShallowWater;
        if (elevation < Beach) return BiomeType.Beach;

        // ----------------------------
        // Mountains
        // ----------------------------

        if (elevation >= Glacier) return BiomeType.Glacier;
        if (elevation >= Snow) return BiomeType.Snow;
        if (elevation >= Mountain) return BiomeType.Mountain;

        if (elevation >= Alpine)
        {
            return temperature < 0.30f
                ? BiomeType.Alpine
                : BiomeType.SubAlpine;
        }

        // ----------------------------
        // Climate matrix
        // ----------------------------

        int t = TemperatureBand(temperature);
        int m = MoistureBand(moisture);

        BiomeType biome = Climate[t, m];

        // ----------------------------
        // Local overrides
        // ----------------------------

        // Wet lowlands → wetlands.
        if (elevation < 0.45f && moisture > 0.90f)
        {
            biome = temperature > 0.75f
                ? BiomeType.Swamp
                : BiomeType.Wetland;
        }

        // Tropical coastal wetlands.
        if (elevation < 0.40f && moisture > 0.85f && temperature > 0.80f)
            biome = BiomeType.Mangrove;

        return biome;
    }

    public void Apply(TerrainGrid grid)
    {
        foreach (var cell in grid)
            cell.Biome = Classify(cell);
    }

    private static int TemperatureBand(float t)
    {
        if (t < 0.15f) return 0; // Polar
        if (t < 0.35f) return 1; // Cold
        if (t < 0.60f) return 2; // Temperate
        if (t < 0.80f) return 3; // Warm
        return 4;                // Tropical
    }

    private static int MoistureBand(float m)
    {
        if (m < 0.15f) return 0;
        if (m < 0.35f) return 1;
        if (m < 0.55f) return 2;
        if (m < 0.75f) return 3;
        return 4;
    }
}
