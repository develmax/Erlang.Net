using System.Reflection;

namespace Erlang.Tests;

internal static class AtomMapWire494Expected
{
    internal static (byte[] Bytes, Dictionary<string, int> Indices) Encode(Term expected)
    {
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var options = new EtfEncodingOptions();
        Func<MapTerm, IReadOnlyList<KeyValuePair<Term, Term>>> entries = map =>
        {
            if (map.Entries.Count > AtomMapWire494Names.MaximumKeys || map.Entries.Any(pair => pair.Key is not Atom))
                throw new InvalidOperationException(AtomMapWire494Names.Failure);
            foreach (var pair in map.Entries)
            {
                string name = ((Atom)pair.Key).Name;
                if (!AtomNameRegistry.Exists(name))
                    throw new InvalidOperationException(AtomMapWire494Names.Failure);
                indices[name] = AtomNameRegistry.IndexOf(name);
            }

            return map.Entries.OrderBy(pair => indices[((Atom)pair.Key).Name]).ToArray();
        };
        typeof(EtfEncodingOptions).GetProperty(AtomMapWire494Names.MapEntries, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(options, entries);

        return (ExternalTermFormat.Encode(expected, options), indices);
    }
}
