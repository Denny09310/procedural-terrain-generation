namespace Core.Services;

public sealed class TerrainNoise(int blockSize, long seed)
{
    public double Core(double x, double y)
    {
        double gx = x / blockSize;
        double gy = y / blockSize;

        int x0 = (int)Math.Floor(gx);
        int y0 = (int)Math.Floor(gy);

        int x1 = x0 + 1;
        int y1 = y0 + 1;

        double v00 = GetValue(x0, y0);
        double v10 = GetValue(x1, y0);
        double v01 = GetValue(x0, y1);
        double v11 = GetValue(x1, y1);

        double tx = Smooth(gx - x0);
        double ty = Smooth(gy - y0);

        double top = Lerp(v00, v10, tx);
        double bottom = Lerp(v01, v11, tx);

        return Lerp(top, bottom, ty);
    }

    public double CoreFractal(
        double x,
        double y,
        int octaves,
        double persistence)
    {
        double total = 0;
        double amplitude = 1;
        double frequency = 1;

        double amplitudeSum = 0;

        for (int i = 0; i < octaves; i++)
        {
            total +=
                Core(
                    x * frequency,
                    y * frequency)
                * amplitude;

            amplitudeSum += amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return total / amplitudeSum;
    }

    private double GetValue(int x, int y)
    {
        uint h = Hash(seed, x, y);
        return h / (double)uint.MaxValue;
    }

    private static uint Hash(long seed, int x, int y)
    {
        unchecked
        {
            ulong h = (ulong)seed ^ 0x9E3779B97F4A7C15UL;
            h += (ulong)x * 0xD2A98B26625EEE7BUL;
            h += (ulong)y * 0xA3F5E6A3B1C9D7E5UL;
            h ^= h >> 33;
            h *= 0xFF51AFD7ED558CCDUL;
            h ^= h >> 33;
            h *= 0xC4CEB9FE1A85EC53UL;
            return (uint)(h ^ (h >> 33));
        }
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    private static double Smooth(double t)
    {
        return t * t * (3 - 2 * t);
    }
}
