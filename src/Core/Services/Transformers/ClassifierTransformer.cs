using Core.Models;

namespace Core.Services;

public sealed class ClassifierTransformer()
{
    private const float DeepOcean = 0.20f;
    private const float Ocean = 0.30f;
    private const float ShallowWater = 0.34f;
    private const float Beach = 0.36f;

    private const float Alpine = 0.70f;
    private const float Mountain = 0.87f;
    private const float Snow = 0.94f;
    private const float Glacier = 0.98f;

    private static readonly TerrainBiome[,] Climate =
    {
        // Polar
        {
            TerrainBiome.Tundra,
            TerrainBiome.Tundra,
            TerrainBiome.Tundra,
            TerrainBiome.Snow,
            TerrainBiome.Glacier
        },

        // Cold
        {
            TerrainBiome.Steppa,
            TerrainBiome.Grassland,
            TerrainBiome.BorealForest,
            TerrainBiome.Taiga,
            TerrainBiome.Taiga
        },

        // Temperate
        {
            TerrainBiome.Shrubland,
            TerrainBiome.Grassland,
            TerrainBiome.TemperateForest,
            TerrainBiome.TemperateRainforest,
            TerrainBiome.TemperateRainforest
        },

        // Warm
        {
            TerrainBiome.Desert,
            TerrainBiome.Steppa,
            TerrainBiome.Savanna,
            TerrainBiome.TropicalForest,
            TerrainBiome.Jungle
        },

        // Tropical
        {
            TerrainBiome.Desert,
            TerrainBiome.Savanna,
            TerrainBiome.TropicalForest,
            TerrainBiome.Jungle,
            TerrainBiome.TropicalRainforest
        }
    };

    public static TerrainBiome Classify(TerrainCell cell)
    {
        float elevation = cell.Elevation;
        float temperature = cell.Temperature;
        float moisture = cell.Moisture;

        // ----------------------------
        // Water
        // ----------------------------

        if (elevation < DeepOcean)
            return TerrainBiome.DeepOcean;

        if (elevation < Ocean)
            return TerrainBiome.Ocean;

        if (elevation < ShallowWater)
            return TerrainBiome.ShallowWater;

        if (elevation < Beach)
            return TerrainBiome.Beach;

        // ----------------------------
        // Mountains
        // ----------------------------

        if (elevation >= Glacier)
            return TerrainBiome.Glacier;

        if (elevation >= Snow)
            return TerrainBiome.Snow;

        if (elevation >= Mountain)
            return TerrainBiome.Mountain;

        if (elevation >= Alpine)
        {
            if (temperature < 0.30f)
                return TerrainBiome.Alpine;

            return TerrainBiome.SubAlpine;
        }

        // ----------------------------
        // Climate
        // ----------------------------

        int t = TemperatureBand(temperature);
        int m = MoistureBand(moisture);

        TerrainBiome biome = Climate[t, m];

        // ----------------------------
        // Local overrides
        // ----------------------------

        // Wet lowlands become wetlands.
        if (elevation < 0.45f && moisture > 0.90f)
        {
            if (temperature > 0.75f)
                biome = TerrainBiome.Swamp;
            else
                biome = TerrainBiome.Wetland;
        }

        // Tropical coastal wetlands.
        if (elevation < 0.40f &&
            moisture > 0.85f &&
            temperature > 0.80f)
        {
            biome = TerrainBiome.Mangrove;
        }

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
