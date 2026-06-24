using Core.Models;

namespace Core.Helpers;

public static class TerrainHelpers
{
    public static void ClassifyTerrain(Cell[,] world, TerrainContext _)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                ref var cell = ref world[y, x];

                if (cell.River > 0)
                {
                    cell.Biome = Biome.River;
                    continue;
                }

                cell.Biome = cell.Elevation switch
                {
                    > 0.90 when cell.Temperature < 0.25 => Biome.Snow,
                    > 0.90 => Biome.Mountains,
                    > 0.75 when cell.Temperature < 0.30 => Biome.Snow,
                    > 0.75 => Biome.Hills,
                    > 0.20 when cell.Moisture < 0.25 => Biome.Desert,
                    > 0.20 when cell.Moisture < 0.60 => Biome.Plains,
                    > 0.20 => Biome.Forest,
                    > 0.10 => Biome.Beach,
                    _ => Biome.Water
                };
            }
        }
    }

}
