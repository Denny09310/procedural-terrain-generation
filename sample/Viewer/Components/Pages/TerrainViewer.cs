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
        return new Grid()
            .OnInitialized(async _ => await state.LoadAsync())
            .Children(
                new TerrainCanvas()
                    .Terrain(state, x => x.Terrain)
                    .CellSize(2)
            );
    }
    public partial class State(TerrainGenerator generator) : ObservableObject
    {
        [ObservableProperty]
        public partial TerrainGrid? Terrain { get; set; }

        public async Task LoadAsync()
        {
            Terrain = await generator.GenerateAsync();
        }
    }
}