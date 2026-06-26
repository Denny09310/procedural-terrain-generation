using Core.Interfaces;

namespace Core.Services;

public sealed class SeededRandomizer(TerrainConfiguration config) : ITerrainRandomizer
{
    private readonly Random _rng = new(config.Seed);

    public float NextFloat() => _rng.NextSingle();
    public float NextFloat(float min, float max) => min + _rng.NextSingle() * (max - min);
    public int NextInt(int min, int max) => _rng.Next(min, max);
}