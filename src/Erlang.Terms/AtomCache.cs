using System.Collections.Frozen;

namespace Erlang;

/// <summary>Reuses a fixed set of immutable values; arbitrary names are never retained.</summary>
internal static class AtomCache
{
    private static readonly FrozenDictionary<string, Atom> Values = new[]
    {
        CommonAtomNames.Ok,
        CommonAtomNames.Error,
        CommonAtomNames.True,
        CommonAtomNames.False,
        CommonAtomNames.Undefined,
        CommonAtomNames.Normal,
        CommonAtomNames.Timeout,
        CommonAtomNames.BadArgument,
        CommonAtomNames.NoProcess,
        CommonAtomNames.Shutdown
    }.ToFrozenDictionary(name => name, name => new Atom(name), StringComparer.Ordinal);

    public static Atom Get(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Values.TryGetValue(name, out var atom) ? atom : new Atom(name);
    }
}
