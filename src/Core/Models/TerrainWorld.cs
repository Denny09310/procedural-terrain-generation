namespace Core.Models;

/// <summary>
/// The top-level world model. Owns all generated <see cref="TerrainGrid"/> chunks
/// and all placed <see cref="TerrainStructure"/> instances, and drives on-demand
/// chunk generation via a factory delegate supplied by <c>TerrainGenerator</c>.
/// </summary>
public sealed class TerrainWorld
{
    private readonly Func<TerrainWorld, int, int, ValueTask<TerrainGrid>> _factory;

    public List<TerrainStructure> Structures { get; } = [];
    public Dictionary<TerrainCoordinate, TerrainGrid> Chunks { get; } = [];


    /// <remarks>
    /// Use <c>TerrainGenerator.CreateWorld()</c> — the factory wires up the full
    /// transformer pipeline so chunks are always fully generated on first access.
    /// </remarks>
    internal TerrainWorld(Func<TerrainWorld, int, int, ValueTask<TerrainGrid>> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Returns an already-generated chunk, or generates and caches a new one
    /// at the given chunk-space coordinates.
    /// </summary>
    public async ValueTask<TerrainGrid> GenerateChunkAsync(int chunkX, int chunkY)
    {
        var key = new TerrainCoordinate(chunkX, chunkY);

        if (Chunks.TryGetValue(key, out var existing))
            return existing;

        var grid = await _factory(this, chunkX, chunkY);
        Chunks[key] = grid;
        return grid;
    }

    /// <summary>Registers a structure in the world.</summary>
    public void AddStructure(TerrainStructure structure) =>
        Structures.Add(structure);
}
