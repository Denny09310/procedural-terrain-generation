namespace Core.Interfaces;

/// <summary>
/// Samples 2D noise. Values are in the range [-1, 1].
/// </summary>
public interface INoiseSource
{
    float Sample(float x, float y);
}

public static class NoiseSourceExtensions
{
    /// <summary>
    /// Fractal Brownian Motion — layers multiple octaves for natural-looking terrain.
    /// </summary>
    public static float SampleFractal(
        this INoiseSource noise,
        float x,
        float y,
        int octaves = 4,
        float frequency = 1f,
        float persistence = 0.5f,
        float lacunarity = 2f)
    {
        float value = 0f;
        float amplitude = 1f;
        float totalAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value += noise.Sample(x * frequency, y * frequency) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / totalAmplitude;
    }
}
