using System.Reflection;

namespace Core.Models;

public sealed record TerrainTransformer(
    Delegate Delegate,
    MethodInfo Method,
    ParameterInfo[] Parameters)
{
    public static TerrainTransformer Create(Delegate handler)
    {
        return new(
            handler,
            handler.Method,
            handler.Method.GetParameters());
    }
}
