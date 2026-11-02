using System.Collections.Concurrent;
using System.Text;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class AtomUtf8BinaryCache
{
    private static readonly Encoding encoding = new UTF8Encoding(false, true);
    private static readonly ConditionalWeakTable<AtomNameTable, ConcurrentDictionary<string, BitString>> binaries = new();

    internal static bool IsCached357(Atom atom, BitString binary)
        => binaries.TryGetValue(AtomNameRegistry.Current, out var values)
            && values.TryGetValue(atom.Name, out var cached) && ReferenceEquals(cached, binary);

    internal static BitString Get(Atom atom)
    {
        AtomNameTable table = AtomNameRegistry.Current;
        table.Observe(atom.Name);

        return binaries.GetValue(table, static _ => new(StringComparer.Ordinal))
            .GetOrAdd(atom.Name, static name => new BitString(encoding.GetBytes(name)));
    }
}
