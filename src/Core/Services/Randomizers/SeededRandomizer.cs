using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public sealed class SeededRandomizer(TerrainConfiguration config) : ITerrainRandomizer
{
    private readonly ThreadLocal<Random> _random = new(() => new Random(config.Seed));

    public float NextFloat() => _random.Value!.NextSingle();
    public float NextFloat(float min, float max) => min + _random.Value!.NextSingle() * (max - min);
    public int NextInt(int min, int max) => _random.Value!.Next(min, max);
}