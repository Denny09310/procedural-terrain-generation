using System.Diagnostics.CodeAnalysis;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainWorld : IDisposable
{
    private readonly Dictionary<ChunkCoordinates, TerrainChunk> _cache = [];
    private readonly int _seed;
    private readonly TerrainSettings _settings;
    private readonly IReadOnlyList<TerrainTransformer> _transformers;
    private readonly IServiceProvider _provider;

    public TerrainWorld(
        int seed,
        TerrainSettings settings,
        IReadOnlyList<TerrainHandler> handlers)
    {
        var services = new ServiceCollection();

        services.AddSingleton(settings);

        services.AddSingleton(_ => new TerrainSeed(seed));
        services.AddSingleton<ElevationProvider>();
        services.AddSingleton<RiversProvider>();

        _provider = services.BuildServiceProvider();

        _seed = seed;
        _settings = settings;

        _transformers = handlers
            .Select(x => TerrainTransformerBinder.Bind(x, _provider))
            .ToList();
    }

    public IEnumerable<ChunkCoordinates> Chunks => _cache.Keys;

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public TerrainChunk LoadChunk(
        int chunkX,
        int chunkY)
    {
        if (_cache.TryGetValue(new(chunkX, chunkY), out var chunk))
        {
            return chunk;
        }

        chunk = GenerateChunk(chunkX, chunkY);

        _cache[new(chunkX, chunkY)] = chunk;

        return chunk;
    }

    public bool IsChunkLoaded(
        int chunkX,
        int chunkY)
    {
        return _cache.ContainsKey(new(chunkX, chunkY));
    }

    public bool TryGetLoadedChunk(
        int chunkX,
        int chunkY,
        [NotNullWhen(true)] out TerrainChunk? chunk)
    {
        return _cache.TryGetValue(new(chunkX, chunkY), out chunk);
    }

    public bool UnloadChunk(
        int chunkX,
        int chunkY)
    {
        return _cache.Remove(new(chunkX, chunkY));
    }

    public void UnloadChunks(IEnumerable<ChunkCoordinates> coordinates)
    {
        foreach (var (chunkX, chunkY) in coordinates)
        {
            _cache.Remove(new(chunkX, chunkY));
        }
    }

    public void UnloadAllChunks()
    {
        _cache.Clear();
    }

    private TerrainChunk GenerateChunk(
        int chunkX,
        int chunkY)
    {
        int size = _settings.ChunkSize;

        var cells = new Cell[size, size];

        Fill(cells);

        var context =
            new TerrainContext(
                Seed: _seed,
                ChunkX: chunkX,
                ChunkY: chunkY,
                Settings: _settings);

        foreach (var transformer in _transformers)
        {
            transformer(cells, context);
        }

        return new TerrainChunk(
            new ChunkCoordinates(chunkX, chunkY),
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
