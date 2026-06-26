using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Core.Models;

namespace Viewer.Components.Shared;

/// <summary>Snapshot of the canvas viewport passed to every ViewportChanged callback.</summary>
public readonly record struct ViewportInfo(
    double Width,
    double Height,
    double OffsetX,
    double OffsetY,
    double Zoom,
    int CellSize);

public sealed class TerrainCanvas : Control
{
    private readonly record struct ChunkRenderData(
        int ChunkX,
        int ChunkY,
        WriteableBitmap Bitmap);

    private readonly List<ChunkRenderData> _renderList = [];

    // ------------------------------------------------------------------
    // Panning / zoom state
    // ------------------------------------------------------------------

    private Point _dragStart;
    private bool _panning;
    private double _checkpointX;
    private double _checkpointY;

    public double OffsetX { get; private set; }
    public double OffsetY { get; private set; }
    public double Zoom { get; private set; } = 1.0;

    // ------------------------------------------------------------------
    // Debounce timer — fires a trailing-edge viewport update after panning stops
    // ------------------------------------------------------------------

    private readonly DispatcherTimer _debounce;

    // ------------------------------------------------------------------
    // Styled properties
    // ------------------------------------------------------------------

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

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    public event Action<ViewportInfo>? ViewportChanged;

    // ------------------------------------------------------------------
    // Ctor
    // ------------------------------------------------------------------

    public TerrainCanvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _checkpointX = OffsetX;
            _checkpointY = OffsetY;
            FireViewportChanged();
        };
    }

    // ------------------------------------------------------------------
    // Public helper — called by the toolbar "reset view" button
    // ------------------------------------------------------------------

    public void ResetViewport()
    {
        OffsetX = 0;
        OffsetY = 0;
        Zoom = 1.0;
        InvalidateVisual();
        QueueViewportUpdate();
    }

    // ------------------------------------------------------------------
    // Property change — Chunks subscription and CellSize
    // ------------------------------------------------------------------

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChunksProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldCol)
                oldCol.CollectionChanged -= OnChunksCollectionChanged;

            ClearRenderList();

            if (change.NewValue is INotifyCollectionChanged newCol)
                newCol.CollectionChanged += OnChunksCollectionChanged;

            // Eagerly bake any chunks already in the new collection
            if (change.NewValue is IEnumerable<TerrainGrid> existing)
            {
                foreach (var chunk in existing)
                    _renderList.Add(Bake(chunk));
            }

            InvalidateVisual();
        }
        else if (change.Property == CellSizeProperty)
        {
            // Pixel density changed: repaint immediately and re-query which
            // chunks are needed for the new coverage.
            InvalidateVisual();
            QueueViewportUpdate();
        }
    }

    // ------------------------------------------------------------------
    // Collection changed — keeps _renderList in sync
    // ------------------------------------------------------------------

    private void OnChunksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                foreach (TerrainGrid chunk in e.NewItems)
                    _renderList.Add(Bake(chunk));
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                foreach (TerrainGrid removed in e.OldItems)
                {
                    int idx = _renderList.FindIndex(
                        r => r.ChunkX == removed.ChunkX && r.ChunkY == removed.ChunkY);

                    if (idx >= 0)
                    {
                        _renderList[idx].Bitmap.Dispose();
                        _renderList.RemoveAt(idx);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ClearRenderList();
                break;
        }

        InvalidateVisual();
    }

    // ------------------------------------------------------------------
    // Render — purely reads _renderList, no allocations
    // ------------------------------------------------------------------

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_renderList.Count == 0) return;

        var cameraTransform =
            Matrix.CreateTranslation(new Vector(-OffsetX, -OffsetY)) *
            Matrix.CreateScale(new Vector(Zoom, Zoom));

        using (context.PushTransform(cameraTransform))
        {
            double viewW = Bounds.Width / Zoom;
            double viewH = Bounds.Height / Zoom;
            var viewport = new Rect(OffsetX, OffsetY, viewW, viewH);

            foreach (var entry in _renderList)
            {
                int pixelSize = entry.Bitmap.PixelSize.Width * CellSize;
                var dest = new Rect(
                    entry.ChunkX * pixelSize,
                    entry.ChunkY * pixelSize,
                    pixelSize,
                    pixelSize);

                if (!viewport.Intersects(dest)) continue;

                var src = new Rect(0, 0, entry.Bitmap.PixelSize.Width, entry.Bitmap.PixelSize.Height);
                context.DrawImage(entry.Bitmap, src, dest);
            }
        }
    }

    // ------------------------------------------------------------------
    // Input — pan and zoom
    // ------------------------------------------------------------------

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        QueueViewportUpdate();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStart = e.GetPosition(this);
            _panning = true;
            _checkpointX = OffsetX;
            _checkpointY = OffsetY;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _panning = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_panning) return;

        var pos = e.GetPosition(this);
        var delta = _dragStart - pos;

        OffsetX += delta.X / Zoom;
        OffsetY += delta.Y / Zoom;
        _dragStart = pos;

        InvalidateVisual(); // instant repaint of cached bitmaps at 60+ fps

        double dist = Math.Sqrt(
            Math.Pow(OffsetX - _checkpointX, 2) +
            Math.Pow(OffsetY - _checkpointY, 2));

        if (dist > 64) // crossed into a new region: stream chunks mid-drag
        {
            _checkpointX = OffsetX;
            _checkpointY = OffsetY;
            _debounce.Stop();
            FireViewportChanged();
        }
        else
        {
            // Slow / stopped drag: trailing-edge timer will catch up
            _debounce.Stop();
            _debounce.Start();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        double oldZoom = Zoom;
        Zoom = Math.Clamp(e.Delta.Y > 0 ? oldZoom * 1.15 : oldZoom / 1.15, 0.15, 8.0);

        var ptr = e.GetPosition(this);
        OffsetX = OffsetX + ptr.X / oldZoom - ptr.X / Zoom;
        OffsetY = OffsetY + ptr.Y / oldZoom - ptr.Y / Zoom;

        InvalidateVisual();
        QueueViewportUpdate();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void QueueViewportUpdate()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void FireViewportChanged() =>
        ViewportChanged?.Invoke(new ViewportInfo(
            Bounds.Width, Bounds.Height,
            OffsetX, OffsetY,
            Zoom,
            CellSize));

    private void ClearRenderList()
    {
        foreach (var entry in _renderList)
            entry.Bitmap.Dispose();
        _renderList.Clear();
    }

    // ------------------------------------------------------------------
    // Bitmap creation — called eagerly on chunk Add, not in Render()
    // ------------------------------------------------------------------

    private static ChunkRenderData Bake(TerrainGrid chunk)
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
                    ptr[cell.Y * stride + cell.X] = BiomeColor(cell.Biome);
            }
        }

        return new ChunkRenderData(chunk.ChunkX, chunk.ChunkY, bitmap);
    }

    private static uint BiomeColor(TerrainBiome biome) => biome switch
    {
        TerrainBiome.DeepOcean => 0xFF002060,
        TerrainBiome.Ocean => 0xFF1565C0,
        TerrainBiome.ShallowWater => 0xFF4FC3F7,
        TerrainBiome.River => 0xFF42A5F5,
        TerrainBiome.Beach => 0xFFF4E19C,
        TerrainBiome.Desert => 0xFFE0C068,
        TerrainBiome.Steppa => 0xFFB8B050,
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
        _ => 0xFFFF00FF,
    };
}
