using Core.Models;

namespace Core.Helpers;

public static class RiversHelpers
{
    public static void GenerateRivers(Cell[,] world, TerrainContext context)
    {
        var random = new Random(context.Seed);

        int height = world.GetLength(0);
        int width = world.GetLength(1);

        var visited = new bool[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (IsRiverSource(world, random, width, height, x, y))
                    TraceRiver(world, visited, width, height, x, y);
    }

    static bool IsRiverSource(
        Cell[,] world,
        Random random,
        int width,
        int height,
        int x,
        int y)
    {
        var cell = world[y, x];

        if (cell.Elevation < 0.70) return false;

        int target = (int)Math.Sqrt(width * height) / 4;
        double probability = target / (double)(width * height);

        return random.NextDouble() <= probability;
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

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= width ||
                    ny < 0 || ny >= height)
                {
                    continue;
                }

                var neighbor = world[ny, nx];

                if (neighbor.Elevation < lowestElevation)
                {
                    lowestElevation = neighbor.Elevation;
                    lowestX = nx;
                    lowestY = ny;
                }
            }
        }

        return (lowestX, lowestY);
    }

    private static void TraceRiver(
        Cell[,] world,
        bool[,] visited,
        int width,
        int height,
        int startX,
        int startY)
    {
        int currentX = startX;
        int currentY = startY;

        Array.Clear(visited);

        while (true)
        {
            if (visited[currentY, currentX])
                break;

            visited[currentY, currentX] = true;

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
}
