using System.Collections.Frozen;

namespace Erlang.Compiler;

internal static class AttributeControls
{
    private static readonly FrozenSet<string> Reserved = AttributeControlNames.Reserved.ToFrozenSet(StringComparer.Ordinal);

    public static bool IsReserved(string name) => Reserved.Contains(name);
}
