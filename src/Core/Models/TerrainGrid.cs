using System.Collections;

namespace Core.Models;

public sealed class TerrainGrid : IEnumerable<TerrainCell>
{
    private readonly TerrainCell[,] _cells;

    public int Width { get; }
    public int Height { get; }

    public int X { get; init; }

    public int Y { get; init; }

    public TerrainGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new TerrainCell[width, height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _cells[x, y] = new TerrainCell { X = x, Y = y };
    }

    /// <summary>Converts a local X coordinate into an absolute world-space X coordinate.</summary>
    public int ToWorldX(int localX) => X * Width + localX;

    /// <summary>Converts a local Y coordinate into an absolute world-space Y coordinate.</summary>
    public int ToWorldY(int localY) => Y * Height + localY;

    public IEnumerator<TerrainCell> GetEnumerator()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return _cells[x, y];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
