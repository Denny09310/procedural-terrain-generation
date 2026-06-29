namespace Core.Models;

public sealed class TerrainCell
{
    public int X { get; init; }
    public int Y { get; init; }

    public float Elevation { get; set; }
    public float Moisture { get; set; }
    public float Temperature { get; set; }

    public BiomeType Biome { get; set; } = BiomeType.Unknown;
}
