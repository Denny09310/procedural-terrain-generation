using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Core.Services;
using Viewer.Components.Shared;

namespace Viewer.Components.Pages.Counter;

public partial class TerrainViewer(Func<int, TerrainGenerator> factory)
    : ViewBase<TerrainViewer.State>(new(factory))
{
    protected override object Build(State state)
    {
        TerrainCanvas? canvas = null;

        canvas = new TerrainCanvas()
            .Chunks(state, x => x.Chunks)
            .CellSize(state, x => x.Size)
            .OnViewportChanged(async info =>
                await state.UpdateViewportAsync(info));

        var toolbar = new Border()
            .Padding(new Thickness(8, 6))
            .Background(Brushes.Black)
            .ZIndex(10)
            .Child(
                new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(
                    // ── Seed ──────────────────────────────────────────────
                    new TextBlock()
                        .Text("Seed:")
                        .VerticalAlignment(VerticalAlignment.Center),

                    new TextBox()
                        .Text(state, x => x.Seed)
                        .MinWidth(110)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .PlaceholderText("integer seed"),

                    new Button()
                        .Content("Regenerate")
                        .OnClick(async (_) => await state.RegenerateAsync()),

                    // ── Separator ─────────────────────────────────────────
                    new Border()
                        .Width(1)
                        .Height(20)
                        .Background(Brushes.Gray)
                        .VerticalAlignment(VerticalAlignment.Center),

                    // ── Pixel size ────────────────────────────────────────
                    new TextBlock()
                        .Text("Pixels:")
                        .VerticalAlignment(VerticalAlignment.Center),

                    new Button().Content("1×").OnClick((_) => state.Size = 1),
                    new Button().Content("2×").OnClick((_) => state.Size = 2),
                    new Button().Content("4×").OnClick((_) => state.Size = 4),
                    new Button().Content("8×").OnClick((_) => state.Size = 8),

                    // ── Separator ─────────────────────────────────────────
                    new Border()
                        .Width(1)
                        .Height(20)
                        .Background(Brushes.Gray)
                        .VerticalAlignment(VerticalAlignment.Center),

                    // ── Viewport reset ────────────────────────────────────
                    new Button()
                        .Content("⌂ Reset View")
                        .OnClick((_) => canvas?.ResetViewport())
                )
            );

        DockPanel.SetDock(toolbar, Dock.Top);

        return new DockPanel()
            .LastChildFill(true)
            .OnInitialized(async _ => await state.InitializeAsync())
            .Children(toolbar, canvas);
    }

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    public partial class State(Func<int, TerrainGenerator> generatorFactory) : ObservableObject
    {
        private TerrainGenerator _generator = generatorFactory(12345);

        private readonly HashSet<(int X, int Y)> _loaded = [];
        public ObservableCollection<TerrainGrid> Chunks { get; } = [];

        private CancellationTokenSource? _cts;

        // ── Observable properties bound to the toolbar ──────────────────────

        [ObservableProperty] public partial int Seed { get; set; } = 12345;
        [ObservableProperty] public partial int Size { get; set; } = 2;

        // Cached so we can re-fire a viewport update after regeneration
        private ViewportInfo _last;

        // ── Regenerate ──────────────────────────────────────────────────────

        public async Task RegenerateAsync()
        {
            _generator = generatorFactory(Seed);
            await InitializeAsync();

            // Re-load the same viewport with the new generator
            if (_last.Width > 0)
                await UpdateViewportAsync(_last);
        }

        // ── Lifecycle ───────────────────────────────────────────────────────

        public async Task InitializeAsync()
        {
            _cts?.Cancel();
            Chunks.Clear();
            _loaded.Clear();
            await Task.CompletedTask;
        }

        // ── Chunk streaming ─────────────────────────────────────────────────

        public async Task UpdateViewportAsync(ViewportInfo vp)
        {
            if (vp.Width <= 0 || vp.Height <= 0) return;

            _last = vp;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            int chunkSize = _generator.Config.ChunkSize;
            int chunkPixelSize = chunkSize * vp.CellSize;
            int padding = 2;

            int minCX = (int)Math.Floor(vp.OffsetX / chunkPixelSize) - padding;
            int maxCX = (int)Math.Ceiling((vp.OffsetX + vp.Width / vp.Zoom) / chunkPixelSize) + padding;
            int minCY = (int)Math.Floor(vp.OffsetY / chunkPixelSize) - padding;
            int maxCY = (int)Math.Ceiling((vp.OffsetY + vp.Height / vp.Zoom) / chunkPixelSize) + padding;

            // Evict chunks that have scrolled off screen
            var offScreen = Chunks
                .Where(c => c.ChunkX < minCX || c.ChunkX > maxCX ||
                            c.ChunkY < minCY || c.ChunkY > maxCY)
                .ToList();

            foreach (var chunk in offScreen)
            {
                Chunks.Remove(chunk);
                _loaded.Remove((chunk.ChunkX, chunk.ChunkY));
            }

            // Queue generation for newly visible chunks
            var tasks = new List<Task>();

            for (int cx = minCX; cx <= maxCX; cx++)
                for (int cy = minCY; cy <= maxCY; cy++)
                {
                    if (token.IsCancellationRequested) return;
                    if (!_loaded.Add((cx, cy))) continue;

                    int x = cx, y = cy;
                    tasks.Add(Task.Run(async () =>
                    {
                        var chunk = await _generator.GenerateChunkAsync(x, y);
                        chunk.ChunkX = x;
                        chunk.ChunkY = y;

                        Dispatcher.UIThread.Post(() => Chunks.Add(chunk));
                    }));
                }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
    }
}
