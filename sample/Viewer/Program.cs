using Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Viewer.Components.Pages.Counter;
using Viewer.Helpers;

[assembly: GenerateMarkupExtensionsForAssembly(typeof(Program))]

var services = new ServiceCollection();

services.AddSingleton<Func<int, TerrainGenerator>>(_ => Factory.BuildGenerator);

var provider = services.BuildServiceProvider();

var lifetime = new ClassicDesktopStyleApplicationLifetime
{
    Args = args,
    ShutdownMode = ShutdownMode.OnLastWindowClose,
};

AppBuilder.Configure<Application>()
    .UsePlatformDetect()
    .AfterSetup(b => b.Instance?.Styles.Add(new FluentTheme()))
    .UseServiceProvider(provider)
    .UseComponentControlFactory(
        type => (Control)ActivatorUtilities.CreateInstance(provider, type))
    .UseViewInitializationStrategy(ViewInitializationStrategy.Lazy)
    .UseHotReload()
    .SetupWithLifetime(lifetime);

lifetime.MainWindow = new Window()
    .Title("Avalonia Procedural Terrain Viewer")
    .Width(1024)
    .Height(700)
    .Content(ViewFactory.Create<TerrainViewer>());

lifetime.Start(args);
