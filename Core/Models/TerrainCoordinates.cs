namespace Core.Models;

public static class TerrainCoordinates
{
    public static int WorldX(TerrainContext context, int localX)
    {
        return
            context.ChunkX *
            context.Settings.ChunkSize +
            localX;
    }

    public static int WorldY(TerrainContext context, int localY)
    {
        return
            context.ChunkY *
            context.Settings.ChunkSize +
            localY;
    }
}