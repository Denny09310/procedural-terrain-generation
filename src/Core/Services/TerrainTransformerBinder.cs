using System.Reflection;
using Core.Models;

namespace Core.Services;

public sealed class TerrainTransformerBinder(IServiceProvider services)
{
    public Func<TerrainContext, ValueTask> Bind(TerrainTransformerHandler handler)
    {
        var resolvers = handler.Parameters
            .Select(ResolveParameter)
            .ToArray();

        return async ctx =>
        {
            var (delegate_, method, _) = handler;

            var args = resolvers.Select(r => r(ctx)).ToArray();
            var result = method.Invoke(delegate_.Target, args);

            if (result is Task task)
                await task;

            else if (result is ValueTask vt)
                await vt;
        };
    }

    private Func<TerrainContext, object?> ResolveParameter(ParameterInfo p)
    {
        if (p.ParameterType == typeof(TerrainGrid))
            return ctx => ctx.Grid;

        if (p.ParameterType == typeof(TerrainConfiguration))
            return ctx => ctx.Config;

        if (services.GetService(p.ParameterType) is { } svc)
            return _ => svc;

        throw new InvalidOperationException(
            $"No binding source for parameter '{p.Name}' of type '{p.ParameterType.Name}'.");
    }
}