using System.Collections.Concurrent;
using System.Text;

namespace Erlang;

internal static class AtomUtf8BinaryCache
{
    private static readonly Encoding encoding = new UTF8Encoding(false, true);
    private static readonly ConcurrentDictionary<string, BitString> binaries = new(StringComparer.Ordinal);

    internal static BitString Get(Atom atom)
        => binaries.GetOrAdd(atom.Name, static name => new BitString(encoding.GetBytes(name)));
}
