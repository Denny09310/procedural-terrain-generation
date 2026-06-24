using Core.Models;

namespace Core.Helpers;

public static class SettlementHelpers
{
    public static void AddSettlements(Cell[,] world, TerrainContext context)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        // 1. Get the global world origin coordinates for this specific chunk
        int chunkOriginX = TerrainCoordinates.WorldX(context, 0);
        int chunkOriginY = TerrainCoordinates.WorldY(context, 0);

        // 2. Create a deterministic seed unique to THIS chunk's location
        // We use HashCode.Combine so the same chunk always produces the exact same random sequence
        Random rand = new(context.Seed);

        // 3. Rarity Check: 5% chance for a chunk to host a settlement center
        if (rand.NextDouble() > 0.05) return;

        // 4. Pick a deterministic center point inside this chunk
        int centerX = rand.Next(2, width - 3);
        int centerY = rand.Next(2, height - 3);

        ref var center = ref world[centerY, centerX];

        // 5. Validation: Don't spawn cities on water, mountains, or snow
        if (center.Biome == Biome.Water ||
            center.Biome == Biome.Mountains ||
            center.Biome == Biome.Snow)
        {
            return;
        }

        // Place the city hub
        center.Structure = StructureType.CityCenter;

        // 6. Procedurally grow a small village around the center
        int radius = rand.Next(context.Settings.ChunkSize / 16, context.Settings.ChunkSize / 8);

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                // Stay safely within the local chunk array boundaries for now
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                ref var housing = ref world[y, x];

                // Don't overwrite the center, water, or mountains
                if (housing.Structure != StructureType.None ||
                    housing.Biome == Biome.Water ||
                    housing.Biome == Biome.Mountains)
                {
                    continue;
                }

                // 40% chance to place a house in valid radius slots
                if (rand.NextDouble() < 0.40)
                {
                    housing.Structure = StructureType.House;
                }
            }
        }
    }
}