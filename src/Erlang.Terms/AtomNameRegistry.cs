using System.Collections.Concurrent;

namespace Erlang;

public static class AtomNameRegistry
{
    private static readonly ConcurrentDictionary<string, int> names = new(StringComparer.Ordinal);
    private static readonly object gate = new();

    static AtomNameRegistry()
    {
        names.TryAdd(ErlangBooleanAtoms.False, 0);
        foreach (string name in ErlangPrimordialAtomNames.All)
            names.TryAdd(name, names.Count);
    }

    public static void Observe(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (names.ContainsKey(name))
            return;
        lock (gate)
        {
            if (names.ContainsKey(name))
                return;
            if (names.Count >= AtomTableLimits.MaximumNames)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            names.TryAdd(name, names.Count);
        }
    }

    public static string Register(string name)
    {
        Observe(name);

        return name;
    }

    public static int Count => names.Count;

    public static bool Exists(string name) => names.ContainsKey(name);

    public static int IndexOf(string name)
    {
        Observe(name);

        return names[name];
    }
}
