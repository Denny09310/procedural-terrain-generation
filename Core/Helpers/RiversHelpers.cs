using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class RiversHelpers
{
    public static void GenerateRivers(
        Cell[,] world,
        TerrainContext context,
        RiversProvider provider)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);
                int worldY = TerrainCoordinates.WorldY(context, y);

                ref var cell = ref world[y, x];

                cell.River = provider.GetRiver(worldX, worldY);
            }
        }
    }
}