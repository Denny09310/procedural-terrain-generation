using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class ElevationHelpers
{
    public static void AddElevation(
        Cell[,] world,
        TerrainContext context,
        ElevationProvider provider)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        Parallel.For(0, height, y =>
        {
            int worldY = TerrainCoordinates.WorldY(context, y);

            for (int x = 0; x < width; x++)
            {
                int worldX = TerrainCoordinates.WorldX(context, x);

                ref var cell = ref world[y, x];

                cell.Elevation = provider.GetElevation(worldX, worldY);
            }
        });
    }
}