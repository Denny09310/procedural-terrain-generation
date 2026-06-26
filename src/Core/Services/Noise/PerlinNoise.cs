using Core.Interfaces;

namespace Core.Services;

public sealed class PerlinNoise : INoiseSource
{
    private readonly int[] _perm;

    public PerlinNoise(TerrainConfiguration config)
    {
        var rng = new Random(config.Seed);
        var p = Enumerable.Range(0, 256).OrderBy(_ => rng.Next()).ToArray();

        _perm = new int[512];
        for (int i = 0; i < 512; i++)
            _perm[i] = p[i & 255];
    }

    public float Sample(float x, float y)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;

        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);

        float u = Fade(xf);
        float v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

        return Lerp(x1, x2, v);
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float Grad(int hash, float x, float y) => (hash & 3) switch
    {
        0 => x + y,
        1 => -x + y,
        2 => x - y,
        3 => -x - y,
        _ => 0
    };
}
