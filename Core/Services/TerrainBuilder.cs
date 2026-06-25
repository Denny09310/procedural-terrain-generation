using Core.Models;

namespace Core.Services;

public sealed class TerrainBuilder(int size, int seed)
{
    private readonly List<Delegate> _transformers = [];

    private TerrainSettings _settings = new(
        ChunkSize: size,
        Octaves: 4,
        Persistence: 0.5,
        Shape: TerrainShape.Archipelago,
        Elevation: new TerrainLayerSetting(BlockSize: size / 10, NoiseSeed: seed),
        Moisture: new TerrainLayerSetting(BlockSize: size / 6, NoiseSeed: seed ^ 0x4F3A1C2B),
        Temperature: new TerrainLayerSetting(BlockSize: size / 5, NoiseSeed: seed ^ 0x9E3779B9));

    public TerrainBuilder WithSettings(Func<TerrainSettings, TerrainSettings> configure)
    {
        _settings = configure(_settings);
        return this;
    }

    public TerrainBuilder WithTransformer(Delegate transformer)
    {
        _transformers.Add(transformer);
        return this;
    }

    public TerrainWorld Build() => new(seed, _settings, _transformers);
}
