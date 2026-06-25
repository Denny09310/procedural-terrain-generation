using Core.Models;

namespace Core.Services;

public sealed class TerrainBuilder(int size, int seed)
{
    private readonly List<TerrainHandler> _transformers = [];

    private TerrainSettings _settings = new(
        ChunkSize: size,
        Octaves: 4,
        Persistence: 0.5,
        Shape: TerrainShape.Archipelago,
        Elevation: new TerrainLayer(BlockSize: size / 10, NoiseSeed: seed),
        Moisture: new TerrainLayer(BlockSize: size / 6, NoiseSeed: seed ^ 0x4F3A1C2B),
        Temperature: new TerrainLayer(BlockSize: size / 5, NoiseSeed: seed ^ 0x9E3779B9));

    public TerrainBuilder WithSettings(Func<TerrainSettings, TerrainSettings> configure)
    {
        _settings = configure(_settings);
        return this;
    }

    public TerrainBuilder WithTransformer<TDelegate>(TDelegate transformer)
        where TDelegate : Delegate
    {
        _transformers.Add(new TerrainHandler(
            transformer,
            transformer.Method,
            transformer.Method.GetParameters()));

        return this;
    }

    public TerrainWorld Build() => new(seed, _settings, _transformers);
}
