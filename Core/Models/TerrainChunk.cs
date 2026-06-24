namespace Core.Models;

public sealed record TerrainChunk(
    int ChunkX,
    int ChunkY,
    Cell[,] Cells);