using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Core.Services;
using Viewer.Components.Shared;

namespace Viewer.Components.Pages.Counter;

public partial class TerrainViewer(TerrainGenerator generator)
    : ViewBase<TerrainViewer.State>(new(generator))
{
    protected override object Build(State state)
    {
        var canvas = new TerrainCanvas()
            .Chunks(state, x => x.Chunks)
            .CellSize(2)
            .OnViewportChanged(async (width, height, offsetX, offsetY, zoom, size) =>
            {
                await state.UpdateViewportAsync(width, height, offsetX, offsetY, zoom, size);
            });

        return new Grid()
            .OnInitialized(async _ => await state.InitializeAsync())
            .Children(canvas);
    }

    public partial class State(TerrainGenerator generator) : ObservableObject
    {
        private readonly HashSet<(int X, int Y)> _loaded = [];
        public ObservableCollection<TerrainGrid> Chunks { get; } = [];

        private CancellationTokenSource? _cts;

        public async Task InitializeAsync()
        {
            _cts?.Cancel();
            Chunks.Clear();
            _loaded.Clear();
        }

        public async Task UpdateViewportAsync(double width, double height, double offsetX, double offsetY, double zoom, int size)
        {
            if (width <= 0 || height <= 0) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            int chunkSize = generator.Config.ChunkSize;
            int chunkPixelSize = chunkSize * size;

            int padding = 2;

            int minChunkX = (int)Math.Floor(offsetX / chunkPixelSize) - padding;
            int maxChunkX = (int)Math.Ceiling((offsetX + (width / zoom)) / chunkPixelSize) + padding;
            int minChunkY = (int)Math.Floor(offsetY / chunkPixelSize) - padding;
            int maxChunkY = (int)Math.Ceiling((offsetY + (height / zoom)) / chunkPixelSize) + padding;

            var offScreenChunks = Chunks
                .Where(c => c.ChunkX < minChunkX || c.ChunkX > maxChunkX || c.ChunkY < minChunkY || c.ChunkY > maxChunkY)
                .ToList();

            foreach (var chunk in offScreenChunks)
            {
                Chunks.Remove(chunk);
                _loaded.Remove((chunk.ChunkX, chunk.ChunkY));
            }

            var tasks = new List<Task>();

            for (int x = minChunkX; x <= maxChunkX; x++)
            {
                for (int y = minChunkY; y <= maxChunkY; y++)
                {
                    if (token.IsCancellationRequested) return;

                    if (_loaded.Add((x, y)))
                    {
                        int currentX = x;
                        int currentY = y;

                        tasks.Add(Task.Run(async () =>
                        {
                            var chunk = await generator.GenerateChunkAsync(currentX, currentY);

                            chunk.ChunkX = currentX;
                            chunk.ChunkY = currentY;

                            Dispatcher.UIThread.Post(() =>
                            {
                                Chunks.Add(chunk);
                            });
                        }));
                    }
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }
}