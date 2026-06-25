namespace Core.Models;

public sealed record TerrainChunk(
    ChunkCoordinates Coordinates,
    Cell[,] Cells);

public readonly record struct ChunkCoordinates(
    int X,
    int Y);