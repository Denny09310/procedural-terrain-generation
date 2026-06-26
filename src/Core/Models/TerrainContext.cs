using Core.Services;

namespace Core.Models;

public sealed class TerrainContext(TerrainGrid grid, TerrainConfiguration config)
{
    public TerrainGrid Grid { get; } = grid;
    public TerrainConfiguration Config { get; } = config;
}
