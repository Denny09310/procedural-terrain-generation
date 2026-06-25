namespace Core.Services;

public sealed class TerrainNoise(int blockSize, long seed)
{
    private readonly double _inverted = 1.0 / blockSize;
    private readonly ulong _hash = (ulong)seed ^ 0x9E3779B97F4A7C15UL;

    // Finalized as constants so the JIT can embed them directly at call sites
    private const double InvUIntMax = 1.0 / uint.MaxValue;
    private const ulong Kx = 0xD2A98B26625EEE7BUL;
    private const ulong Ky = 0xA3F5E6A3B1C9D7E5UL;
    private const ulong M1 = 0xFF51AFD7ED558CCDUL;
    private const ulong M2 = 0xC4CEB9FE1A85EC53UL;

    public static int Combine(params int[] values)
    {
        unchecked
        {
            uint hash = 2166136261; // FNV-1a offset basis

            foreach (int value in values)
            {
                hash ^= (uint)value;
                hash *= 16777619;
            }

            return (int)hash;
        }
    }

    /// <summary>
    /// Returns a single-octave noise value in [0, 1] at the given world coordinates.
    /// </summary>
    public double Sample(double x, double y)
    {
        // Multiply by inverse instead of dividing — same result, cheaper operation
        return SampleGrid(x * _inverted, y * _inverted);
    }

    /// <summary>
    /// Returns fractal Brownian motion noise in [0, 1].
    /// Converts to grid space once upfront; each octave doubles the grid coordinates
    /// rather than re-scaling world coordinates and dividing again inside Sample.
    /// </summary>
    public double SampleFractal(double x, double y, int octaves, double persistence)
    {
        // One multiply each instead of octaves multiplies + octaves divisions
        double gx = x * _inverted;
        double gy = y * _inverted;

        double total = 0;
        double amplitude = 1;
        double amplitudeSum = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += SampleGrid(gx, gy) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= persistence;
            gx *= 2; // doubling grid coords ≡ doubling frequency then dividing by blockSize
            gy *= 2;
        }

        return total / amplitudeSum;
    }

    /// <summary>
    /// Core sampling in grid space (coordinates already divided by blockSize).
    /// </summary>
    private double SampleGrid(double gx, double gy)
    {
        int x0 = (int)Math.Floor(gx);
        int y0 = (int)Math.Floor(gy);

        double tx = Smooth(gx - x0);
        double ty = Smooth(gy - y0);

        // Each corner needs: seedHash + x*Kx + y*Ky, then mix.
        // Since x1 = x0+1 and y1 = y0+1, the four pre-mix values share
        // all their structure. Factor into row and column terms:
        //   rowY = seedHash + y*Ky  (2 values, 1 mul each)
        //   colX = x*Kx             (2 values, 1 mul + 1 add)
        //   corner = rowY + colX    (4 additions, 0 extra muls)
        // Total: 3 multiplies for all 4 corners instead of 8.
        ulong rowY0 = _hash + (ulong)y0 * Ky;
        ulong rowY1 = rowY0 + Ky;     // y1 = y0 + 1, so (y0+1)*Ky = y0*Ky + Ky
        ulong colX0 = (ulong)x0 * Kx;
        ulong colX1 = colX0 + Kx;    // x1 = x0 + 1, so (x0+1)*Kx = x0*Kx + Kx

        double v00 = Mix(rowY0 + colX0);
        double v10 = Mix(rowY0 + colX1);
        double v01 = Mix(rowY1 + colX0);
        double v11 = Mix(rowY1 + colX1);

        return Lerp(Lerp(v00, v10, tx), Lerp(v01, v11, tx), ty);
    }

    /// <summary>
    /// Finalises a combined hash value into a double in [0, 1].
    /// The seed XOR is already baked into the input via <see cref="_hash"/>.
    /// </summary>
    private static double Mix(ulong h)
    {
        unchecked
        {
            h ^= h >> 33;
            h *= M1;
            h ^= h >> 33;
            h *= M2;
            // Cast to uint before multiplying so we stay in [0, 1]
            return (uint)(h ^ (h >> 33)) * InvUIntMax;
        }
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double Smooth(double t) => t * t * (3.0 - 2.0 * t);
}