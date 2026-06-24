using Core.Models;

namespace Core.Helpers;

public static class RiversHelpers
{
    public static void GenerateRivers(Cell[,] world, TerrainContext context)
    {
        var random = new Random(context.Seed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        int total = width * height;
        int target = (int)Math.Sqrt(total) / 4;
        double probability = target / (double)total;

        int[,] visited = new int[height, width];
        int currentId = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (world[y, x].Elevation >= 0.70 && random.NextDouble() <= probability)
                {
                    currentId++;
                    TraceRiver(world, visited, currentId, width, height, x, y);
                }
            }
        }
    }

    private static void TraceRiver(
        Cell[,] world,
        int[,] visited,
        int traceId,
        int width,
        int height,
        int startX,
        int startY)
    {
        int currentX = startX;
        int currentY = startY;

        while (true)
        {
            if (visited[currentY, currentX] == traceId)
                break;

            visited[currentY, currentX] = traceId;

            if (world[currentY, currentX].Elevation <= 0.3)
                break;

            var (nextX, nextY) = FindLowestNeighbor(world, width, height, currentX, currentY);

            if (nextX == currentX && nextY == currentY)
                break;

            world[nextY, nextX].River += 1.0;

            currentX = nextX;
            currentY = nextY;
        }
    }

    private static (int X, int Y) FindLowestNeighbor(
        Cell[,] world,
        int width,
        int height,
        int x,
        int y)
    {
        int lowestX = x;
        int lowestY = y;
        double lowestElevation = world[y, x].Elevation;

        int minY = Math.Max(0, y - 1);
        int maxY = Math.Min(height - 1, y + 1);
        int minX = Math.Max(0, x - 1);
        int maxX = Math.Min(width - 1, x + 1);

        for (int ny = minY; ny <= maxY; ny++)
        {
            for (int nx = minX; nx <= maxX; nx++)
            {
                if (nx == x && ny == y)
                    continue;

                double neighborElevation = world[ny, nx].Elevation;

                if (neighborElevation < lowestElevation)
                {
                    lowestElevation = neighborElevation;
                    lowestX = nx;
                    lowestY = ny;
                }
            }
        }

        return (lowestX, lowestY);
    }
}