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

        var elevations = new double[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevations[y, x] = world[y, x].Elevation;

        var visited = new int[height, width];
        int current = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (elevations[y, x] >= 0.70 && random.NextDouble() <= probability)
                {
                    current++;
                    TraceRiver(world, elevations, visited, current, width, height, x, y);
                }
            }
        }
    }

    private static void TraceRiver(
        Cell[,] world,
        double[,] elevations,
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

            if (elevations[currentY, currentX] <= 0.3)
                break;

            var (nextX, nextY) = FindLowestNeighbor(elevations, width, height, currentX, currentY);

            if (nextX == currentX && nextY == currentY)
                break;

            world[nextY, nextX].River += 1.0;

            currentX = nextX;
            currentY = nextY;
        }
    }

    private static (int X, int Y) FindLowestNeighbor(
        double[,] elevations,
        int width,
        int height,
        int x,
        int y)
    {
        double lowest = elevations[y, x];
        int lowestX = x;
        int lowestY = y;

        bool hasLeft = x > 0;
        bool hasRight = x < width - 1;
        bool hasTop = y > 0;
        bool hasBottom = y < height - 1;

        double e;

        if (hasTop)
        {
            int ny = y - 1;
            if (hasLeft && (e = elevations[ny, x - 1]) < lowest) { lowest = e; lowestX = x - 1; lowestY = ny; }
            if ((e = elevations[ny, x]) < lowest) { lowest = e; lowestX = x; lowestY = ny; }
            if (hasRight && (e = elevations[ny, x + 1]) < lowest) { lowest = e; lowestX = x + 1; lowestY = ny; }
        }

        if (hasLeft && (e = elevations[y, x - 1]) < lowest) { lowest = e; lowestX = x - 1; lowestY = y; }
        if (hasRight && (e = elevations[y, x + 1]) < lowest) { lowest = e; lowestX = x + 1; lowestY = y; }

        if (hasBottom)
        {
            int ny = y + 1;
            if (hasLeft && (e = elevations[ny, x - 1]) < lowest) { lowest = e; lowestX = x - 1; lowestY = ny; }
            if ((e = elevations[ny, x]) < lowest) { lowest = e; lowestX = x; lowestY = ny; }
            if (hasRight && (e = elevations[ny, x + 1]) < lowest) { lowest = e; lowestX = x + 1; lowestY = ny; }
        }

        return (lowestX, lowestY);
    }
}