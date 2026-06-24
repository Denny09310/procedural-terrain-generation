namespace Core.Models;

public sealed class Cell
{
    public double Elevation { get; set; }
    public double Moisture { get; set; }
    public double Temperature { get; set; }

    public double River { get; set; }

    public Biome Biome { get; set; }
    public StructureType Structure { get; set; }
}
