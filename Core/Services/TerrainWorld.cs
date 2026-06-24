using Core.Models;

namespace Core.Services;

public sealed class TerrainWorld(
    int seed,
    TerrainSettings settings,
    IReadOnlyList<TerrainTransformer> transformers)
{
    private readonly Dictionary<(int, int), TerrainChunk> _cache = [];

    public TerrainChunk GetChunk(
        int chunkX,
        int chunkY)
    {
        if (_cache.TryGetValue((chunkX, chunkY), out var chunk))
        {
            return chunk;
        }

        chunk = GenerateChunk(chunkX, chunkY);

        _cache[(chunkX, chunkY)] = chunk;

        return chunk;
    }

    private TerrainChunk GenerateChunk(
        int chunkX,
        int chunkY)
    {
        int size = settings.ChunkSize;

        var cells = new Cell[size, size];

        Fill(cells);

        var context =
            new TerrainContext(
                Seed: seed,
                Settings: settings,
                ChunkX: chunkX,
                ChunkY: chunkY);

        foreach (var transformer in transformers)
        {
            transformer(cells, context);
        }

        return new TerrainChunk(
            chunkX,
            chunkY,
            cells);
    }

    private static void Fill(Cell[,] cells)
    {
        int height = cells.GetLength(0);
        int width = cells.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells[y, x] = new Cell();
            }
        }
    }
}