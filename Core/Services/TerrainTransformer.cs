using System.Linq.Expressions;
using System.Reflection;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services;

public sealed record TerrainHandler(
    Delegate Delegate,
    MethodInfo Method,
    ParameterInfo[] Parameters);

public delegate void TerrainTransformer(
    Cell[,] world,
    TerrainContext context);

public static class TerrainTransformerBinder
{
    private static readonly MethodInfo Getter =
        typeof(ServiceProviderServiceExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1);

    public static TerrainTransformer Bind(TerrainHandler handler, IServiceProvider services)
    {
        var cells = Expression.Parameter(typeof(Cell[,]), "cells");
        var context = Expression.Parameter(typeof(TerrainContext), "context");
        var provider = Expression.Constant(services);

        var arguments = new List<Expression>();

        foreach (var parameter in handler.Parameters)
        {
            if (parameter.ParameterType == typeof(Cell[,]))
            {
                arguments.Add(cells);
                continue;
            }

            if (parameter.ParameterType == typeof(TerrainContext))
            {
                arguments.Add(context);
                continue;
            }

            var getter = Getter
                .MakeGenericMethod(parameter.ParameterType);

            arguments.Add(Expression.Call(getter, provider));
        }

        var body = Expression.Call(handler.Method, arguments);

        return Expression.Lambda<TerrainTransformer>(
            body,
            cells,
            context).Compile();
    }
}