using System.Collections;

namespace Core.Models;

public sealed class TerrainGrid : IEnumerable<TerrainCell>
{
    private readonly TerrainCell[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public int ChunkX { get; set; }
    public int ChunkY { get; set; }

    public TerrainGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new TerrainCell[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _cells[x, y] = new TerrainCell { X = x, Y = y };
    }

    public TerrainCell this[int x, int y] => _cells[x, y];

    public IEnumerator<TerrainCell> GetEnumerator()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return _cells[x, y];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}