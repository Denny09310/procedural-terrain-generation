using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Core.Models;

namespace Viewer.Components.Shared;

public sealed class TerrainCanvas : Control
{
    private WriteableBitmap? _bitmap;

    public static readonly StyledProperty<TerrainGrid?> TerrainProperty =
        AvaloniaProperty.Register<TerrainCanvas, TerrainGrid?>(nameof(Terrain));

    public static readonly StyledProperty<int> CellSizeProperty =
        AvaloniaProperty.Register<TerrainCanvas, int>(nameof(CellSize), defaultValue: 2);

    public TerrainGrid? Terrain
    {
        get => GetValue(TerrainProperty);
        set => SetValue(TerrainProperty, value);
    }

    public int CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Regenerate the bitmap only when the Terrain data actually changes
        if (change.Property == TerrainProperty)
        {
            UpdateBitmap();
            InvalidateVisual();
        }
        else if (change.Property == CellSizeProperty)
        {
            InvalidateVisual();
        }
    }

    private void UpdateBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;

        if (Terrain is null || !Terrain.Any())
            return;

        int width = Terrain.Width;
        int height = Terrain.Height;

        // Create a 1-to-1 pixel bitmap matching our grid size
        _bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using var buf = _bitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)buf.Address;
            int stride = buf.RowBytes / 4; // Number of uints per row

            foreach (var cell in Terrain)
            {
                // Calculate pixel index position in memory
                int index = (cell.Y * stride) + cell.X;
                ptr[index] = GetBgraColor(cell.Biome);
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_bitmap is null)
            return;

        // Let the GPU scale our pixel map perfectly to match the CellSize
        var sourceRect = new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height);
        var destRect = new Rect(0, 0, _bitmap.PixelSize.Width * CellSize, _bitmap.PixelSize.Height * CellSize);

        // Render the pre-baked bitmap to the screen instantly
        context.DrawImage(_bitmap, sourceRect, destRect);
    }

    // Pre-calculated Hex colors as native uints (0xFFRRGGBB) matching Bgra8888 format
    private static uint GetBgraColor(TerrainBiome biome) => biome switch
    {
        TerrainBiome.DeepOcean => 0xFF002060,
        TerrainBiome.Ocean => 0xFF1565C0,
        TerrainBiome.ShallowWater => 0xFF4FC3F7,
        TerrainBiome.River => 0xFF42A5F5,
        TerrainBiome.Beach => 0xFFF4E19C,
        TerrainBiome.Desert => 0xFFE0C068,
        TerrainBiome.DryGrassland => 0xFFB8B050,
        TerrainBiome.Savanna => 0xFFA5C85A,
        TerrainBiome.Grassland => 0xFF4CAF50,
        TerrainBiome.Shrubland => 0xFF5E8C4A,
        TerrainBiome.TemperateForest => 0xFF2E7D32,
        TerrainBiome.TemperateRainforest => 0xFF1B5E20,
        TerrainBiome.TropicalForest => 0xFF228B22,
        TerrainBiome.TropicalRainforest => 0xFF006400,
        TerrainBiome.Jungle => 0xFF004B23,
        TerrainBiome.Taiga => 0xFF3D6B52,
        TerrainBiome.BorealForest => 0xFF5F8D4E,
        TerrainBiome.Tundra => 0xFFBFC6C4,
        TerrainBiome.Alpine => 0xFF888888,
        TerrainBiome.SubAlpine => 0xFF6E6E6E,
        TerrainBiome.Mountain => 0xFF505050,
        TerrainBiome.Snow => 0xFFFFFFFF,
        TerrainBiome.Glacier => 0xFFDDEEFF,
        TerrainBiome.Wetland => 0xFF608040,
        TerrainBiome.Swamp => 0xFF355E3B,
        TerrainBiome.Mangrove => 0xFF2F6F3E,
        TerrainBiome.Volcanic => 0xFF5A2A00,
        _ => 0xFFFF00FF // Magenta fallback
    };
}