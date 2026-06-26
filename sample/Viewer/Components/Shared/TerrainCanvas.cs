using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Core.Models;

namespace Viewer.Components.Shared;

public sealed class TerrainCanvas : Control
{
    private readonly Dictionary<(int X, int Y), WriteableBitmap> _cache = [];
    private readonly DispatcherTimer _debounce;

    private Point _start;
    private bool _panning;
    private double _lastUpdateX;
    private double _lastUpdateY;

    public double OffsetX { get; private set; }
    public double OffsetY { get; private set; }
    public double Zoom { get; private set; } = 1.0;

    public event Action<double, double, double, double, double, int>? ViewportChanged;

    public static readonly StyledProperty<IEnumerable<TerrainGrid>?> ChunksProperty =
      AvaloniaProperty.Register<TerrainCanvas, IEnumerable<TerrainGrid>?>(nameof(Chunks));

    public static readonly StyledProperty<int> CellSizeProperty =
      AvaloniaProperty.Register<TerrainCanvas, int>(nameof(CellSize), defaultValue: 2);

    public IEnumerable<TerrainGrid>? Chunks
    {
        get => GetValue(ChunksProperty);
        set => SetValue(ChunksProperty, value);
    }

    public int CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    public TerrainCanvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _debounce.Tick += (s, e) =>
        {
            _debounce.Stop();

            _lastUpdateX = OffsetX;
            _lastUpdateY = OffsetY;

            ViewportChanged?.Invoke(Bounds.Width, Bounds.Height, OffsetX, OffsetY, Zoom, CellSize);
        };
    }

    private void QueueViewportUpdate()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        QueueViewportUpdate();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_panning)
        {
            var currentPos = e.GetPosition(this);
            var delta = _start - currentPos;

            OffsetX += delta.X / Zoom;
            OffsetY += delta.Y / Zoom;
            _start = currentPos;

            InvalidateVisual(); // Repaint loaded chunks at 60+ FPS instantly

            // Calculate the physical distance moved from our last data request checkpoint
            double distanceMoved = Math.Sqrt(Math.Pow(OffsetX - _lastUpdateX, 2) + Math.Pow(OffsetY - _lastUpdateY, 2));

            if (distanceMoved > 64) // Checkpoint reached: stream new chunks mid-drag
            {
                _lastUpdateX = OffsetX;
                _lastUpdateY = OffsetY;
                _debounce.Stop(); // Reset trailing edge timer

                ViewportChanged?.Invoke(Bounds.Width, Bounds.Height, OffsetX, OffsetY, Zoom, CellSize);
            }
            else
            {
                // If the user drags slowly or stops, let the trailing-edge timer catch up
                _debounce.Stop();
                _debounce.Start();
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double oldZoom = Zoom;
        Zoom = Math.Clamp(e.Delta.Y > 0 ? oldZoom * 1.15 : oldZoom / 1.15, 0.15, 8.0);

        var pointerPos = e.GetPosition(this);
        OffsetX = (OffsetX + (pointerPos.X / oldZoom)) - (pointerPos.X / Zoom);
        OffsetY = (OffsetY + (pointerPos.Y / oldZoom)) - (pointerPos.Y / Zoom);

        InvalidateVisual();
        QueueViewportUpdate();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _start = e.GetPosition(this);
            _panning = true;

            // Establish our base coordinate alignment when the drag begins
            _lastUpdateX = OffsetX;
            _lastUpdateY = OffsetY;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _panning = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChunksProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldNotify)
                oldNotify.CollectionChanged -= OnChunksCollectionChanged;

            ClearCache();

            if (change.NewValue is INotifyCollectionChanged newNotify)
                newNotify.CollectionChanged += OnChunksCollectionChanged;

            InvalidateVisual();
        }
    }

    private void OnChunksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
            ClearCache();
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (TerrainGrid oldChunk in e.OldItems)
            {
                var key = (oldChunk.ChunkX, oldChunk.ChunkY);
                if (_cache.TryGetValue(key, out var bitmap))
                {
                    bitmap.Dispose();
                    _cache.Remove(key);
                }
            }
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Chunks is null) return;

        var cameraTransform = Matrix.CreateTranslation(new Vector(-OffsetX, -OffsetY))
                            * Matrix.CreateScale(new Vector(Zoom, Zoom));

        using (context.PushTransform(cameraTransform))
        {
            // Canvas side culling: Only draw chunks that are actually inside the camera view frame
            double viewW = Bounds.Width / Zoom;
            double viewH = Bounds.Height / Zoom;
            var viewportRect = new Rect(OffsetX, OffsetY, viewW, viewH);

            foreach (var chunk in Chunks.ToList())
            {
                int chunkPixelSize = chunk.Width * CellSize;
                var destRect = new Rect(chunk.ChunkX * chunkPixelSize, chunk.ChunkY * chunkPixelSize, chunkPixelSize, chunkPixelSize);

                if (!viewportRect.Intersects(destRect))
                    continue; // Skip rendering if it's completely hidden off-screen

                var key = (chunk.ChunkX, chunk.ChunkY);
                if (!_cache.TryGetValue(key, out var bitmap))
                {
                    bitmap = CreateChunkBitmap(chunk);
                    _cache[key] = bitmap;
                }

                context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), destRect);
            }
        }
    }

    private static WriteableBitmap CreateChunkBitmap(TerrainGrid chunk)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(chunk.Width, chunk.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using (var buf = bitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)buf.Address;
                int stride = buf.RowBytes / 4;
                foreach (var cell in chunk)
                {
                    ptr[(cell.Y * stride) + cell.X] = GetColor(cell.Biome);
                }
            }
        }
        return bitmap;
    }

    private void ClearCache()
    {
        foreach (var bmp in _cache.Values) bmp.Dispose();
        _cache.Clear();
    }

    private static uint GetColor(TerrainBiome biome) => biome switch
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
        _ => 0xFFFF00FF
    };
}