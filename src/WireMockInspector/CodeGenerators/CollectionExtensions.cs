using System.Collections.Generic;

namespace WireMockInspector.CodeGenerators;

internal static class CollectionExtensions
{
    public static IDictionary<string, T>? OrNullWhenEmpty<T>(this IDictionary<string, T>? @this)
    {
        if (@this == null || @this.Count == 0)
        {
            return null;
        }

        return @this;
    }
}