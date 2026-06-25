using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed class TerrainWorld : IDisposable
{
    private readonly Dictionary<(int, int), TerrainChunk> _cache = [];
    private readonly int _seed;
    private readonly TerrainSettings _settings;
    private readonly IReadOnlyList<TerrainTransformer> _transformers;
    private readonly IServiceProvider _provider;

    public TerrainWorld(
        int seed,
        TerrainSettings settings,
        IReadOnlyList<Delegate> delegates)
    {
        var services = new ServiceCollection();

        services.AddSingleton(settings);

        services.AddSingleton(_ => new TerrainSeed(seed));
        services.AddSingleton<ElevationProvider>();
        services.AddSingleton<RiversProvider>();

        _provider = services.BuildServiceProvider();

        _seed = seed;
        _settings = settings;

        _transformers = delegates
            .Select(x => TerrainTransformerBinder.Bind(x.Method, _provider))
            .ToList();
    }


    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

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
