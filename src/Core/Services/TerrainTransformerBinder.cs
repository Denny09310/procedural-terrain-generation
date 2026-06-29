using System.Reflection;
using Core.Models;

namespace Core.Services;

internal sealed class TerrainTransformerBinder(IServiceProvider services)
{
    public Func<TerrainContext, ValueTask> Bind(TerrainTransformer handler)
    {
        var (delegate_, method, parameters) = handler;

        var resolvers = parameters
            .Select(ResolveParameter)
            .ToArray();

        return async ctx =>
        {
            var args = resolvers.Select(r => r(ctx)).ToArray();
            var result = method.Invoke(delegate_.Target, args);

            if (result is Task task)
                await task;

            else if (result is ValueTask vt)
                await vt;
        };
    }

    private Func<TerrainContext, dynamic> ResolveParameter(ParameterInfo p)
    {
        if (p.ParameterType == typeof(TerrainWorld))
            return ctx => ctx.World;

        if (p.ParameterType == typeof(TerrainGrid))
            return ctx => ctx.Grid;

        if (p.ParameterType == typeof(TerrainConfiguration))
            return ctx => ctx.Configuration;

        if (p.ParameterType == typeof(TerrainContext))
            return ctx => ctx;

        if (services.GetService(p.ParameterType) is { } svc)
            return _ => svc;

        throw new InvalidOperationException(
            $"No binding source for parameter '{p.Name}' of type '{p.ParameterType.Name}'.");
    }
}