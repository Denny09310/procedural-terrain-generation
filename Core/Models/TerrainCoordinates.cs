namespace Core.Models;

public static class TerrainCoordinates
{
    public static int WorldX(
        TerrainContext context,
        int localX)
    {
        return
            context.ChunkX *
            context.Settings.Size +
            localX;
    }

    public static int WorldY(
        TerrainContext context,
        int localY)
    {
        return
            context.ChunkY *
            context.Settings.Size +
            localY;
    }
}