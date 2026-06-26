using System.Reflection;

namespace Core.Models;

public sealed record TerrainTransformerHandler(
    Delegate Delegate,
    MethodInfo Method,
    ParameterInfo[] Parameters)
{
    public static TerrainTransformerHandler Create(Delegate handler)
    {
        return new(
            handler,
            handler.Method,
            handler.Method.GetParameters());
    }
}