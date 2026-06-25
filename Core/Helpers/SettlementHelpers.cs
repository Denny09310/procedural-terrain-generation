using Core.Models;
using Core.Services;

namespace Core.Helpers;

public static class SettlementHelpers
{
    public static void AddSettlements(Cell[,] world, TerrainContext context)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        int chunkOriginX = TerrainCoordinates.WorldX(context, 0);
        int chunkOriginY = TerrainCoordinates.WorldY(context, 0);

        int chunkSeed = TerrainNoise.Combine(context.Seed, chunkOriginX, chunkOriginY);
        Random rand = new(chunkSeed);

        // 5% chance this chunk contains a settlement.
        if (rand.NextDouble() > 0.05)
            return;

        int centerX = rand.Next(3, width - 3);
        int centerY = rand.Next(3, height - 3);

        ref var center = ref world[centerY, centerX];

        if (center.Biome == Biome.Water ||
            center.Biome == Biome.Mountains ||
            center.Biome == Biome.Snow)
        {
            return;
        }

        center.Structure = StructureType.CityCenter;

        int radius = rand.Next(
            context.Settings.ChunkSize / 16,
            context.Settings.ChunkSize / 8);

        GenerateRoads(world, rand, centerX, centerY, radius);

        PlaceHouses(world, rand, centerX, centerY, radius);
    }

    private static void GenerateRoads(
        Cell[,] world,
        Random rand,
        int centerX,
        int centerY,
        int radius)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        // Main cross roads.
        DrawRoad(world, centerX, centerY, 1, 0, radius);
        DrawRoad(world, centerX, centerY, -1, 0, radius);
        DrawRoad(world, centerX, centerY, 0, 1, radius);
        DrawRoad(world, centerX, centerY, 0, -1, radius);

        // Extra roads (2-4)
        int roads = rand.Next(2, 5);

        for (int i = 0; i < roads; i++)
        {
            int dir = rand.Next(4);

            int dx = 0;
            int dy = 0;

            switch (dir)
            {
                case 0: dx = 1; break;
                case 1: dx = -1; break;
                case 2: dy = 1; break;
                case 3: dy = -1; break;
            }

            int offset = rand.Next(2, Math.Max(3, radius));

            int startX = centerX + (dy != 0 ? offset : 0);
            int startY = centerY + (dx != 0 ? offset : 0);

            if (startX < 1 || startX >= width - 1 ||
                startY < 1 || startY >= height - 1)
            {
                continue;
            }

            DrawRoad(
                world,
                startX,
                startY,
                dx,
                dy,
                rand.Next(radius / 2, radius + 1));
        }

        world[centerY, centerX].Structure = StructureType.CityCenter;
    }

    private static void DrawRoad(
        Cell[,] world,
        int startX,
        int startY,
        int dx,
        int dy,
        int length)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        int x = startX;
        int y = startY;

        for (int i = 0; i < length; i++)
        {
            if (x < 0 || x >= width ||
                y < 0 || y >= height)
            {
                break;
            }

            ref Cell cell = ref world[y, x];

            if (cell.Biome == Biome.Water ||
                cell.Biome == Biome.Mountains)
            {
                break;
            }

            if (cell.Structure == StructureType.None)
            {
                cell.Structure = StructureType.Road;
            }

            x += dx;
            y += dy;
        }
    }

    private static void PlaceHouses(
        Cell[,] world,
        Random rand,
        int centerX,
        int centerY,
        int radius)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        int minX = Math.Max(0, centerX - radius);
        int maxX = Math.Min(width - 1, centerX + radius);

        int minY = Math.Max(0, centerY - radius);
        int maxY = Math.Min(height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                ref Cell cell = ref world[y, x];

                if (cell.Structure != StructureType.None)
                    continue;

                if (cell.Biome == Biome.Water ||
                    cell.Biome == Biome.Mountains)
                    continue;

                if (!IsAdjacentToRoad(world, x, y))
                    continue;

                if (rand.NextDouble() < 0.55)
                {
                    cell.Structure = StructureType.House;
                }
            }
        }
    }

    private static bool IsAdjacentToRoad(Cell[,] world, int x, int y)
    {
        int height = world.GetLength(0);
        int width = world.GetLength(1);

        if (x > 0 &&
            (world[y, x - 1].Structure == StructureType.Road ||
             world[y, x - 1].Structure == StructureType.CityCenter))
            return true;

        if (x < width - 1 &&
            (world[y, x + 1].Structure == StructureType.Road ||
             world[y, x + 1].Structure == StructureType.CityCenter))
            return true;

        if (y > 0 &&
            (world[y - 1, x].Structure == StructureType.Road ||
             world[y - 1, x].Structure == StructureType.CityCenter))
            return true;

        if (y < height - 1 &&
            (world[y + 1, x].Structure == StructureType.Road ||
             world[y + 1, x].Structure == StructureType.CityCenter))
            return true;

        return false;
    }
}